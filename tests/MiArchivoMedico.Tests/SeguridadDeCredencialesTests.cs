using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using MiArchivoMedico.Tests.Apoyo;
using MiArchivoMedico.Web.Dominio;
using MiArchivoMedico.Web.Servicios;

namespace MiArchivoMedico.Tests;

public class SeguridadDeCredencialesTests(AplicacionDePrueba aplicacion) : IClassFixture<AplicacionDePrueba>
{
    [Fact(DisplayName = "AC-58: la cookie de autenticación presenta Secure, HttpOnly y SameSite=Strict")]
    public async Task CookieDeAutenticacion_TieneLosAtributosExigidos()
    {
        var cliente = aplicacion.CrearClienteSinRedirecciones();

        var ingreso = await cliente.IniciarSesionAsync(AplicacionDePrueba.UsuarioUno, AplicacionDePrueba.Contrasena);

        var cookie = ingreso.Headers.TryGetValues("Set-Cookie", out var valores)
            ? valores.FirstOrDefault(v => v.StartsWith("archivo-medico=", StringComparison.Ordinal))
            : null;

        Assert.NotNull(cookie);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "AC-76: la contraseña almacenada no está en claro y usa PBKDF2-HMAC-SHA256")]
    public async Task ContrasenaAlmacenada_UsaElAlgoritmoExigido()
    {
        using var alcance = aplicacion.Services.CreateScope();
        var administrador = alcance.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        var usuario = await administrador.FindByNameAsync(AplicacionDePrueba.UsuarioUno);

        Assert.NotNull(usuario?.PasswordHash);
        Assert.DoesNotContain(AplicacionDePrueba.Contrasena, usuario.PasswordHash, StringComparison.Ordinal);

        var bytes = Convert.FromBase64String(usuario.PasswordHash);
        Assert.Equal(0x01, bytes[0]);                                   // formato V3 de Identity
        Assert.Equal(1u, LeerBigEndian(bytes.AsSpan(1)));               // PRF = HMAC-SHA256
        Assert.True(LeerBigEndian(bytes.AsSpan(5)) >= HasherPbkdf2Sha256.IteracionesMinimas,
            "RNF-03 exige al menos 100.000 iteraciones.");
    }

    [Fact(DisplayName = "RNF-03: el hasher se niega a construirse por debajo de las 100.000 iteraciones")]
    public void Hasher_ConPocasIteraciones_FallaAlConstruirse()
    {
        var opciones = Microsoft.Extensions.Options.Options.Create(
            new PasswordHasherOptions { IterationCount = 1_000 });

        Assert.Throws<InvalidOperationException>(() => new HasherPbkdf2Sha256(opciones));
    }

    private static uint LeerBigEndian(ReadOnlySpan<byte> origen) =>
        ((uint)origen[0] << 24) | ((uint)origen[1] << 16) | ((uint)origen[2] << 8) | origen[3];
}
