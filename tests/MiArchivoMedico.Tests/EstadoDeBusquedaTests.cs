using MiArchivoMedico.Tests.Apoyo;

namespace MiArchivoMedico.Tests;

/// <summary>
/// El criterio vive en la sesión del servidor y no en la dirección, porque el término de búsqueda es
/// un dato médico y la infraestructura registra las direcciones solicitadas (RNF-63, RF-40).
/// </summary>
public class EstadoDeBusquedaTests
{
    private static readonly DateOnly Hoy = DateOnly.FromDateTime(AplicacionDePrueba.MomentoInicial.UtcDateTime);

    [Fact(DisplayName = "AC-96: el término buscado no aparece en la dirección de ninguna solicitud")]
    public async Task TerminoBuscado_NoViajaEnLaDireccion()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();
        const string terminoUnico = "zxqvunicoirrepetible";

        await cliente.CrearEstudioAsync($"Estudio {terminoUnico}", Hoy);

        var busqueda = await cliente.BuscarAsync(terminoUnico);
        Assert.DoesNotContain(terminoUnico, busqueda.RequestMessage!.RequestUri!.ToString(), StringComparison.OrdinalIgnoreCase);

        // Y tampoco en el enlace de paginación ni en ningún otro de la página resultante.
        var html = await busqueda.Content.ReadAsStringAsync();
        foreach (var enlace in ExtraerDirecciones(html))
            Assert.DoesNotContain(terminoUnico, enlace, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "AC-95: el criterio se conserva al pasar de página y al volver del detalle")]
    public async Task Criterio_SeConservaAlPaginarYAlVolver()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();

        for (var i = 1; i <= 30; i++)
            await cliente.CrearEstudioAsync($"Ecografía número {i:D2}", Hoy);
        await cliente.CrearEstudioAsync("Análisis que no coincide", Hoy);

        await cliente.BuscarAsync("ecografía");

        var segundaPagina = await (await cliente.IrAPaginaAsync(2)).Content.ReadAsStringAsync();
        Assert.DoesNotContain("Análisis que no coincide", segundaPagina, StringComparison.Ordinal);

        // Vuelve al listado sin reenviar nada: el criterio sigue puesto.
        var alVolver = await (await cliente.GetAsync("/Estudios")).Content.ReadAsStringAsync();
        Assert.DoesNotContain("Análisis que no coincide", alVolver, StringComparison.Ordinal);
        Assert.Contains("Ecografía", alVolver, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "RF-40: cada sesión tiene su propio criterio, sin mezclarse con la de otra cuenta")]
    public async Task Criterio_EsPropioDeCadaSesion()
    {
        using var aplicacion = new AplicacionDePrueba();
        var deAna = await aplicacion.ClienteAutenticadoAsync();
        await deAna.CrearEstudioAsync("Ecografía de Ana", Hoy);
        await deAna.CrearEstudioAsync("Análisis de Ana", Hoy);
        await deAna.BuscarAsync("ecografía");

        var deBruno = await aplicacion.ClienteAutenticadoAsync(AplicacionDePrueba.UsuarioDos);
        await deBruno.CrearEstudioAsync("Ecografía de Bruno", Hoy);
        await deBruno.CrearEstudioAsync("Análisis de Bruno", Hoy);

        var listadoDeBruno = await (await deBruno.GetAsync("/Estudios")).Content.ReadAsStringAsync();

        // Bruno no heredó el filtro de Ana.
        Assert.Contains("Análisis de Bruno", listadoDeBruno, StringComparison.Ordinal);
    }

    private static IEnumerable<string> ExtraerDirecciones(string html) =>
        System.Text.RegularExpressions.Regex.Matches(html, """(?:href|action)="([^"]*)""")
            .Select(m => m.Groups[1].Value);
}
