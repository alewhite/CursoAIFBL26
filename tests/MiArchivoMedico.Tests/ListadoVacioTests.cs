using MiArchivoMedico.Tests.Apoyo;

namespace MiArchivoMedico.Tests;

/// <summary>
/// Dos situaciones muy distintas llegan al mismo listado vacío, y el usuario necesita distinguirlas:
/// la cuenta recién creada y la búsqueda sin resultados (RF-39, AC-93, AC-94).
/// </summary>
public class ListadoVacioTests
{
    private static readonly DateOnly Hoy = DateOnly.FromDateTime(AplicacionDePrueba.MomentoInicial.UtcDateTime);

    [Fact(DisplayName = "AC-93: una cuenta sin estudios ve el mensaje de bienvenida y la acción de crear el primero")]
    public async Task CuentaSinEstudios_InvitaACrearElPrimero()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();

        var html = await (await cliente.GetAsync("/Estudios")).Content.ReadAsStringAsync();

        Assert.Contains("data-estado=\"sin-estudios\"", html, StringComparison.Ordinal);
        Assert.Contains("/Estudios/Crear", html, StringComparison.Ordinal);
        Assert.DoesNotContain("data-estado=\"sin-resultados\"", html, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "AC-94: una búsqueda sin coincidencias informa cero resultados y ofrece limpiar los filtros")]
    public async Task BusquedaSinResultados_OfreceLimpiarFiltros()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();
        await cliente.CrearEstudioAsync("Ecografía abdominal", Hoy);

        var html = await (await cliente.BuscarAsync("resonancia")).Content.ReadAsStringAsync();

        Assert.Contains("data-estado=\"sin-resultados\"", html, StringComparison.Ordinal);
        Assert.Contains("LimpiarFiltros", html, StringComparison.Ordinal);
        Assert.Contains("0", html, StringComparison.Ordinal);
        Assert.DoesNotContain("data-estado=\"sin-estudios\"", html, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "RNF-53: ningún estado vacío revela la existencia de estudios de otra cuenta")]
    public async Task EstadosVacios_NoRevelanOtrasCuentas()
    {
        using var aplicacion = new AplicacionDePrueba();
        var deBruno = await aplicacion.ClienteAutenticadoAsync(AplicacionDePrueba.UsuarioDos);
        await deBruno.CrearEstudioAsync("Radiografía de Bruno", Hoy);

        var deAna = await aplicacion.ClienteAutenticadoAsync();
        var html = await (await deAna.GetAsync("/Estudios")).Content.ReadAsStringAsync();

        Assert.Contains("data-estado=\"sin-estudios\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Bruno", html, StringComparison.OrdinalIgnoreCase);
    }
}
