using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
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

    [Fact(DisplayName = "AC-40: la pantalla sin conexión se resuelve sin sesión y no contiene datos médicos")]
    public async Task PantallaSinConexion_SeSirveSinSesionYSinDatosMedicos()
    {
        var cliente = _app.CrearCliente();

        var respuesta = await cliente.GetAsync("/sin-conexion");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        var html = await respuesta.Content.ReadAsStringAsync();
        Assert.Contains("Sin conexión", html);
        Assert.Contains("No hay conexión a internet", html);

        // No enlaza ni nombra nada de la aplicación privada: es una pantalla terminal, no una portada.
        Assert.DoesNotContain("/Estudios", html);
        Assert.DoesNotContain("/Archivos", html);
    }

    [Fact(DisplayName = "AC-41: la pantalla sin conexión es idéntica con sesión y sin ella")]
    public async Task PantallaSinConexion_NoDependeDeLaSesion()
    {
        var anonimo = _app.CrearCliente();
        var autenticado = _app.CrearCliente();
        await autenticado.IniciarSesionAsync(AplicacionDePrueba.Usuario, AplicacionDePrueba.Contrasena);

        var sinSesion = await anonimo.GetStringAsync("/sin-conexion");
        var conSesion = await autenticado.GetStringAsync("/sin-conexion");

        // El service worker guarda una sola copia y la muestra a quien sea: si la página variara según la
        // sesión, esa copia podría exponer algo de la cuenta que la guardó.
        Assert.Equal(sinSesion, conSesion);
    }

    [Fact(DisplayName = "AC-41: el service worker solo precarga recursos que se resuelven sin sesión")]
    public async Task ServiceWorker_SoloPrecargaRecursosAnonimos()
    {
        var cliente = _app.CrearCliente();

        var codigo = await cliente.GetStringAsync("/sw.js");
        var precargados = ExtraerListaDeEstaticos(codigo);

        Assert.NotEmpty(precargados);
        Assert.Contains("/sin-conexion", precargados);

        foreach (var ruta in precargados)
        {
            var respuesta = await cliente.GetAsync(ruta);

            // Una ruta privada acá respondería con la redirección al inicio de sesión, y eso significaría
            // que el service worker está por guardar en el navegador algo que exige autenticación.
            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        }
    }

    /// <summary>Lee la lista ESTATICOS del service worker tal como la ejecutaría el navegador.</summary>
    private static List<string> ExtraerListaDeEstaticos(string codigo)
    {
        var lista = Regex.Match(codigo, @"const ESTATICOS = \[(?<cuerpo>.*?)\];", RegexOptions.Singleline);
        Assert.True(lista.Success, "El service worker ya no declara la lista ESTATICOS.");

        return Regex.Matches(lista.Groups["cuerpo"].Value, @"'(?<ruta>[^']+)'")
            .Select(m => m.Groups["ruta"].Value)
            .Concat(Regex.IsMatch(lista.Groups["cuerpo"].Value, @"\bPANTALLA_SIN_CONEXION\b")
                ? ["/sin-conexion"]
                : Array.Empty<string>())
            .Distinct()
            .ToList();
    }
}
