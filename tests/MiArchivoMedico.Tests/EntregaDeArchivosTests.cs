using System.Net;
using System.Security.Cryptography;
using MiArchivoMedico.Tests.Apoyo;

namespace MiArchivoMedico.Tests;

public class EntregaDeArchivosTests
{
    private static readonly DateOnly Hoy = DateOnly.FromDateTime(AplicacionDePrueba.MomentoInicial.UtcDateTime);

    [Fact(DisplayName = "AC-15: el archivo se muestra dentro de la aplicación sin que el usuario deba descargarlo")]
    public async Task Visualizacion_SeMuestraDentroDeLaAplicacion()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();
        var archivo = await SembrarArchivoAsync(aplicacion, cliente);

        var vista = await cliente.GetAsync($"/Archivos/Ver/{archivo}");
        var html = await vista.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, vista.StatusCode);
        Assert.Contains("<iframe", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("sandbox", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains($"/Archivos/Contenido/{archivo}", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "AC-26: la entrega lleva las cabeceras que impiden ejecutar contenido activo")]
    public async Task Entrega_LlevaCabecerasQueBloqueanContenidoActivo()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();
        var archivo = await SembrarArchivoAsync(aplicacion, cliente, ArchivosFicticios.PdfConJavaScript());

        var contenido = await cliente.GetAsync(await RutaDeContenidoAsync(cliente, archivo));

        Assert.Equal(HttpStatusCode.OK, contenido.StatusCode);
        Assert.Contains("nosniff", string.Join(" ", contenido.Headers.GetValues("X-Content-Type-Options")));

        var politica = string.Join(" ", contenido.Headers.GetValues("Content-Security-Policy"));
        Assert.Contains("sandbox", politica, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("script-src 'none'", politica, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "AC-16, AC-77: el archivo descargado tiene la misma huella que antes de cargarlo")]
    public async Task Descarga_ConservaLaHuella()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();

        var original = ArchivosFicticios.PdfValido();
        var huellaPrevia = Convert.ToHexString(SHA256.HashData(original)).ToLowerInvariant();

        var archivo = await SembrarArchivoAsync(aplicacion, cliente, original);

        // Se visualiza antes de descargar, para comprobar que ver el archivo no lo altera.
        await cliente.GetAsync(await RutaDeContenidoAsync(cliente, archivo));

        var descarga = await cliente.GetAsync(await RutaDeDescargaAsync(cliente, archivo));
        var bajado = await descarga.Content.ReadAsByteArrayAsync();

        Assert.Equal(HttpStatusCode.OK, descarga.StatusCode);
        Assert.Equal(huellaPrevia, Convert.ToHexString(SHA256.HashData(bajado)).ToLowerInvariant());
        Assert.Equal(original, bajado);
    }

    [Fact(DisplayName = "AC-16: la descarga usa el nombre original sanitizado")]
    public async Task Descarga_UsaElNombreOriginalSanitizado()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();
        var archivo = await SembrarArchivoAsync(aplicacion, cliente, nombre: "../../etc/passwd.pdf");

        var descarga = await cliente.GetAsync(await RutaDeDescargaAsync(cliente, archivo));

        var disposicion = descarga.Content.Headers.ContentDisposition?.ToString() ?? string.Empty;
        Assert.Contains("passwd.pdf", disposicion, StringComparison.Ordinal);
        Assert.DoesNotContain("..", disposicion, StringComparison.Ordinal);
        Assert.DoesNotContain("/etc/", disposicion, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "AC-78: ni el listado ni el detalle incluyen algo que descargue el contenido por su cuenta")]
    public async Task ListadoYDetalle_NoTransfierenContenido()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();
        await SembrarArchivoAsync(aplicacion, cliente);
        var estudio = (await aplicacion.EstudiosDeAsync(AplicacionDePrueba.UsuarioUno))[0];

        foreach (var ruta in new[] { "/Estudios", $"/Estudios/Detalle/{estudio.Id}" })
        {
            var html = await (await cliente.GetAsync(ruta)).Content.ReadAsStringAsync();

            Assert.DoesNotContain("<img", html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("<iframe", html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("<embed", html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("/Archivos/Contenido/", html, StringComparison.OrdinalIgnoreCase);
        }
    }

    internal static async Task<Guid> SembrarArchivoAsync(
        AplicacionDePrueba aplicacion, HttpClient cliente, byte[]? contenido = null, string nombre = "informe.pdf")
    {
        await cliente.CrearEstudioAsync("Estudio con archivo", Hoy, archivos:
            [(nombre, contenido ?? ArchivosFicticios.PdfValido(), "application/pdf")]);

        return (await aplicacion.EstudiosDeAsync(AplicacionDePrueba.UsuarioUno))[0].Archivos[0].Id;
    }

    internal static async Task<string> RutaDeContenidoAsync(HttpClient cliente, Guid archivo) =>
        ExtraerRuta(await (await cliente.GetAsync($"/Archivos/Ver/{archivo}")).Content.ReadAsStringAsync(), "Contenido");

    internal static async Task<string> RutaDeDescargaAsync(HttpClient cliente, Guid archivo) =>
        ExtraerRuta(await (await cliente.GetAsync($"/Archivos/Ver/{archivo}")).Content.ReadAsStringAsync(), "Descargar");

    private static string ExtraerRuta(string html, string accion)
    {
        var coincidencia = System.Text.RegularExpressions.Regex.Match(
            html, $"""/Archivos/{accion}/[0-9a-fA-F-]+\?t=[^"'\s]+""");
        Assert.True(coincidencia.Success, $"No se encontró la ruta de {accion} en la vista.");
        return System.Net.WebUtility.HtmlDecode(coincidencia.Value);
    }
}
