using MiArchivoMedico.Tests.Apoyo;

namespace MiArchivoMedico.Tests;

public class BusquedaTests
{
    private static readonly DateOnly Hoy = DateOnly.FromDateTime(AplicacionDePrueba.MomentoInicial.UtcDateTime);

    [Fact(DisplayName = "AC-71: buscando «abdominal» aparece el estudio titulado «Ecografía abdominal»")]
    public Task PorTitulo_Encuentra() =>
        ComprobarAsync(titulo: "Ecografía abdominal", termino: "abdominal", esperado: "Ecografía abdominal");

    [Fact(DisplayName = "AC-72: buscando «rutina» aparece el estudio descripto como «control anual de rutina»")]
    public Task PorDescripcion_Encuentra() =>
        ComprobarAsync(descripcion: "control anual de rutina", termino: "rutina");

    [Fact(DisplayName = "AC-73: buscando «Rivas» aparece el estudio del profesional «Dra. Rivas»")]
    public Task PorProfesional_Encuentra() =>
        ComprobarAsync(profesional: "Dra. Rivas", termino: "Rivas");

    [Fact(DisplayName = "AC-29: buscando «Central» aparece el estudio de la institución «Hospital Central»")]
    public Task PorInstitucion_Encuentra() =>
        ComprobarAsync(institucion: "Hospital Central", termino: "Central");

    [Fact(DisplayName = "AC-30: buscando «cardiología» aparece el estudio etiquetado como «cardiología»")]
    public Task PorEtiqueta_Encuentra() =>
        ComprobarAsync(etiquetas: "cardiología", termino: "cardiología");

    [Fact(DisplayName = "AC-45: la búsqueda ignora mayúsculas y espacios sobrantes")]
    public Task IgnoraMayusculasYEspacios() =>
        ComprobarAsync(institucion: "Hospital Central", termino: "  hospital central  ");

    [Fact(DisplayName = "AC-46: la búsqueda ignora los acentos")]
    public Task IgnoraAcentos() =>
        ComprobarAsync(etiquetas: "cardiología", termino: "cardiologia");

    [Fact(DisplayName = "RF-16: un término que no coincide con nada no devuelve estudios")]
    public async Task TerminoSinCoincidencias_NoDevuelveNada()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();
        await cliente.CrearEstudioAsync("Ecografía abdominal", Hoy);

        var html = await (await cliente.BuscarAsync("resonancia")).Content.ReadAsStringAsync();

        Assert.DoesNotContain("Ecografía abdominal", html, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "RNF-53: la búsqueda no devuelve estudios de otra cuenta")]
    public async Task Busqueda_NoCruzaCuentas()
    {
        using var aplicacion = new AplicacionDePrueba();
        var deBruno = await aplicacion.ClienteAutenticadoAsync(AplicacionDePrueba.UsuarioDos);
        await deBruno.CrearEstudioAsync("Radiografía de Bruno", Hoy, institucion: "Hospital Central");

        var deAna = await aplicacion.ClienteAutenticadoAsync();
        await deAna.CrearEstudioAsync("Análisis de Ana", Hoy, institucion: "Hospital Central");

        var html = await (await deAna.BuscarAsync("central")).Content.ReadAsStringAsync();

        Assert.Contains("Análisis de Ana", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Radiografía de Bruno", html, StringComparison.Ordinal);
    }

    private static async Task ComprobarAsync(
        string titulo = "Estudio de prueba",
        string? profesional = null,
        string? institucion = null,
        string? descripcion = null,
        string? etiquetas = null,
        string termino = "",
        string? esperado = null)
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();

        await cliente.CrearEstudioAsync(titulo, Hoy, profesional, institucion, descripcion, etiquetas);
        await cliente.CrearEstudioAsync("Estudio que no debe aparecer", Hoy);

        var html = await (await cliente.BuscarAsync(termino)).Content.ReadAsStringAsync();

        Assert.Contains(esperado ?? titulo, html, StringComparison.Ordinal);
        Assert.DoesNotContain("Estudio que no debe aparecer", html, StringComparison.Ordinal);
    }
}
