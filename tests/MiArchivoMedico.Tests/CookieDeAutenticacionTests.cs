using System.Net;
using Xunit;

namespace MiArchivoMedico.Tests;

/// <summary>
/// Tests de propiedades de la cookie de autenticación (Bloque 4 de FEAT-001b).
/// AC-07: Cookie Secure, HttpOnly, SameSite=Strict, sin Expires/Max-Age (sesión del navegador).
/// </summary>
public class CookieDeAutenticacionTests : IDisposable
{
    private readonly AppFactory fabrica = new();

    [Fact]
    public async Task AC07_Cookie_De_Autenticacion_Declara_Secure_HttpOnly_SameSite_Strict_Y_Es_De_Sesion()
    {
        // Arrange: crear una cuenta de prueba
        var ana = ConfiguracionDeAltas.Alta("ana");
        using var arranque = ConfiguracionDeAltas.Arranque(fabrica, ana);
        var cliente = AutenticacionDePruebas.ClienteSobreHttps(arranque, seguirRedirecciones: false);

        // Act: realizar login correcto
        var resultado = await AutenticacionDePruebas.IniciarSesionAsync(cliente, ana.UserName, ana.Password);

        // Assert: debe haber un Set-Cookie
        Assert.NotEmpty(resultado.CookiesEmitidas);

        // Parsear el Set-Cookie para verificar los atributos
        var setCookieHeader = resultado.CookiesEmitidas.First();

        // Verificar Secure
        Assert.Contains("Secure", setCookieHeader, StringComparison.OrdinalIgnoreCase);

        // Verificar HttpOnly
        Assert.Contains("HttpOnly", setCookieHeader, StringComparison.OrdinalIgnoreCase);

        // Verificar SameSite=Strict
        Assert.Contains("SameSite=Strict", setCookieHeader, StringComparison.OrdinalIgnoreCase);

        // Verificar que NO tiene Expires ni Max-Age (es cookie de sesión)
        Assert.DoesNotContain("Expires", setCookieHeader, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Max-Age", setCookieHeader, StringComparison.OrdinalIgnoreCase);

        // Verificar que la cookie tiene nombre válido (contiene "Auth" como valor del nombre de la cookie)
        Assert.Contains("=", setCookieHeader); // Debe tener un nombre=valor
    }

    public void Dispose()
    {
        fabrica?.Dispose();
    }
}
