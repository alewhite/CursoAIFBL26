using MiArchivoMedico.Tests.Apoyo;

namespace MiArchivoMedico.Tests;

public class ListadoTests
{
    private static readonly DateOnly Hoy = DateOnly.FromDateTime(AplicacionDePrueba.MomentoInicial.UtcDateTime);

    [Fact(DisplayName = "AC-28: el listado ordena los estudios del más reciente al más antiguo")]
    public async Task Listado_OrdenaDelMasRecienteAlMasAntiguo()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();

        await cliente.CrearEstudioAsync("El mas antiguo", Hoy.AddYears(-3));
        await cliente.CrearEstudioAsync("El del medio", Hoy.AddYears(-1));
        await cliente.CrearEstudioAsync("El mas reciente", Hoy);

        var html = await (await cliente.GetAsync("/Estudios")).Content.ReadAsStringAsync();

        var reciente = html.IndexOf("El mas reciente", StringComparison.Ordinal);
        var medio = html.IndexOf("El del medio", StringComparison.Ordinal);
        var antiguo = html.IndexOf("El mas antiguo", StringComparison.Ordinal);

        Assert.True(reciente >= 0 && medio >= 0 && antiguo >= 0, "Faltan estudios en el listado.");
        Assert.True(reciente < medio, "El más reciente debe aparecer antes que el del medio.");
        Assert.True(medio < antiguo, "El del medio debe aparecer antes que el más antiguo.");
    }

    [Fact(DisplayName = "AC-54: con 26 estudios se muestran 25 y hay control para avanzar")]
    public async Task VeintiseisEstudios_MuestraVeinticincoYPermiteAvanzar()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();

        for (var i = 1; i <= 26; i++)
            await cliente.CrearEstudioAsync($"Estudio {i:D2}", Hoy);

        var html = await (await cliente.GetAsync("/Estudios")).Content.ReadAsStringAsync();

        Assert.Equal(25, ContarFilas(html));
        Assert.Contains("data-pagina-siguiente", html, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "AC-101: en la segunda página hay control para retroceder y la página está indicada")]
    public async Task SegundaPagina_PermiteRetrocederEIndicaLaPagina()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();

        for (var i = 1; i <= 26; i++)
            await cliente.CrearEstudioAsync($"Estudio {i:D2}", Hoy);

        var html = await (await cliente.IrAPaginaAsync(2)).Content.ReadAsStringAsync();

        Assert.Equal(1, ContarFilas(html));
        Assert.Contains("data-pagina-anterior", html, StringComparison.Ordinal);
        Assert.Contains("data-pagina-actual=\"2\"", html, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "RNF-27: con 25 estudios o menos no se ofrece navegación entre páginas")]
    public async Task UnaSolaPagina_NoOfreceNavegacion()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();

        for (var i = 1; i <= 5; i++)
            await cliente.CrearEstudioAsync($"Estudio {i:D2}", Hoy);

        var html = await (await cliente.GetAsync("/Estudios")).Content.ReadAsStringAsync();

        Assert.DoesNotContain("data-pagina-siguiente", html, StringComparison.Ordinal);
        Assert.DoesNotContain("data-pagina-anterior", html, StringComparison.Ordinal);
    }

    private static int ContarFilas(string html) =>
        System.Text.RegularExpressions.Regex.Matches(html, "data-estudio=").Count;
}
