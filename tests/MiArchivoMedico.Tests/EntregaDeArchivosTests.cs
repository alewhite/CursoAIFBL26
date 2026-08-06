using System.Net;
using MiArchivoMedico.Tests.Infraestructura;

namespace MiArchivoMedico.Tests;

/// <summary>Visualización, descarga y cifrado en reposo (RF-11, RF-12, RNF-02, RNF-18, RNF-20, RNF-28).</summary>
public class EntregaDeArchivosTests : IAsyncLifetime
{
    private readonly AplicacionDePrueba _app = new();

    public Task InitializeAsync() => _app.InitializeAsync();

    public Task DisposeAsync() => _app.DisposeAsync();

    [Fact(DisplayName = "AC-15: visualizar renderiza el archivo dentro de la aplicación")]
    public async Task Visualizar_RenderizaDentroDeLaAplicacion()
    {
        var (cliente, _, idDeArchivo) = await ConUnArchivoAsync(ArchivosFicticios.Pdf());

        var pagina = await cliente.GetAsync($"/Archivos/Ver/{idDeArchivo}");
        pagina.EnsureSuccessStatusCode();
        var html = await pagina.Content.ReadAsStringAsync();

        // El contenido queda incrustado en la página, sin exigir una descarga previa.
        Assert.Contains($"/Archivos/Contenido/{idDeArchivo}", html);
        Assert.Contains("<iframe", html);

        var contenido = await cliente.GetAsync($"/Archivos/Contenido/{idDeArchivo}");
        contenido.EnsureSuccessStatusCode();
        Assert.Equal("application/pdf", contenido.Content.Headers.ContentType!.MediaType);
    }

    [Fact(DisplayName = "AC-16, AC-77: el archivo descargado conserva el hash previo a la carga")]
    public async Task ElArchivoDescargado_ConservaElHash()
    {
        var original = ArchivosFicticios.Pdf("integridad verificable");
        var hashPrevio = ArchivosFicticios.HashDe(original);

        var (cliente, _, idDeArchivo) = await ConUnArchivoAsync(original);

        // Se visualiza antes de descargar: el recorrido completo de AC-77.
        (await cliente.GetAsync($"/Archivos/Contenido/{idDeArchivo}")).EnsureSuccessStatusCode();

        var descarga = await cliente.GetAsync($"/Archivos/Descargar/{idDeArchivo}");
        descarga.EnsureSuccessStatusCode();
        var recibido = await descarga.Content.ReadAsByteArrayAsync();

        Assert.Equal(hashPrevio, ArchivosFicticios.HashDe(recibido));
        Assert.Equal(original, recibido);
        Assert.Equal("attachment", descarga.Content.Headers.ContentDisposition!.DispositionType);
    }

    [Fact(DisplayName = "AC-26: un PDF con JavaScript se sirve sin permitir contenido activo")]
    public async Task UnPdfConJavaScript_SeSirveSinContenidoActivo()
    {
        var (cliente, _, idDeArchivo) = await ConUnArchivoAsync(ArchivosFicticios.PdfConJavaScript());

        var contenido = await cliente.GetAsync($"/Archivos/Contenido/{idDeArchivo}");
        contenido.EnsureSuccessStatusCode();

        // La CSP con 'sandbox' y sin tokens apaga scripts y plugins en el documento servido.
        var csp = Assert.Single(contenido.Headers.GetValues("Content-Security-Policy"));
        Assert.Contains("sandbox", csp);
        Assert.Contains("default-src 'none'", csp);
        Assert.Equal("nosniff", Assert.Single(contenido.Headers.GetValues("X-Content-Type-Options")));

        // Y el visor lo incrusta en un iframe con sandbox, que es la segunda barrera.
        var html = await (await cliente.GetAsync($"/Archivos/Ver/{idDeArchivo}")).Content.ReadAsStringAsync();
        Assert.Contains("sandbox", html);
    }

    [Fact(DisplayName = "AC-57: en disco los bytes no corresponden al archivo en claro")]
    public async Task EnDisco_LosBytesEstanCifrados()
    {
        var original = ArchivosFicticios.Pdf("contenido confidencial ficticio");
        var (_, _, idDeArchivo) = await ConUnArchivoAsync(original);

        var enDisco = await File.ReadAllBytesAsync(
            Path.Combine(_app.RutaDeAlmacenamiento, $"{idDeArchivo:N}.bin"));

        Assert.NotEqual(original, enDisco);

        // Ni el encabezado del PDF ni el texto en claro sobreviven en disco.
        Assert.False(
            enDisco.AsSpan().IndexOf("%PDF-"u8) >= 0,
            "El encabezado del PDF aparece en claro en el archivo almacenado.");
        Assert.False(
            enDisco.AsSpan().IndexOf("contenido confidencial ficticio"u8) >= 0,
            "El texto en claro aparece en el archivo almacenado.");
    }

    [Fact(DisplayName = "AC-78: abrir el detalle no transfiere el contenido de ningún archivo")]
    public async Task AbrirElDetalle_NoTransfiereContenido()
    {
        var original = ArchivosFicticios.Pdf("no se debe transferir");
        var (cliente, idDeEstudio, _) = await ConUnArchivoAsync(original);

        var listado = await (await cliente.GetAsync("/Estudios")).Content.ReadAsByteArrayAsync();
        var detalle = await (await cliente.GetAsync($"/Estudios/Detalle/{idDeEstudio}"))
            .Content.ReadAsByteArrayAsync();

        // Ninguna de las dos respuestas contiene el archivo: solo enlaces para pedirlo.
        Assert.False(listado.AsSpan().IndexOf("%PDF-"u8) >= 0);
        Assert.False(detalle.AsSpan().IndexOf("%PDF-"u8) >= 0);
        Assert.Contains("/Archivos/Ver/", System.Text.Encoding.UTF8.GetString(detalle));
    }

    [Fact(DisplayName = "AC-02: sin sesión, el archivo no se entrega")]
    public async Task SinSesion_ElArchivoNoSeEntrega()
    {
        var (_, _, idDeArchivo) = await ConUnArchivoAsync(ArchivosFicticios.Pdf());

        var anonimo = _app.CrearCliente();

        foreach (var ruta in new[] { "Contenido", "Descargar", "Ver" })
        {
            var respuesta = await anonimo.GetAsync($"/Archivos/{ruta}/{idDeArchivo}");

            Assert.True(
                respuesta.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Unauthorized
                    or HttpStatusCode.Forbidden,
                $"/Archivos/{ruta} respondió {(int)respuesta.StatusCode} a un anónimo.");
            Assert.NotEqual(HttpStatusCode.OK, respuesta.StatusCode);
        }
    }

    [Fact(DisplayName = "AC-84: la misma URL que sirvió a un autorizado no entrega nada sin sesión")]
    public async Task LaMismaUrl_NoEntregaSinSesion()
    {
        var (cliente, _, idDeArchivo) = await ConUnArchivoAsync(ArchivosFicticios.Pdf());
        var url = $"/Archivos/Descargar/{idDeArchivo}";

        (await cliente.GetAsync(url)).EnsureSuccessStatusCode();

        var anonimo = _app.CrearCliente();
        Assert.NotEqual(HttpStatusCode.OK, (await anonimo.GetAsync(url)).StatusCode);

        // Pasados 5 minutos tampoco: la URL nunca fue de acceso público, ni temporal ni permanente.
        _app.Reloj.Advance(TimeSpan.FromMinutes(6));
        Assert.NotEqual(HttpStatusCode.OK, (await anonimo.GetAsync(url)).StatusCode);
    }

    private async Task<(HttpClient Cliente, Guid IdDeEstudio, Guid IdDeArchivo)> ConUnArchivoAsync(
        byte[] contenido)
    {
        var cliente = _app.CrearCliente();
        await cliente.IniciarSesionAsync(AplicacionDePrueba.Usuario, AplicacionDePrueba.Contrasena);

        var idDeEstudio = await cliente.CrearYObtenerIdAsync(
            "Estudio con archivo",
            archivos: [new ArchivoAEnviar("informe.pdf", "application/pdf", contenido)]);

        var idDeArchivo = (await cliente.ObtenerIdsDeArchivosAsync(idDeEstudio)).Single();
        return (cliente, idDeEstudio, idDeArchivo);
    }
}
