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
}
