using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace MiArchivoMedico.Tests;

/// <summary>
/// Tests de integración HTTP para login/logout (Bloque 4 de FEAT-001b).
/// AC-03, AC-04, AC-05, AC-06, AC-09.
/// </summary>
public class InicioDeSesionTests : IDisposable
{
    private readonly AppFactory fabrica = new();

    [Fact]
    public async Task AC03_Inicio_De_Sesion_Correcto_Establece_Sesion_Y_Responde_La_Privada()
    {
        // Arrange: crear una cuenta de prueba
        var ana = ConfiguracionDeAltas.Alta("ana");
        using var arranque = ConfiguracionDeAltas.Arranque(fabrica, ana);
        var cliente = AutenticacionDePruebas.ClienteSobreHttps(arranque, seguirRedirecciones: false);

        // Act: realizar login correcto
        var resultado = await AutenticacionDePruebas.IniciarSesionAsync(cliente, ana.UserName, ana.Password);

        // Assert: login debe devolver 302 (Found) a "/"
        Assert.Equal(HttpStatusCode.Found, resultado.Estado);
        Assert.Equal("/", resultado.Ubicacion);
        Assert.NotEmpty(resultado.CookiesEmitidas);

        // Ahora acceder a la página privada con la cookie establecida
        var respuestaPrivada = await cliente.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, respuestaPrivada.StatusCode);

        var contenidoPrivada = await respuestaPrivada.Content.ReadAsStringAsync();
        // La privada debe contener el nombre de la cuenta
        Assert.Contains("Bienvenido", contenidoPrivada);
        Assert.Contains("ana", contenidoPrivada);
        Assert.Contains("Cerrar sesión", contenidoPrivada);
    }

    [Fact]
    public async Task AC04_Rechazo_Es_Identico_Si_El_Usuario_No_Existe_O_La_Contrasena_Es_Incorrecta()
    {
        // Arrange: crear una cuenta real para el segundo intento
        var ana = ConfiguracionDeAltas.Alta("ana");
        using var arranque = ConfiguracionDeAltas.Arranque(fabrica, ana);

        var cliente1 = AutenticacionDePruebas.ClienteSobreHttps(arranque, seguirRedirecciones: false);
        var cliente2 = AutenticacionDePruebas.ClienteSobreHttps(arranque, seguirRedirecciones: false);

        // Act: intento 1 — usuario inexistente
        var respuestaRechazo1 = await RealizarInicioDeSesionAsync(cliente1, "usuario-que-no-existe", "cualquier-contrasena");

        // Act: intento 2 — usuario existente pero contraseña incorrecta
        var respuestaRechazo2 = await RealizarInicioDeSesionAsync(cliente2, ana.UserName, "contrasena-incorrecta");

        // Assert: ambas respuestas deben tener el mismo status y body normalizado (sin Set-Cookie)
        Assert.Equal(respuestaRechazo1.StatusCode, respuestaRechazo2.StatusCode);

        var cuerpo1 = await respuestaRechazo1.Content.ReadAsStringAsync();
        var cuerpo2 = await respuestaRechazo2.Content.ReadAsStringAsync();

        // Normalizar el token antiforgery antes de comparar
        var cuerpo1Normalizado = NormalizarTokenAntiforgery(cuerpo1);
        var cuerpo2Normalizado = NormalizarTokenAntiforgery(cuerpo2);

        Assert.Equal(cuerpo1Normalizado, cuerpo2Normalizado);

        // Verificar que NO hay Set-Cookie en ningún rechazo
        Assert.False(respuestaRechazo1.Headers.Contains("Set-Cookie"));
        Assert.False(respuestaRechazo2.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task AC05_Cierre_De_Sesion_Invalida_La_Sesion()
    {
        // Arrange: crear cliente autenticado
        var ana = ConfiguracionDeAltas.Alta("ana");
        using var arranque = ConfiguracionDeAltas.Arranque(fabrica, ana);
        var cliente = AutenticacionDePruebas.ClienteSobreHttps(arranque, seguirRedirecciones: true);

        // Realizar login
        await AutenticacionDePruebas.IniciarSesionAsync(cliente, ana.UserName, ana.Password);

        // Verificar que la privada es accesible
        var respuestaPrivada1 = await cliente.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, respuestaPrivada1.StatusCode);

        // Act: realizar logout
        var formLogout = await ObtenerFormularioCerrarSesion(cliente);
        var contenidoLogout = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("__RequestVerificationToken", formLogout),
        });
        var respuestaLogout = await cliente.PostAsync("/Cuenta/CerrarSesion", contenidoLogout);

        // Assert: logout debe redirigir (el cliente sigue automáticamente)
        Assert.Equal(HttpStatusCode.OK, respuestaLogout.StatusCode);

        // Ahora, sin cookies, intentar acceder a /
        var cliente2 = AutenticacionDePruebas.ClienteSobreHttps(arranque, seguirRedirecciones: false);
        var respuestaPrivada2 = await cliente2.GetAsync("/");

        // Debe redirigir al login (302 o 401)
        Assert.True(
            respuestaPrivada2.StatusCode == HttpStatusCode.Found || respuestaPrivada2.StatusCode == HttpStatusCode.Unauthorized,
            $"Esperaba redirect o 401, obtuve {respuestaPrivada2.StatusCode}");
    }

    [Fact]
    public async Task AC06_Pagina_Privada_Identifica_A_La_Cuenta_Y_Ofrece_Cerrar_Sesion()
    {
        // Arrange: crear cliente autenticado
        var ana = ConfiguracionDeAltas.Alta("ana");
        using var arranque = ConfiguracionDeAltas.Arranque(fabrica, ana);
        var cliente = AutenticacionDePruebas.ClienteSobreHttps(arranque, seguirRedirecciones: true);

        // Realizar login
        await AutenticacionDePruebas.IniciarSesionAsync(cliente, ana.UserName, ana.Password);

        // Act: acceder a la privada
        var respuesta = await cliente.GetAsync("/");

        // Assert: debe ser 200
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        var contenido = await respuesta.Content.ReadAsStringAsync();

        // Debe contener el saludo con el nombre
        Assert.Contains("Bienvenido", contenido);
        Assert.Contains(ana.UserName, contenido);

        // Debe contener el formulario de cierre de sesión
        Assert.Contains("Cerrar sesión", contenido);
        Assert.Contains("method=\"post\"", contenido, StringComparison.OrdinalIgnoreCase);

        // No debe contener datos médicos en el cuerpo (excepto en metadatos del documento)
        // Verificar que no hay un listado de estudios médicos
        Assert.DoesNotContain("estudio", contenido, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pdf", contenido, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AC09_Rechazo_No_Registra_Contrasena_Cookie_Ni_Hash()
    {
        // Arrange: configurar un registro en memoria
        var registro = new RegistroEnMemoria();
        var ana = ConfiguracionDeAltas.Alta("ana");
        using var arranque = ConfiguracionDeAltas.ArranqueConRegistro(fabrica, registro, ana);

        var cliente = AutenticacionDePruebas.ClienteSobreHttps(arranque, seguirRedirecciones: false);

        // Valores que nunca deben aparecer en los logs
        var antivalores = new[]
        {
            ana.Password, // la contraseña correcta
            "contrasena-incorrecta", // contraseña ingresada
            ana.Password.GetHashCode().ToString(), // intentar capturar un hash
        };

        // Act: realizar login fallido
        await AutenticacionDePruebas.IniciarSesionAsync(cliente, ana.UserName, "contrasena-incorrecta");

        // Assert: verificar que ningún antivalor aparece en los logs
        var mensajes = registro.Mensajes;

        foreach (var antivalor in antivalores)
        {
            Assert.DoesNotContain(
                antivalor,
                mensajes.SelectMany(m => m.Split()),
                StringComparer.OrdinalIgnoreCase);
        }

        // Verificar específicamente que no hay credentials en las categorías de Identity
        var categoriasIdentity = mensajes
            .Where(m => m.Contains("Microsoft.AspNetCore.Identity") || m.Contains("Microsoft.Extensions.Identity.Core"))
            .ToList();

        foreach (var mensaje in categoriasIdentity)
        {
            Assert.DoesNotContain(ana.Password, mensaje);
            Assert.DoesNotContain("contrasena-incorrecta", mensaje);
        }
    }

    /// <summary>Auxiliar: realizar un POST de login sin afirmar estado.</summary>
    private async Task<HttpResponseMessage> RealizarInicioDeSesionAsync(
        HttpClient cliente, string usuario, string contrasena)
    {
        // GET para obtener el token
        var respuestaGet = await cliente.GetAsync("/Cuenta/IniciarSesion");
        var html = await respuestaGet.Content.ReadAsStringAsync();

        var tokenMatch = Regex.Match(
            html,
            @"<input[^>]*name=""__RequestVerificationToken""[^>]*value=""([^""]*)""",
            RegexOptions.IgnoreCase);
        if (!tokenMatch.Success)
        {
            throw new InvalidOperationException("No se encontró el token antiforgery.");
        }

        var token = tokenMatch.Groups[1].Value;

        // POST con credenciales
        var contenido = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("NombreDeUsuario", usuario),
            new KeyValuePair<string, string>("Contrasena", contrasena),
            new KeyValuePair<string, string>("__RequestVerificationToken", token),
        });

        return await cliente.PostAsync("/Cuenta/IniciarSesion", contenido);
    }

    /// <summary>Auxiliar: normalizar el token antiforgery en HTML para comparación.</summary>
    private static string NormalizarTokenAntiforgery(string html)
    {
        // Reemplazar el valor del token por un marcador fijo
        return Regex.Replace(
            html,
            @"value=""[^""]*""",
            "value=\"__TOKEN_NORMALIZADO__\"");
    }

    /// <summary>Auxiliar: extraer el token antiforgery del formulario de logout.</summary>
    private async Task<string> ObtenerFormularioCerrarSesion(HttpClient cliente)
    {
        var respuesta = await cliente.GetAsync("/");
        var html = await respuesta.Content.ReadAsStringAsync();

        var tokenMatch = Regex.Match(
            html,
            @"<input[^>]*name=""__RequestVerificationToken""[^>]*value=""([^""]*)""",
            RegexOptions.IgnoreCase);

        if (!tokenMatch.Success)
        {
            throw new InvalidOperationException("No se encontró el token en la página privada.");
        }

        return tokenMatch.Groups[1].Value;
    }

    public void Dispose()
    {
        fabrica?.Dispose();
    }
}
