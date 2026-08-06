using MiArchivoMedico.Tests.Infraestructura;
using MiArchivoMedico.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MiArchivoMedico.Tests;

/// <summary>
/// Guarda del filtro global por propietario (RNF-53). Hoy no hay entidades médicas —llegan en la
/// Feature 2—; el test falla en cuanto se agregue una que implemente <see cref="IPropiedadDeUsuario"/>
/// sin quedar filtrada.
/// </summary>
public class AislamientoPorPropietarioTests : IAsyncLifetime
{
    private readonly AplicacionDePrueba _app = new();

    public Task InitializeAsync() => _app.InitializeAsync();

    public Task DisposeAsync() => _app.DisposeAsync();

    [Fact(DisplayName = "RNF-53: toda entidad con propietario tiene filtro global por OwnerId")]
    public async Task TodaEntidadConPropietario_TieneFiltroGlobal()
    {
        await _app.EnAlcanceAsync(servicios =>
        {
            var contexto = servicios.GetRequiredService<ArchivoMedicoDbContext>();

            var sinFiltro = contexto.Model.GetEntityTypes()
                .Where(tipo => typeof(IPropiedadDeUsuario).IsAssignableFrom(tipo.ClrType))
                .Where(tipo => tipo.GetQueryFilter() is null)
                .Select(tipo => tipo.ClrType.Name)
                .ToList();

            Assert.True(
                sinFiltro.Count == 0,
                $"Entidades con propietario sin filtro global: {string.Join(", ", sinFiltro)}.");

            return Task.CompletedTask;
        });
    }
}
