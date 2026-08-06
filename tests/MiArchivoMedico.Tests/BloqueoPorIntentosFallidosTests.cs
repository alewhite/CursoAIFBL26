using System.Net;
using MiArchivoMedico.Tests.Infraestructura;
using MiArchivoMedico.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace MiArchivoMedico.Tests;

/// <summary>Bloqueo tras intentos fallidos repetidos (RNF-60).</summary>
public class BloqueoPorIntentosFallidosTests : IAsyncLifetime
{
    private readonly AplicacionDePrueba _app = new();

    public Task InitializeAsync() => _app.InitializeAsync();

    public Task DisposeAsync() => _app.DisposeAsync();

    [Fact(DisplayName = "AC-69: tras 5 fallos, la contraseña correcta se rechaza con el mismo mensaje")]
    public async Task TrasCincoFallos_LaContrasenaCorrectaSeRechazaConElMismoMensaje()
    {
        await FallarAsync(5);

        var respuesta = await _app.CrearCliente()
            .IniciarSesionAsync(AplicacionDePrueba.Usuario, AplicacionDePrueba.Contrasena);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);   // no redirige: no inició sesión
        var html = WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync());
        Assert.Contains("Usuario o contraseña incorrectos.", html);

        // El bloqueo no se nombra en ninguna forma: es indistinguible de una credencial inválida (RNF-13).
        Assert.DoesNotContain("bloque", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "AC-86: pasados 15 minutos desde el quinto fallo, se concede el acceso")]
    public async Task PasadosQuinceMinutos_SeConcedeElAcceso()
    {
        await FallarAsync(5);

        // UserManager compara el fin del bloqueo contra el reloj real del sistema, que la prueba no puede
        // adelantar: se retrocede el vencimiento almacenado, que es el mismo estado que dejarían pasar
        // 15 minutos.
        await _app.EnAlcanceAsync(async servicios =>
        {
            var usuarios = servicios.GetRequiredService<UserManager<UsuarioApp>>();
            var usuario = await usuarios.FindByNameAsync(AplicacionDePrueba.Usuario);
            Assert.True(await usuarios.IsLockedOutAsync(usuario!), "La cuenta debía quedar bloqueada.");
            await usuarios.SetLockoutEndDateAsync(usuario!, DateTimeOffset.UtcNow.AddSeconds(-1));
        });

        var cliente = _app.CrearCliente();
        var respuesta = await cliente.IniciarSesionAsync(
            AplicacionDePrueba.Usuario, AplicacionDePrueba.Contrasena);

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.True(await cliente.TieneSesionValidaAsync());
    }

    [Fact(DisplayName = "AC-87: un inicio de sesión exitoso reinicia el contador de fallos")]
    public async Task UnInicioDeSesionExitoso_ReiniciaElContadorDeFallos()
    {
        await FallarAsync(4);

        var exitoso = await _app.CrearCliente()
            .IniciarSesionAsync(AplicacionDePrueba.Usuario, AplicacionDePrueba.Contrasena);
        Assert.Equal(HttpStatusCode.Redirect, exitoso.StatusCode);

        await FallarAsync(4);

        var cliente = _app.CrearCliente();
        var respuesta = await cliente.IniciarSesionAsync(
            AplicacionDePrueba.Usuario, AplicacionDePrueba.Contrasena);

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.True(await cliente.TieneSesionValidaAsync());
    }

    [Fact(DisplayName = "RNF-60: los fallos anteriores a la ventana de 15 minutos no cuentan")]
    public async Task LosFallosFueraDeLaVentana_NoCuentan()
    {
        await FallarAsync(4);

        _app.Reloj.Advance(TimeSpan.FromMinutes(16));

        // Con el contador reiniciado, un quinto fallo aislado no alcanza el umbral de bloqueo.
        await FallarAsync(1);

        var cliente = _app.CrearCliente();
        var respuesta = await cliente.IniciarSesionAsync(
            AplicacionDePrueba.Usuario, AplicacionDePrueba.Contrasena);

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.True(await cliente.TieneSesionValidaAsync());
    }

    private async Task FallarAsync(int intentos)
    {
        for (var i = 0; i < intentos; i++)
        {
            await _app.CrearCliente()
                .IniciarSesionAsync(AplicacionDePrueba.Usuario, $"Ficticia-2026-mala-{i}");
        }
    }
}
