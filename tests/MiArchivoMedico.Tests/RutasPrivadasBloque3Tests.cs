using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace MiArchivoMedico.Tests;

/// <summary>
/// Tests de Bloque 3: Vistas Razor, validaciones ViewModel e infraestructura MVC de presentación.
/// Ejercitan la capa de vistas HTML incluyendo FormTagHelper, validaciones DataAnnotations y
/// la infraestructura de autenticación sin datos médicos.
/// AC-03, AC-04, AC-05, AC-06, O3, O5, O6, FR-04, NFR-03.
/// </summary>
public class RutasPrivadasBloque3Tests : IDisposable
{
    private readonly AppFactory fabrica = new();

    /// <summary>
    /// Vista_De_Inicio_De_Sesion_Incluye_El_Token_De_Antiforgery — el HTML del GET login debe
    /// incluir un input hidden `__RequestVerificationToken` para proteger contra CSRF (O3, AC-03).
    /// </summary>
    [Fact]
    public async Task Vista_De_Inicio_De_Sesion_Incluye_El_Token_De_Antiforgery()
    {
        using var arranque = ConfiguracionDeAltas.Arranque(fabrica);
        using var cliente = AutenticacionDePruebas.ClienteSobreHttps(arranque, seguirRedirecciones: false);

        var respuesta = await cliente.GetAsync("/Cuenta/IniciarSesion");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var html = await respuesta.Content.ReadAsStringAsync();

        // Buscar el input __RequestVerificationToken
        var tokenMatch = Regex.Match(
            html,
            @"<input[^>]*name=""__RequestVerificationToken""[^>]*value=""([^""]*)""",
            RegexOptions.IgnoreCase);

        Assert.True(tokenMatch.Success, "El HTML debe incluir el input __RequestVerificationToken");
        Assert.NotEmpty(tokenMatch.Groups[1].Value);
    }

    /// <summary>
    /// La_Contrasena_No_Se_Renderiza_En_El_Rechazo — cuando un login falla, el HTML de la respuesta
    /// NO debe contener el valor de la contraseña enviada. Previene fugas de credenciales en logs,
    /// caché del navegador o respaldos de HTML (O6, AC-06, AC-09).
    /// </summary>
    [Fact]
    public async Task La_Contrasena_No_Se_Renderiza_En_El_Rechazo()
    {
        var ana = ConfiguracionDeAltas.Alta("ana");

        using var arranque = ConfiguracionDeAltas.Arranque(fabrica, ana);
        using var cliente = AutenticacionDePruebas.ClienteSobreHttps(arranque, seguirRedirecciones: false);

        // Paso 1: GET /Cuenta/IniciarSesion para obtener el token
        var respuestaGet = await cliente.GetAsync("/Cuenta/IniciarSesion");
        var htmlDelGet = await respuestaGet.Content.ReadAsStringAsync();

        var tokenMatch = Regex.Match(
            htmlDelGet,
            @"<input[^>]*name=""__RequestVerificationToken""[^>]*value=""([^""]*)""",
            RegexOptions.IgnoreCase);
        Assert.True(tokenMatch.Success, "No se encontró el token antiforgery");

        var token = tokenMatch.Groups[1].Value;

        // Paso 2: POST con credenciales incorrectas
        var contrasenaMala = "contrasena-incorrecta-prueba";
        var contenido = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("NombreDeUsuario", ana.UserName),
            new KeyValuePair<string, string>("Contrasena", contrasenaMala),
            new KeyValuePair<string, string>("__RequestVerificationToken", token),
        });

        var respuestaPost = await cliente.PostAsync("/Cuenta/IniciarSesion", contenido);

        Assert.Equal(HttpStatusCode.OK, respuestaPost.StatusCode);
        var htmlDelPost = await respuestaPost.Content.ReadAsStringAsync();

        // La contraseña NO debe aparecer en el HTML de respuesta
        Assert.DoesNotContain(contrasenaMala, htmlDelPost);
    }

    /// <summary>
    /// La_Privada_Ofrece_El_Formulario_De_Cierre_De_Sesion — la página privada debe incluir:
    ///   1. Un formulario POST a `/Cuenta/CerrarSesion`
    ///   2. La cabecera HTTP `Cache-Control: no-store` para evitar que el navegador cachee la página autenticada (O5, AC-06)
    ///   3. El nombre del usuario autenticado (FR-04)
    /// No contiene datos médicos ni datos de otras cuentas (invariante central).
    /// </summary>
    [Fact]
    public async Task La_Privada_Ofrece_El_Formulario_De_Cierre_De_Sesion()
    {
        var ana = ConfiguracionDeAltas.Alta("ana");

        using var arranque = ConfiguracionDeAltas.Arranque(fabrica, ana);
        using var cliente = AutenticacionDePruebas.ClienteSobreHttps(arranque, seguirRedirecciones: true);

        // Iniciar sesión
        var resultado = await AutenticacionDePruebas.IniciarSesionAsync(cliente, ana.UserName, ana.Password);
        Assert.Equal(HttpStatusCode.OK, resultado.Estado);

        // GET / (página privada)
        var respuesta = await cliente.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        // Verificar la cabecera Cache-Control
        Assert.True(
            respuesta.Headers.TryGetValues("Cache-Control", out var cacheControl),
            "La página privada debe incluir Cache-Control");
        var cacheControlValue = cacheControl?.FirstOrDefault();
        Assert.NotNull(cacheControlValue);
        Assert.Contains("no-store", cacheControlValue);

        var html = await respuesta.Content.ReadAsStringAsync();

        // Verificar que contiene el nombre del usuario
        Assert.Contains(ana.UserName, html);

        // Verificar que contiene un formulario POST a /Cuenta/CerrarSesion
        var formMatch = Regex.Match(
            html,
            @"<form[^>]*method=""post""[^>]*action=""[^""]*CerrarSesion""",
            RegexOptions.IgnoreCase);
        Assert.True(formMatch.Success, "Debe haber un formulario POST a /Cuenta/CerrarSesion");

        // Verificar que contiene el token antiforgery (automático del FormTagHelper)
        var tokenMatch = Regex.Match(
            html,
            @"<input[^>]*name=""__RequestVerificationToken""",
            RegexOptions.IgnoreCase);
        Assert.True(tokenMatch.Success, "El formulario debe incluir el token antiforgery");
    }

    public void Dispose()
    {
        fabrica?.Dispose();
    }
}
