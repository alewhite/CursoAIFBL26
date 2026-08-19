using MiArchivoMedico.Tests.Apoyo;

namespace MiArchivoMedico.Tests;

public class FormularioDeEstudioTests
{
    private static readonly DateOnly Hoy = DateOnly.FromDateTime(AplicacionDePrueba.MomentoInicial.UtcDateTime);

    [Fact(DisplayName = "AC-80: el error del título aparece junto al campo y el del archivo junto a ese archivo")]
    public async Task ErroresDeValidacion_AparecenJuntoASuOrigen()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();

        var html = await (await cliente.CrearEstudioAsync(string.Empty, Hoy, archivos:
            [("vacio.pdf", ArchivosFicticios.Vacio(), "application/pdf")])).Content.ReadAsStringAsync();

        var posicionDelCampo = html.IndexOf("name=\"Titulo\"", StringComparison.Ordinal);
        var errorDelTitulo = html.IndexOf("data-error-de=\"Titulo\"", StringComparison.Ordinal);
        var errorDelArchivo = html.IndexOf("data-error-de-archivo=\"vacio.pdf\"", StringComparison.Ordinal);

        Assert.True(posicionDelCampo >= 0, "No se encontró el campo título en la respuesta.");
        Assert.True(errorDelTitulo >= 0, "El error del título debe estar marcado junto a su campo.");
        Assert.True(errorDelArchivo >= 0, "El error del archivo debe estar marcado junto a ese archivo.");
    }

    [Fact(DisplayName = "AC-100: el formulario rechazado vuelve con los metadatos intactos y avisa por los archivos")]
    public async Task FormularioRechazado_ConservaLosMetadatos()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();

        var html = await (await cliente.CrearEstudioAsync(
            string.Empty, Hoy, profesional: "Dra. Rivas", institucion: "Hospital Central",
            descripcion: "control anual")).Content.ReadAsStringAsync();

        Assert.Contains("Dra. Rivas", html, StringComparison.Ordinal);
        Assert.Contains("Hospital Central", html, StringComparison.Ordinal);
        Assert.Contains("control anual", html, StringComparison.Ordinal);
        Assert.Contains("adjunt", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "AC-99: reenviar el mismo formulario no crea un segundo estudio")]
    public async Task DobleEnvio_CreaUnSoloEstudio()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();

        var formulario = await cliente.GetAsync("/Estudios/Crear");
        var html = await formulario.Content.ReadAsStringAsync();
        var token = ClienteDeSesion.ExtraerTokenAntifalsificacion(html);
        var marca = ClienteDeEstudios.ExtraerMarcaDeEnvio(html);

        await cliente.ReenviarCreacionAsync("Estudio enviado dos veces", Hoy, token, marca);
        await cliente.ReenviarCreacionAsync("Estudio enviado dos veces", Hoy, token, marca);

        Assert.Single(await aplicacion.EstudiosDeAsync(AplicacionDePrueba.UsuarioUno));
    }
}
