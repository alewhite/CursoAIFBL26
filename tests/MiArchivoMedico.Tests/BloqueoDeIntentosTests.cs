using System.Diagnostics;
using System.Net;
using MiArchivoMedico.Tests.Apoyo;

namespace MiArchivoMedico.Tests;

public class BloqueoDeIntentosTests
{
    private const string ContrasenaEquivocada = "esta-no-es-la-contrasena";

    [Fact(DisplayName = "AC-69: tras 5 fallos en 15 minutos, la contraseña correcta también se rechaza")]
    public async Task CincoFallos_RechazaLaContrasenaCorrecta()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = aplicacion.CrearClienteSinRedirecciones();

        for (var i = 0; i < 5; i++)
            await cliente.IniciarSesionAsync(AplicacionDePrueba.UsuarioUno, ContrasenaEquivocada);

        aplicacion.Reloj.Advance(TimeSpan.FromMinutes(5));
        var sexto = await cliente.IniciarSesionAsync(AplicacionDePrueba.UsuarioUno, AplicacionDePrueba.Contrasena);

        Assert.Equal(HttpStatusCode.OK, sexto.StatusCode);
        Assert.True((await cliente.GetAsync("/Estudios")).RedirigeAlIngreso(),
            "La cuenta bloqueada no debía quedar autenticada.");
    }

    [Fact(DisplayName = "AC-86: pasados 15 minutos desde el quinto fallo, la contraseña correcta funciona")]
    public async Task PasadaLaVentana_ConcedeElAcceso()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = aplicacion.CrearClienteSinRedirecciones();

        for (var i = 0; i < 5; i++)
            await cliente.IniciarSesionAsync(AplicacionDePrueba.UsuarioUno, ContrasenaEquivocada);

        aplicacion.Reloj.Advance(TimeSpan.FromMinutes(16));
        await cliente.IniciarSesionAsync(AplicacionDePrueba.UsuarioUno, AplicacionDePrueba.Contrasena);

        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/Estudios")).StatusCode);
    }

    [Fact(DisplayName = "AC-87: un ingreso exitoso reinicia el contador de intentos fallidos")]
    public async Task IngresoExitoso_ReiniciaElContador()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = aplicacion.CrearClienteSinRedirecciones();

        for (var i = 0; i < 4; i++)
            await cliente.IniciarSesionAsync(AplicacionDePrueba.UsuarioUno, ContrasenaEquivocada);

        await cliente.IniciarSesionAsync(AplicacionDePrueba.UsuarioUno, AplicacionDePrueba.Contrasena);
        await cliente.CerrarSesionAsync();

        for (var i = 0; i < 4; i++)
            await cliente.IniciarSesionAsync(AplicacionDePrueba.UsuarioUno, ContrasenaEquivocada);

        await cliente.IniciarSesionAsync(AplicacionDePrueba.UsuarioUno, AplicacionDePrueba.Contrasena);

        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/Estudios")).StatusCode);
    }

    [Fact(DisplayName = "AC-98: un nombre de usuario inexistente se bloquea igual que una cuenta real")]
    public async Task NombreInexistente_SeBloqueaIgual()
    {
        using var aplicacion = new AplicacionDePrueba();
        var clienteInexistente = aplicacion.CrearClienteSinRedirecciones();
        var clienteReal = aplicacion.CrearClienteSinRedirecciones();

        for (var i = 0; i < 5; i++)
        {
            await clienteInexistente.IniciarSesionAsync("nadie.existe", ContrasenaEquivocada);
            await clienteReal.IniciarSesionAsync(AplicacionDePrueba.UsuarioDos, ContrasenaEquivocada);
        }

        var sextoInexistente = await clienteInexistente.IniciarSesionAsync("nadie.existe", ContrasenaEquivocada);
        var sextoReal = await clienteReal.IniciarSesionAsync(AplicacionDePrueba.UsuarioDos, AplicacionDePrueba.Contrasena);

        var mensajeInexistente = AutenticacionTests.ExtraerMensajeDeError(await sextoInexistente.Content.ReadAsStringAsync());
        var mensajeReal = AutenticacionTests.ExtraerMensajeDeError(await sextoReal.Content.ReadAsStringAsync());

        Assert.Equal(mensajeInexistente, mensajeReal);
        Assert.True((await clienteReal.GetAsync("/Estudios")).RedirigeAlIngreso());
    }

    [Fact(DisplayName = "RNF-65: el rechazo tarda lo mismo con una cuenta real que con un nombre inexistente")]
    public async Task DemoraDelRechazo_EsComparable()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = aplicacion.CrearClienteSinRedirecciones();

        // Una primera pasada descarta el costo de arranque de la aplicación.
        await cliente.IniciarSesionAsync(AplicacionDePrueba.UsuarioUno, ContrasenaEquivocada);
        await cliente.IniciarSesionAsync("nadie.existe", ContrasenaEquivocada);

        var conCuentaReal = await Medir(() => cliente.IniciarSesionAsync(AplicacionDePrueba.UsuarioUno, ContrasenaEquivocada));
        var sinCuenta = await Medir(() => cliente.IniciarSesionAsync("tampoco.existe", ContrasenaEquivocada));

        var mayor = Math.Max(conCuentaReal, sinCuenta);
        var menor = Math.Min(conCuentaReal, sinCuenta);
        Assert.True(mayor <= menor * 4 + 50,
            $"La demora debe ser comparable: {conCuentaReal} ms con cuenta real y {sinCuenta} ms sin cuenta.");
    }

    private static async Task<double> Medir(Func<Task<HttpResponseMessage>> operacion)
    {
        var cronometro = Stopwatch.StartNew();
        await operacion();
        return cronometro.Elapsed.TotalMilliseconds;
    }
}
