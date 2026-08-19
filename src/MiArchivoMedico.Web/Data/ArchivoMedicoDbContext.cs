using System.Reflection;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MiArchivoMedico.Web.Dominio;
using MiArchivoMedico.Web.Servicios;

namespace MiArchivoMedico.Web.Data;

/// <summary>
/// Punto autoritativo único de tres invariantes:
/// 1. El aislamiento por propietario, aplicado por reflexión a toda entidad <see cref="IPropiedadDeUsuario"/>.
/// 2. El estampado de OwnerId y el recálculo de las columnas normalizadas, en la interceptación de SaveChanges.
/// 3. La persistencia de DateTimeOffset como ticks UTC, porque SQLite no los compara en un ORDER BY.
/// Ningún controlador repite nada de esto.
/// </summary>
public class ArchivoMedicoDbContext(
    DbContextOptions<ArchivoMedicoDbContext> opciones,
    IUsuarioActual usuarioActual,
    TimeProvider reloj)
    : IdentityDbContext<Usuario>(opciones)
{
    private readonly IUsuarioActual _usuarioActual = usuarioActual;
    private readonly TimeProvider _reloj = reloj;

    public DbSet<Estudio> Estudios => Set<Estudio>();
    public DbSet<ArchivoDeEstudio> Archivos => Set<ArchivoDeEstudio>();
    public DbSet<EtiquetaDeEstudio> Etiquetas => Set<EtiquetaDeEstudio>();

    /// <summary>Sin filtro global: se consulta sin sesión, que es cuando se evalúa un intento.</summary>
    public DbSet<IntentoDeInicioDeSesion> Intentos => Set<IntentoDeInicioDeSesion>();

    protected override void OnModelCreating(ModelBuilder constructor)
    {
        base.OnModelCreating(constructor);

        constructor.Entity<Estudio>(e =>
        {
            e.Property(x => x.Titulo).HasMaxLength(Estudio.LargoMaximoTitulo).IsRequired();
            e.Property(x => x.TituloNormalizado).HasMaxLength(Estudio.LargoMaximoTitulo).IsRequired();
            e.Property(x => x.Profesional).HasMaxLength(Estudio.LargoMaximoProfesional);
            e.Property(x => x.ProfesionalNormalizado).HasMaxLength(Estudio.LargoMaximoProfesional).IsRequired();
            e.Property(x => x.Institucion).HasMaxLength(Estudio.LargoMaximoInstitucion);
            e.Property(x => x.InstitucionNormalizada).HasMaxLength(Estudio.LargoMaximoInstitucion).IsRequired();
            e.Property(x => x.Descripcion).HasMaxLength(Estudio.LargoMaximoDescripcion);
            e.Property(x => x.DescripcionNormalizada).HasMaxLength(Estudio.LargoMaximoDescripcion).IsRequired();

            e.HasIndex(x => x.TituloNormalizado);
            e.HasIndex(x => x.ProfesionalNormalizado);
            e.HasIndex(x => x.InstitucionNormalizada);
            e.HasIndex(x => x.DescripcionNormalizada);
            e.HasIndex(x => new { x.OwnerId, x.Fecha });

            e.HasMany(x => x.Archivos).WithOne(x => x.Estudio!)
                .HasForeignKey(x => x.EstudioId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Etiquetas).WithOne(x => x.Estudio!)
                .HasForeignKey(x => x.EstudioId).OnDelete(DeleteBehavior.Cascade);
        });

        constructor.Entity<ArchivoDeEstudio>(e =>
        {
            e.Property(x => x.NombreOriginal).HasMaxLength(ArchivoDeEstudio.LargoMaximoNombreOriginal).IsRequired();
            e.Property(x => x.TipoDeContenido).HasMaxLength(100).IsRequired();
            e.Property(x => x.Sha256).HasMaxLength(64).IsRequired();
        });

        constructor.Entity<EtiquetaDeEstudio>(e =>
        {
            e.Property(x => x.Texto).HasMaxLength(EtiquetaDeEstudio.LargoMaximoTexto).IsRequired();
            e.Property(x => x.TextoNormalizado).HasMaxLength(EtiquetaDeEstudio.LargoMaximoTexto).IsRequired();
            e.HasIndex(x => x.TextoNormalizado);
        });

        constructor.Entity<IntentoDeInicioDeSesion>(e =>
        {
            e.Property(x => x.NombreDeUsuarioNormalizado).HasMaxLength(256).IsRequired();
            e.HasIndex(x => x.NombreDeUsuarioNormalizado).IsUnique();
        });

        AplicarConversorDeFechas(constructor);
        AplicarFiltrosDePropietario(constructor);
    }

    /// <summary>
    /// Recorre el modelo y le pone el filtro por propietario a toda entidad que implemente
    /// <see cref="IPropiedadDeUsuario"/>. Una entidad médica nueva queda aislada con solo implementar
    /// la interfaz, y <c>AislamientoPorPropietarioTests</c> falla si alguna queda afuera.
    /// </summary>
    private void AplicarFiltrosDePropietario(ModelBuilder constructor)
    {
        var metodo = typeof(ArchivoMedicoDbContext)
            .GetMethod(nameof(AplicarFiltroDePropietario), BindingFlags.NonPublic | BindingFlags.Instance)!;

        foreach (var tipo in constructor.Model.GetEntityTypes())
        {
            if (tipo.ClrType is { } clr && typeof(IPropiedadDeUsuario).IsAssignableFrom(clr))
                metodo.MakeGenericMethod(clr).Invoke(this, [constructor]);
        }
    }

    private void AplicarFiltroDePropietario<T>(ModelBuilder constructor) where T : class, IPropiedadDeUsuario =>
        constructor.Entity<T>().HasQueryFilter(e => e.OwnerId == _usuarioActual.Id);

    private static void AplicarConversorDeFechas(ModelBuilder constructor)
    {
        var conversor = new ValueConverter<DateTimeOffset, long>(
            v => v.UtcTicks,
            v => new DateTimeOffset(v, TimeSpan.Zero));

        foreach (var tipo in constructor.Model.GetEntityTypes())
        foreach (var propiedad in tipo.GetProperties())
        {
            if (propiedad.ClrType == typeof(DateTimeOffset) || propiedad.ClrType == typeof(DateTimeOffset?))
                propiedad.SetValueConverter(conversor);
        }
    }

    public override int SaveChanges(bool aceptarTodo)
    {
        PrepararEntidades();
        return base.SaveChanges(aceptarTodo);
    }

    public override Task<int> SaveChangesAsync(bool aceptarTodo, CancellationToken cancelacion = default)
    {
        PrepararEntidades();
        return base.SaveChangesAsync(aceptarTodo, cancelacion);
    }

    /// <summary>
    /// Estampa el propietario en las entidades nuevas y recalcula las columnas normalizadas. Ningún
    /// camino de escritura puede saltearlo, y por lo tanto tampoco hay que replicarlo en los controladores.
    /// </summary>
    private void PrepararEntidades()
    {
        foreach (var entrada in ChangeTracker.Entries())
        {
            if (entrada.State is not (EntityState.Added or EntityState.Modified)) continue;

            if (entrada.State == EntityState.Added && entrada.Entity is IPropiedadDeUsuario propiedad
                && string.IsNullOrEmpty(propiedad.OwnerId))
            {
                propiedad.OwnerId = _usuarioActual.Id
                    ?? throw new InvalidOperationException(
                        "No hay usuario en la sesión: no se puede estampar el propietario de una entidad médica.");
            }

            if (entrada.Entity is ITieneColumnasNormalizadas normalizable)
                normalizable.RecalcularNormalizados();

            if (entrada.State == EntityState.Added && entrada.Entity is Estudio estudio && estudio.CreadoEn == default)
                estudio.CreadoEn = _reloj.GetUtcNow();

            if (entrada.State == EntityState.Added && entrada.Entity is ArchivoDeEstudio archivo && archivo.CargadoEn == default)
                archivo.CargadoEn = _reloj.GetUtcNow();
        }
    }
}
