using System.Net;
using MiArchivoMedico.Tests.Infraestructura;

namespace MiArchivoMedico.Tests;

public class AutenticacionTests : IAsyncLifetime
{
    private readonly AplicacionDePrueba _app = new();

    public Task InitializeAsync() => _app.InitializeAsync();

    public Task DisposeAsync() => _app.DisposeAsync();

    [Fact(DisplayName = "AC-01: sin sesión, la aplicación redirige al inicio de sesión")]
    public async Task SinSesion_RedirigeAlInicioDeSesion()
    {
        var cliente = _app.CrearCliente();

        var respuesta = await cliente.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Contains("/Cuenta/Login", respuesta.Headers.Location!.OriginalString);
    }

    [Fact(DisplayName = "AC-03: con credenciales válidas se accede a la aplicación")]
    public async Task CredencialesValidas_InicianSesion()
    {
        var cliente = _app.CrearCliente();

        var respuesta = await cliente.IniciarSesionAsync(
            AplicacionDePrueba.Usuario, AplicacionDePrueba.Contrasena);

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.True(await cliente.TieneSesionValidaAsync());
    }

    [Fact(DisplayName = "AC-04: usuario inexistente y contraseña incorrecta dan el mismo mensaje")]
    public async Task CredencialesInvalidas_DevuelvenElMismoMensaje()
    {
        var porUsuarioInexistente = await _app.CrearCliente()
            .IniciarSesionAsync("no.existe", "Ficticia-2026-nula");
        var porContrasenaIncorrecta = await _app.CrearCliente()
            .IniciarSesionAsync(AplicacionDePrueba.Usuario, "Ficticia-2026-mala");

        Assert.Equal(HttpStatusCode.OK, porUsuarioInexistente.StatusCode);
        Assert.Equal(HttpStatusCode.OK, porContrasenaIncorrecta.StatusCode);

        var mensajeUsuario = ExtraerResumenDeValidacion(
            await porUsuarioInexistente.Content.ReadAsStringAsync());
        var mensajeContrasena = ExtraerResumenDeValidacion(
            await porContrasenaIncorrecta.Content.ReadAsStringAsync());

        Assert.Equal(mensajeUsuario, mensajeContrasena);
        Assert.Contains("Usuario o contraseña incorrectos.", mensajeUsuario);
    }

    [Fact(DisplayName = "AC-05: cerrar sesión invalida la sesión")]
    public async Task CerrarSesion_InvalidaLaSesion()
    {
        var cliente = _app.CrearCliente();
        await cliente.IniciarSesionAsync(AplicacionDePrueba.Usuario, AplicacionDePrueba.Contrasena);
        Assert.True(await cliente.TieneSesionValidaAsync());

        await cliente.CerrarSesionAsync();

        Assert.False(await cliente.TieneSesionValidaAsync());
    }

    [Fact(DisplayName = "AC-06: tras 30 minutos de inactividad hay que autenticarse de nuevo")]
    public async Task TrasTreintaMinutosDeInactividad_ExigeAutenticarseDeNuevo()
    {
        var cliente = _app.CrearCliente();
        await cliente.IniciarSesionAsync(AplicacionDePrueba.Usuario, AplicacionDePrueba.Contrasena);

        _app.Reloj.Advance(TimeSpan.FromMinutes(31));

        Assert.False(await cliente.TieneSesionValidaAsync());
    }

    [Fact(DisplayName = "AC-07: la sesión expira a las 24 horas aunque haya actividad continua")]
    public async Task ConActividadContinua_LaSesionExpiraALasVeinticuatroHoras()
    {
        var cliente = _app.CrearCliente();
        await cliente.IniciarSesionAsync(AplicacionDePrueba.Usuario, AplicacionDePrueba.Contrasena);

        // Actividad cada 20 minutos: nunca se alcanza la expiración por inactividad, de modo que lo
        // único que puede cortar la sesión es el tope absoluto.
        var transcurrido = TimeSpan.Zero;
        while (transcurrido < TimeSpan.FromHours(23))
        {
            _app.Reloj.Advance(TimeSpan.FromMinutes(20));
            transcurrido += TimeSpan.FromMinutes(20);
            Assert.True(
                await cliente.TieneSesionValidaAsync(),
                $"La sesión se cortó a las {transcurrido}, antes del tope de 24 horas.");
        }

        _app.Reloj.Advance(TimeSpan.FromHours(1) + TimeSpan.FromMinutes(1));

        Assert.False(await cliente.TieneSesionValidaAsync());
    }

    [Fact(DisplayName = "AC-58: la cookie de autenticación es Secure, HttpOnly y SameSite=Strict")]
    public async Task CookieDeAutenticacion_TieneLosAtributosDeSeguridad()
    {
        var cliente = _app.CrearCliente();

        var respuesta = await cliente.IniciarSesionAsync(
            AplicacionDePrueba.Usuario, AplicacionDePrueba.Contrasena);

        var cookie = Assert.Single(
            respuesta.Headers.GetValues("Set-Cookie"),
            valor => valor.Contains("__Host-archivo-medico"));

        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
    }

    private static string ExtraerResumenDeValidacion(string html)
    {
        const string marca = "validation-summary-errors";
        var inicio = html.IndexOf(marca, StringComparison.Ordinal);
        Assert.True(inicio >= 0, "La página no mostró un resumen de errores de validación.");

        var fin = html.IndexOf("</div>", inicio, StringComparison.Ordinal);

        // Razor escapa los acentos: se decodifica para comparar contra el mensaje tal como se redactó.
        return WebUtility.HtmlDecode(html[inicio..fin]);
    }
}
