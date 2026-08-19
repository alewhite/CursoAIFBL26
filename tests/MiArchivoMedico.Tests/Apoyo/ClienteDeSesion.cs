using System.Net;
using System.Text.RegularExpressions;

namespace MiArchivoMedico.Tests.Apoyo;

/// <summary>
/// Extensiones de HttpClient que resuelven el formulario y el token antifalsificación. Las pruebas
/// ejercitan el HTTP real, no los controladores, así que conviene reusar esto en lugar de armar el
/// POST a mano.
/// </summary>
public static partial class ClienteDeSesion
{
    /// <summary>
    /// El cliente habla https porque la cookie de autenticación se emite con Secure (RNF-11) y un
    /// contenedor de cookies no la devolvería sobre http. No hay TLS real: lo resuelve el servidor
    /// de pruebas.
    /// </summary>
    public static HttpClient CrearClienteSinRedirecciones(this AplicacionDePrueba aplicacion) =>
        aplicacion.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
            BaseAddress = new Uri("https://localhost"),
        });

    public static HttpClient CrearCliente(this AplicacionDePrueba aplicacion) =>
        aplicacion.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
        });

    public static async Task<HttpResponseMessage> IniciarSesionAsync(
        this HttpClient cliente, string usuario, string contrasena)
    {
        var formulario = await cliente.GetAsync("/Cuenta/InicioDeSesion");
        var token = ExtraerTokenAntifalsificacion(await formulario.Content.ReadAsStringAsync());

        var campos = new Dictionary<string, string>
        {
            ["NombreDeUsuario"] = usuario,
            ["Contrasena"] = contrasena,
        };
        if (token is not null) campos["__RequestVerificationToken"] = token;

        return await cliente.PostAsync("/Cuenta/InicioDeSesion", new FormUrlEncodedContent(campos));
    }

    public static async Task<HttpResponseMessage> CerrarSesionAsync(this HttpClient cliente)
    {
        var pagina = await cliente.GetAsync("/Estudios");
        var token = ExtraerTokenAntifalsificacion(await pagina.Content.ReadAsStringAsync());

        var campos = new Dictionary<string, string>();
        if (token is not null) campos["__RequestVerificationToken"] = token;

        return await cliente.PostAsync("/Cuenta/CerrarSesion", new FormUrlEncodedContent(campos));
    }

    public static bool RedirigeAlIngreso(this HttpResponseMessage respuesta) =>
        respuesta.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Found
        && respuesta.Headers.Location?.OriginalString.Contains("InicioDeSesion", StringComparison.OrdinalIgnoreCase) == true;

    public static string? ExtraerTokenAntifalsificacion(string html) =>
        ExpresionDelToken().Match(html) is { Success: true } coincidencia
            ? coincidencia.Groups["valor"].Value
            : null;

    [GeneratedRegex("""name="__RequestVerificationToken"[^>]*value="(?<valor>[^"]+)""")]
    private static partial Regex ExpresionDelToken();
}
