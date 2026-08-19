using System.Net;
using System.Text.Json;
using MiArchivoMedico.Tests.Infraestructura;

namespace MiArchivoMedico.Tests;

/// <summary>
/// Verifica el paquete de instalación de la PWA. Lo que AC-38 pide del sistema operativo —el ícono propio y
/// la ventana sin barra de direcciones— lo decide el navegador a partir del manifiesto: acá se verifica que
/// el manifiesto lo declare y que todo lo que referencia exista y sea alcanzable sin sesión.
/// </summary>
public class PwaTests : IAsyncLifetime
{
    private readonly AplicacionDePrueba _app = new();

    public Task InitializeAsync() => _app.InitializeAsync();

    public Task DisposeAsync() => _app.DisposeAsync();

    [Fact(DisplayName = "AC-38: el manifiesto se sirve sin sesión y declara una ventana propia")]
    public async Task Manifiesto_DeclaraLaInstalacionEnVentanaPropia()
    {
        var cliente = _app.CrearCliente();

        var respuesta = await cliente.GetAsync("/manifest.webmanifest");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("manifest+json", respuesta.Content.Headers.ContentType?.MediaType);

        var manifiesto = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync()).RootElement;

        // "standalone" es lo que hace que el navegador abra la aplicación sin su barra de direcciones.
        Assert.Equal("standalone", manifiesto.GetProperty("display").GetString());
        Assert.Equal("/", manifiesto.GetProperty("start_url").GetString());
        Assert.Equal("/", manifiesto.GetProperty("scope").GetString());
        Assert.False(string.IsNullOrWhiteSpace(manifiesto.GetProperty("name").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(manifiesto.GetProperty("short_name").GetString()));
    }

    [Fact(DisplayName = "AC-38: los iconos del manifiesto existen y cubren los tamaños que exige la instalación")]
    public async Task Iconos_ExistenYCubrenLosTamanosRequeridos()
    {
        var cliente = _app.CrearCliente();

        var manifiesto = JsonDocument
            .Parse(await cliente.GetStringAsync("/manifest.webmanifest"))
            .RootElement;
        var iconos = manifiesto.GetProperty("icons").EnumerateArray().ToList();

        var tamanos = iconos.Select(i => i.GetProperty("sizes").GetString()).ToList();
        Assert.Contains("192x192", tamanos);
        Assert.Contains("512x512", tamanos);
        Assert.Contains(iconos, i => i.GetProperty("purpose").GetString() == "maskable");

        foreach (var icono in iconos)
        {
            var ruta = icono.GetProperty("src").GetString()!;

            var respuesta = await cliente.GetAsync(ruta);

            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            Assert.Equal("image/png", respuesta.Content.Headers.ContentType?.MediaType);
            Assert.NotEmpty(await respuesta.Content.ReadAsByteArrayAsync());
        }
    }
}
