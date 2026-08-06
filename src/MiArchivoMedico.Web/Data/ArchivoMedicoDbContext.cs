using System.Reflection;
using MiArchivoMedico.Web.Security;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MiArchivoMedico.Web.Data;

public class ArchivoMedicoDbContext : IdentityDbContext<UsuarioApp>
{
    private static readonly MethodInfo MetodoAplicarFiltro =
        typeof(ArchivoMedicoDbContext).GetMethod(
            nameof(AplicarFiltroDePropietario),
            BindingFlags.Instance | BindingFlags.NonPublic)!;

    private readonly IUsuarioActual _usuarioActual;

    public ArchivoMedicoDbContext(
        DbContextOptions<ArchivoMedicoDbContext> options,
        IUsuarioActual usuarioActual)
        : base(options)
    {
        _usuarioActual = usuarioActual;
    }

    public DbSet<Estudio> Estudios => Set<Estudio>();

    public DbSet<EtiquetaDeEstudio> Etiquetas => Set<EtiquetaDeEstudio>();

    public DbSet<ArchivoDeEstudio> Archivos => Set<ArchivoDeEstudio>();

    public override int SaveChanges()
    {
        PrepararEntidades();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        PrepararEntidades();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // SQLite no sabe comparar DateTimeOffset en un ORDER BY, y el listado ordena por fecha de carga
        // (RF-15). Se persisten como ticks UTC, que sí ordenan, sin perder precisión.
        var aTicksUtc = new ValueConverter<DateTimeOffset, long>(
            valor => valor.UtcTicks,
            ticks => new DateTimeOffset(ticks, TimeSpan.Zero));

        modelBuilder.Entity<Estudio>(entidad =>
        {
            entidad.Property(e => e.CreadoUtc).HasConversion(aTicksUtc);
            entidad.Property(e => e.Titulo).IsRequired().HasMaxLength(200);
            entidad.Property(e => e.Profesional).HasMaxLength(200);
            entidad.Property(e => e.Institucion).HasMaxLength(200);
            entidad.Property(e => e.Descripcion).HasMaxLength(2000);

            entidad.HasMany(e => e.Etiquetas)
                .WithOne()
                .HasForeignKey(t => t.EstudioId)
                .OnDelete(DeleteBehavior.Cascade);

            entidad.HasMany(e => e.Archivos)
                .WithOne(a => a.Estudio!)
                .HasForeignKey(a => a.EstudioId)
                .OnDelete(DeleteBehavior.Cascade);

            // El listado ordena por fecha descendente (RF-15); los índices normalizados sostienen la
            // búsqueda por subcadena del volumen previsto (RNF-24, RNF-55).
            entidad.HasIndex(e => new { e.OwnerId, e.Fecha });
            entidad.HasIndex(e => e.TituloNormalizado);
            entidad.HasIndex(e => e.ProfesionalNormalizado);
            entidad.HasIndex(e => e.InstitucionNormalizada);
            entidad.HasIndex(e => e.DescripcionNormalizada);
        });

        modelBuilder.Entity<EtiquetaDeEstudio>(entidad =>
        {
            entidad.Property(t => t.Texto).IsRequired().HasMaxLength(100);
            entidad.HasIndex(t => t.TextoNormalizado);
        });

        modelBuilder.Entity<ArchivoDeEstudio>(entidad =>
        {
            entidad.Property(a => a.CargadoUtc).HasConversion(aTicksUtc);
            entidad.Property(a => a.NombreOriginal).IsRequired().HasMaxLength(255);
            entidad.Property(a => a.TipoMime).IsRequired().HasMaxLength(100);
            entidad.Property(a => a.HashSha256).IsRequired().HasMaxLength(64);
        });

        // Toda entidad con propietario queda filtrada por el usuario autenticado sin que cada consulta
        // tenga que recordarlo (RNF-53). Alcanza con implementar IPropiedadDeUsuario: la convención se
        // encarga del resto, y AislamientoPorPropietarioTests falla si alguna queda afuera.
        foreach (var tipo in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(IPropiedadDeUsuario).IsAssignableFrom(tipo.ClrType))
            {
                MetodoAplicarFiltro.MakeGenericMethod(tipo.ClrType).Invoke(this, [modelBuilder]);
            }
        }
    }

    private void AplicarFiltroDePropietario<TEntidad>(ModelBuilder modelBuilder)
        where TEntidad : class, IPropiedadDeUsuario
    {
        modelBuilder.Entity<TEntidad>().HasQueryFilter(e => e.OwnerId == _usuarioActual.Id);
    }

    /// <summary>
    /// Estampa el propietario en las entidades nuevas y recalcula las columnas normalizadas. Vive en
    /// <c>SaveChanges</c> para que ningún camino de escritura pueda salteárselo (RNF-53, RNF-55).
    /// </summary>
    private void PrepararEntidades()
    {
        foreach (var entrada in ChangeTracker.Entries<IPropiedadDeUsuario>())
        {
            if (entrada.State == EntityState.Added && string.IsNullOrEmpty(entrada.Entity.OwnerId))
            {
                entrada.Entity.OwnerId = _usuarioActual.Id
                    ?? throw new InvalidOperationException(
                        "No se puede guardar una entidad con propietario sin un usuario autenticado.");
            }
        }

        foreach (var entrada in ChangeTracker.Entries<Estudio>())
        {
            if (entrada.State is EntityState.Added or EntityState.Modified)
            {
                entrada.Entity.RecalcularNormalizados();
            }
        }

        foreach (var entrada in ChangeTracker.Entries<EtiquetaDeEstudio>())
        {
            if (entrada.State is EntityState.Added or EntityState.Modified)
            {
                entrada.Entity.RecalcularNormalizados();
            }
        }
    }
}
