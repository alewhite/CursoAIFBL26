using MiArchivoMedico.Tests.Apoyo;

namespace MiArchivoMedico.Tests;

public class FiltrosTests
{
    private static readonly DateOnly Hoy = DateOnly.FromDateTime(AplicacionDePrueba.MomentoInicial.UtcDateTime);

    [Fact(DisplayName = "AC-31: el filtro por rango de fechas devuelve solo los estudios del rango")]
    public async Task RangoDeFechas_DevuelveSoloLosDelRango()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();

        await cliente.CrearEstudioAsync("De hace tres años", Hoy.AddYears(-3));
        await cliente.CrearEstudioAsync("Del año pasado", Hoy.AddYears(-1));
        await cliente.CrearEstudioAsync("De este año", Hoy);

        var html = await (await cliente.FiltrarAsync(
            desde: Hoy.AddYears(-2), hasta: Hoy)).Content.ReadAsStringAsync();

        Assert.Contains("Del año pasado", html, StringComparison.Ordinal);
        Assert.Contains("De este año", html, StringComparison.Ordinal);
        Assert.DoesNotContain("De hace tres años", html, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "AC-31: el rango incluye los estudios fechados exactamente en cada extremo")]
    public async Task RangoDeFechas_IncluyeLosExtremos()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();

        await cliente.CrearEstudioAsync("Justo al inicio", Hoy.AddDays(-10));
        await cliente.CrearEstudioAsync("Justo al final", Hoy);

        var html = await (await cliente.FiltrarAsync(
            desde: Hoy.AddDays(-10), hasta: Hoy)).Content.ReadAsStringAsync();

        Assert.Contains("Justo al inicio", html, StringComparison.Ordinal);
        Assert.Contains("Justo al final", html, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "AC-34: el filtro por institución devuelve solo los estudios de esa institución")]
    public async Task PorInstitucion_DevuelveSoloEsa()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();

        await cliente.CrearEstudioAsync("En el Central", Hoy, institucion: "Hospital Central");
        await cliente.CrearEstudioAsync("En la Clínica", Hoy, institucion: "Clínica del Sur");

        var html = await (await cliente.FiltrarAsync(institucion: "Hospital Central")).Content.ReadAsStringAsync();

        Assert.Contains("En el Central", html, StringComparison.Ordinal);
        Assert.DoesNotContain("En la Clínica", html, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "AC-35: la búsqueda combinada con el filtro devuelve solo lo que cumple ambas")]
    public async Task BusquedaYFiltro_SeCombinan()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();

        await cliente.CrearEstudioAsync("Ecografía en el Central", Hoy, institucion: "Hospital Central");
        await cliente.CrearEstudioAsync("Ecografía en la Clínica", Hoy, institucion: "Clínica del Sur");
        await cliente.CrearEstudioAsync("Análisis en el Central", Hoy, institucion: "Hospital Central");

        var html = await (await cliente.BuscarAsync("ecografía", "Hospital Central")).Content.ReadAsStringAsync();

        Assert.Contains("Ecografía en el Central", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Ecografía en la Clínica", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Análisis en el Central", html, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "AC-92: la lista de instituciones del filtro no incluye las de otra cuenta")]
    public async Task ListaDeInstituciones_NoCruzaCuentas()
    {
        using var aplicacion = new AplicacionDePrueba();
        var deBruno = await aplicacion.ClienteAutenticadoAsync(AplicacionDePrueba.UsuarioDos);
        await deBruno.CrearEstudioAsync("De Bruno", Hoy, institucion: "Sanatorio Norte");

        var deAna = await aplicacion.ClienteAutenticadoAsync();
        await deAna.CrearEstudioAsync("De Ana", Hoy, institucion: "Hospital Central");

        var html = await (await deAna.GetAsync("/Estudios")).Content.ReadAsStringAsync();

        Assert.Contains("Hospital Central", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Sanatorio Norte", html, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "AC-36: limpiar los filtros restablece el listado completo")]
    public async Task LimpiarFiltros_RestableceElListado()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();

        await cliente.CrearEstudioAsync("En el Central", Hoy, institucion: "Hospital Central");
        await cliente.CrearEstudioAsync("En la Clínica", Hoy, institucion: "Clínica del Sur");

        await cliente.FiltrarAsync(institucion: "Hospital Central");
        var html = await (await cliente.LimpiarFiltrosAsync()).Content.ReadAsStringAsync();

        Assert.Contains("En el Central", html, StringComparison.Ordinal);
        Assert.Contains("En la Clínica", html, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "AC-36: limpiar los filtros también borra el término de búsqueda")]
    public async Task LimpiarFiltros_BorraElTermino()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();

        await cliente.CrearEstudioAsync("Ecografía", Hoy);
        await cliente.CrearEstudioAsync("Análisis", Hoy);

        await cliente.BuscarAsync("ecografía");
        var html = await (await cliente.LimpiarFiltrosAsync()).Content.ReadAsStringAsync();

        Assert.Contains("Análisis", html, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "AC-37: el listado informa cuántos estudios se encontraron")]
    public async Task Contador_InformaLosResultados()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();

        for (var i = 1; i <= 5; i++)
            await cliente.CrearEstudioAsync($"Ecografía número {i}", Hoy);
        await cliente.CrearEstudioAsync("Análisis aparte", Hoy);

        var html = await (await cliente.BuscarAsync("ecografía")).Content.ReadAsStringAsync();

        Assert.Contains("5", ExtraerContador(html), StringComparison.Ordinal);
    }

    private static string ExtraerContador(string html)
    {
        var marca = html.IndexOf("data-contador", StringComparison.Ordinal);
        Assert.True(marca >= 0, "No se encontró el contador de resultados en el listado.");
        var cierre = html.IndexOf('>', marca);
        var fin = html.IndexOf("</", cierre, StringComparison.Ordinal);
        return html[(cierre + 1)..fin];
    }
}
