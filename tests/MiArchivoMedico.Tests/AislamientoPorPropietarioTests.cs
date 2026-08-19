using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MiArchivoMedico.Web.Data;
using MiArchivoMedico.Web.Dominio;

namespace MiArchivoMedico.Tests;

/// <summary>
/// Guarda la invariante más importante del sistema: que ninguna entidad médica quede sin el filtro
/// global por propietario. Si alguien agrega una entidad nueva y se olvida, esto falla acá y no en
/// producción (RNF-53).
/// </summary>
public class AislamientoPorPropietarioTests(AplicacionDePrueba aplicacion)
    : IClassFixture<AplicacionDePrueba>
{
    [Fact(DisplayName = "RNF-53: toda entidad que implementa IPropiedadDeUsuario tiene filtro global por propietario")]
    public void EntidadesDePropiedadDeUsuario_TodasTienenFiltroGlobal()
    {
        using var alcance = aplicacion.Services.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<ArchivoMedicoDbContext>();

        var sinFiltro = contexto.Model.GetEntityTypes()
            .Where(t => typeof(IPropiedadDeUsuario).IsAssignableFrom(t.ClrType))
            .Where(t => t.GetQueryFilter() is null)
            .Select(t => t.ClrType.Name)
            .ToList();

        Assert.True(sinFiltro.Count == 0,
            $"Estas entidades médicas quedaron sin filtro global por propietario: {string.Join(", ", sinFiltro)}");
    }

    [Fact(DisplayName = "RNF-53: las entidades médicas del modelo implementan IPropiedadDeUsuario")]
    public void EntidadesMedicas_ImplementanLaMarcaDePropiedad()
    {
        using var alcance = aplicacion.Services.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<ArchivoMedicoDbContext>();

        Type[] medicas = [typeof(Estudio), typeof(ArchivoDeEstudio), typeof(EtiquetaDeEstudio)];

        foreach (var tipo in medicas)
        {
            Assert.True(typeof(IPropiedadDeUsuario).IsAssignableFrom(tipo),
                $"{tipo.Name} contiene datos médicos y debe implementar IPropiedadDeUsuario.");
            Assert.NotNull(contexto.Model.FindEntityType(tipo)?.GetQueryFilter());
        }
    }

    [Fact(DisplayName = "RNF-53: sin sesión, una consulta sobre datos médicos devuelve vacío en lugar de todo")]
    public async Task ConsultaSinSesion_DevuelveVacio()
    {
        using var alcance = aplicacion.Services.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<ArchivoMedicoDbContext>();

        // El alcance no tiene solicitud HTTP, así que IUsuarioActual.Id es null.
        Assert.Empty(await contexto.Estudios.ToListAsync());
        Assert.Empty(await contexto.Archivos.ToListAsync());
        Assert.Empty(await contexto.Etiquetas.ToListAsync());
    }
}
