using System.Reflection;
using MiArchivoMedico.Web.Security;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Toda entidad con propietario queda filtrada por el usuario autenticado sin que cada consulta
        // tenga que recordarlo (RNF-53). Hoy no hay ninguna: las entidades médicas llegan en la Feature 2,
        // y la convención las cubre en cuanto implementen IPropiedadDeUsuario.
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
}
