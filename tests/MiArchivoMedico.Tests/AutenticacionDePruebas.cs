using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MiArchivoMedico.Tests;

/// <summary>
/// Helper para autenticación en tests. Proporciona utilidades compartidas para login, logout y
/// manejo de sesiones de prueba (Bloques 2 y 4).
/// </summary>
internal static class AutenticacionDePruebas
{
    /// <summary>
    /// Crea un cliente HTTP que sigue redirecciones y confía en certificados autofirmados HTTPS.
    /// BaseAddress es "https://localhost" para que las cookies Secure viajen correctamente (NFR-01).
    /// </summary>
    internal static HttpClient ClienteSobreHttps(WebApplicationFactory<Program> arranque, bool seguirRedirecciones)
    {
        var cliente = arranque.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = seguirRedirecciones,
            HandleCookies = true,
        });

        cliente.BaseAddress = new Uri("https://localhost");
        return cliente;
    }

    /// <summary>
    /// Realiza un login en el sistema. Ejecuta:
    ///   1. GET /Cuenta/IniciarSesion para obtener el token antiforgery
    ///   2. POST /Cuenta/IniciarSesion con credenciales y token
    /// Captura el estado, la ubicación de redirección y las cookies emitidas.
    /// </summary>
    internal static async Task<ResultadoDeInicioDeSesion> IniciarSesionAsync(
        HttpClient cliente, string usuario, string contrasena)
    {
        // Paso 1: GET /Cuenta/IniciarSesion para obtener el token
        var respuestaGet = await cliente.GetAsync("/Cuenta/IniciarSesion");
        var htmlDelGet = await respuestaGet.Content.ReadAsStringAsync();

        var tokenMatch = Regex.Match(htmlDelGet, @"<input[^>]*name=""__RequestVerificationToken""[^>]*value=""([^""]*)""", RegexOptions.IgnoreCase);
        if (!tokenMatch.Success)
        {
            throw new InvalidOperationException("No se encontró el token antiforgery en la página de login.");
        }

        var token = tokenMatch.Groups[1].Value;

        // Paso 2: POST /Cuenta/IniciarSesion con credenciales y token
        var contenido = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("NombreDeUsuario", usuario),
            new KeyValuePair<string, string>("Contrasena", contrasena),
            new KeyValuePair<string, string>("__RequestVerificationToken", token),
        });

        var respuestaPost = await cliente.PostAsync("/Cuenta/IniciarSesion", contenido);

        // Capturar cookies y ubicación
        var setCookieHeaders = respuestaPost.Headers.TryGetValues("Set-Cookie", out var cookies)
            ? cookies.ToList()
            : new List<string>();

        var ubicacion = respuestaPost.Headers.Location?.ToString();

        return new ResultadoDeInicioDeSesion(respuestaPost.StatusCode, ubicacion, setCookieHeaders);
    }

    /// <summary>Resultado de un intento de inicio de sesión.</summary>
    internal sealed record ResultadoDeInicioDeSesion(
        HttpStatusCode Estado,
        string? Ubicacion,
        IReadOnlyList<string> CookiesEmitidas);

    /// <summary>
    /// Crea un cliente HTTP autenticado con las altas declaradas (por nombre de usuario).
    /// El cliente sigue redirecciones y mantiene cookies de sesión.
    /// </summary>
    internal static async Task<HttpClient> CrearClienteAutenticadoAsync(
        AppFactory fabrica, params string[] usuariosAAutenticar)
    {
        // Construir las altas a partir de los nombres de usuario
        var altas = usuariosAAutenticar.Select(u => ConfiguracionDeAltas.Alta(u)).ToArray();

        // Usar una fábrica para cada sesión de autenticación
        using var arranque = ConfiguracionDeAltas.Arranque(fabrica, altas);

        var cliente = ClienteSobreHttps(arranque, seguirRedirecciones: true);

        // Logear con la primera cuenta
        var primeraAlta = altas.First();
        var resultado = await IniciarSesionAsync(cliente, primeraAlta.UserName, primeraAlta.Password);

        if (resultado.Estado != HttpStatusCode.Found && resultado.Estado != HttpStatusCode.OK)
        {
            throw new InvalidOperationException($"El login falló con estado {resultado.Estado}.");
        }

        return cliente;
    }
}
