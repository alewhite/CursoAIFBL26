using System.Net;
using MiArchivoMedico.Tests.Apoyo;

namespace MiArchivoMedico.Tests;

/// <summary>
/// Las ventanas temporales se ejercitan adelantando el tiempo simulado, que también se inyecta en el
/// manejador de la cookie. Cada prueba usa su propia aplicación porque mueve el reloj.
/// </summary>
public class SesionTests
{
    [Fact(DisplayName = "AC-06: tras 30 minutos de inactividad, el usuario debe autenticarse de nuevo")]
    public async Task TreintaMinutosSinActividad_ExigeAutenticarse()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = aplicacion.CrearClienteSinRedirecciones();
        await cliente.IniciarSesionAsync(AplicacionDePrueba.UsuarioUno, AplicacionDePrueba.Contrasena);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/Estudios")).StatusCode);

        aplicacion.Reloj.Advance(TimeSpan.FromMinutes(31));

        Assert.True((await cliente.GetAsync("/Estudios")).RedirigeAlIngreso());
    }

    [Fact(DisplayName = "AC-06: la ventana de inactividad es deslizante y se reinicia con cada solicitud")]
    public async Task ActividadSostenida_MantieneLaSesion()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = aplicacion.CrearClienteSinRedirecciones();
        await cliente.IniciarSesionAsync(AplicacionDePrueba.UsuarioUno, AplicacionDePrueba.Contrasena);

        for (var i = 0; i < 4; i++)
        {
            aplicacion.Reloj.Advance(TimeSpan.FromMinutes(20));
            Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/Estudios")).StatusCode);
        }
    }

    [Fact(DisplayName = "AC-07: superadas las 24 horas, el sistema exige una nueva autenticación")]
    public async Task VeinticuatroHoras_ExigeNuevaAutenticacion()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = aplicacion.CrearClienteSinRedirecciones();
        await cliente.IniciarSesionAsync(AplicacionDePrueba.UsuarioUno, AplicacionDePrueba.Contrasena);

        // Actividad sostenida durante 25 horas: la ventana deslizante sola no alcanzaría para expirar.
        for (var i = 0; i < 100; i++)
        {
            aplicacion.Reloj.Advance(TimeSpan.FromMinutes(15));
            await cliente.GetAsync("/Estudios");
        }

        Assert.True((await cliente.GetAsync("/Estudios")).RedirigeAlIngreso(),
            "El tope absoluto de 24 horas debe prevalecer sobre la ventana deslizante.");
    }

    [Fact(DisplayName = "AC-103: iniciar sesión en otro dispositivo invalida la sesión anterior de la cuenta")]
    public async Task SegundoIngreso_InvalidaLaSesionAnterior()
    {
        using var aplicacion = new AplicacionDePrueba();
        var primerDispositivo = aplicacion.CrearClienteSinRedirecciones();
        var segundoDispositivo = aplicacion.CrearClienteSinRedirecciones();

        await primerDispositivo.IniciarSesionAsync(AplicacionDePrueba.UsuarioUno, AplicacionDePrueba.Contrasena);
        Assert.Equal(HttpStatusCode.OK, (await primerDispositivo.GetAsync("/Estudios")).StatusCode);

        await segundoDispositivo.IniciarSesionAsync(AplicacionDePrueba.UsuarioUno, AplicacionDePrueba.Contrasena);

        Assert.True((await primerDispositivo.GetAsync("/Estudios")).RedirigeAlIngreso(),
            "La sesión del primer dispositivo debía dejar de ser válida.");
        Assert.Equal(HttpStatusCode.OK, (await segundoDispositivo.GetAsync("/Estudios")).StatusCode);
    }

    [Fact(DisplayName = "AC-104: la cookie de autenticación no declara vencimiento propio")]
    public async Task CookieDeAutenticacion_EsDeSesionDelNavegador()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = aplicacion.CrearClienteSinRedirecciones();

        var ingreso = await cliente.IniciarSesionAsync(AplicacionDePrueba.UsuarioUno, AplicacionDePrueba.Contrasena);

        var cookie = ingreso.Headers.TryGetValues("Set-Cookie", out var valores)
            ? valores.FirstOrDefault(v => v.StartsWith("archivo-medico=", StringComparison.Ordinal))
            : null;

        Assert.NotNull(cookie);
        Assert.DoesNotContain("expires=", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("max-age=", cookie, StringComparison.OrdinalIgnoreCase);
    }
}
