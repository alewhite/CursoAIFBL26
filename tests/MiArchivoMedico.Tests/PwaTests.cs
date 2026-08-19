using System.Net;
using System.Text.RegularExpressions;
using MiArchivoMedico.Tests.Apoyo;

namespace MiArchivoMedico.Tests;

/// <summary>
/// La caché del navegador no toca la aplicación: el service worker guarda solo lo que declara su lista
/// ESTATICOS. Si alguna de esas entradas exigiera sesión, la copia guardada podría mostrar información
/// médica a quien no la tiene (RNF-51, AC-41).
/// </summary>
public partial class PwaTests(AplicacionDePrueba aplicacion) : IClassFixture<AplicacionDePrueba>
{
    [Fact(DisplayName = "RNF-51, AC-41: cada entrada de la lista ESTATICOS responde sin sesión")]
    public async Task EntradasDeLaCache_RespondenSinSesion()
    {
        var cliente = aplicacion.CrearClienteSinRedirecciones();

        var serviceWorker = await cliente.GetAsync("/sw.js");
        Assert.Equal(HttpStatusCode.OK, serviceWorker.StatusCode);

        var estaticos = ExtraerEstaticos(await serviceWorker.Content.ReadAsStringAsync());
        Assert.NotEmpty(estaticos);

        foreach (var entrada in estaticos)
        {
            var respuesta = await cliente.GetAsync(entrada);
            Assert.True(respuesta.StatusCode == HttpStatusCode.OK,
                $"«{entrada}» está en la lista de la caché y sin sesión respondió {respuesta.StatusCode}.");
        }
    }

    [Fact(DisplayName = "RNF-51: la lista ESTATICOS no incluye ninguna ruta de datos médicos")]
    public async Task ListaDeLaCache_NoIncluyeRutasDeLaAplicacion()
    {
        var cliente = aplicacion.CrearClienteSinRedirecciones();
        var estaticos = ExtraerEstaticos(await (await cliente.GetAsync("/sw.js")).Content.ReadAsStringAsync());

        string[] prohibidas = ["/Estudios", "/Archivos", "/Cuenta"];
        foreach (var entrada in estaticos)
        {
            Assert.DoesNotContain(prohibidas, p => entrada.StartsWith(p, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact(DisplayName = "AC-40: la pantalla sin conexión es anónima y no contiene estudios ni metadatos")]
    public async Task PantallaSinConexion_EsAnonimaYVacia()
    {
        var cliente = aplicacion.CrearClienteSinRedirecciones();

        var respuesta = await cliente.GetAsync("/sin-conexion");
        var html = await respuesta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("conexión", html, StringComparison.OrdinalIgnoreCase);
        // No usa el layout, para que la copia guardada sea idéntica para cualquiera.
        Assert.DoesNotContain("Cerrar sesión", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/Estudios", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "RF-24: el manifiesto declara ícono propio y ventana sin barra de direcciones")]
    public async Task Manifiesto_DeclaraLaAplicacionInstalable()
    {
        var cliente = aplicacion.CrearClienteSinRedirecciones();

        var respuesta = await cliente.GetAsync("/manifest.webmanifest");
        var manifiesto = await respuesta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("\"display\"", manifiesto, StringComparison.Ordinal);
        Assert.Contains("standalone", manifiesto, StringComparison.Ordinal);
        Assert.Contains("\"icons\"", manifiesto, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "RNF-51: el service worker nunca guarda una respuesta de la red")]
    public async Task ServiceWorker_NoGuardaRespuestasDeLaRed()
    {
        var cliente = aplicacion.CrearClienteSinRedirecciones();
        var codigo = await (await cliente.GetAsync("/sw.js")).Content.ReadAsStringAsync();

        // La única escritura admitida es la precarga de la lista ESTATICOS al instalarse.
        var escrituras = Regex.Matches(codigo, @"\.put\(|cache\.add\b").Count;
        Assert.True(escrituras == 0,
            "El service worker no debe escribir en la caché fuera de la precarga de ESTATICOS.");
    }

    private static IReadOnlyList<string> ExtraerEstaticos(string codigo)
    {
        var bloque = ExpresionDeEstaticos().Match(codigo);
        Assert.True(bloque.Success, "No se encontró la lista ESTATICOS en el service worker.");

        return Regex.Matches(bloque.Groups["lista"].Value, "['\"](?<ruta>[^'\"]+)['\"]")
            .Select(m => m.Groups["ruta"].Value)
            .ToList();
    }

    [GeneratedRegex(@"ESTATICOS\s*=\s*\[(?<lista>[^\]]*)\]")]
    private static partial Regex ExpresionDeEstaticos();
}
