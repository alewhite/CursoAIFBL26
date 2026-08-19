using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using MiArchivoMedico.Web.Dominio;

namespace MiArchivoMedico.Tests;

public class CuentasInicialesTests
{
    [Fact(DisplayName = "AC-62: con 5 cuentas activas, el alta de una sexta se rechaza informando el límite")]
    public async Task SextaCuenta_SeRechaza()
    {
        using var aplicacion = new AplicacionDePruebaConCuentas(
            [("uno", "contrasena-larga-01"), ("dos", "contrasena-larga-02"), ("tres", "contrasena-larga-03"),
             ("cuatro", "contrasena-larga-04"), ("cinco", "contrasena-larga-05"), ("seis", "contrasena-larga-06")]);

        var error = await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            using var alcance = aplicacion.Services.CreateScope();
            await Task.CompletedTask;
        });

        Assert.Contains("5 cuentas", DesenrollarMensaje(error), StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "AC-105: el alta con una contraseña de 11 caracteres se rechaza al arrancar")]
    public async Task ContrasenaCorta_SeRechaza()
    {
        using var aplicacion = new AplicacionDePruebaConCuentas([("corta", "12345678901")]);

        var error = await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            using var alcance = aplicacion.Services.CreateScope();
            await Task.CompletedTask;
        });

        Assert.Contains("12", DesenrollarMensaje(error));
    }

    [Fact(DisplayName = "RNF-56: la instalación no supera las 5 cuentas activas")]
    public async Task InstalacionDePrueba_NoSuperaElMaximo()
    {
        using var aplicacion = new AplicacionDePrueba();
        using var alcance = aplicacion.Services.CreateScope();
        var administrador = alcance.ServiceProvider.GetRequiredService<UserManager<Usuario>>();

        Assert.True(administrador.Users.Count() <= 5);
        await Task.CompletedTask;
    }

    private static string DesenrollarMensaje(Exception error)
    {
        var mensajes = new List<string>();
        for (Exception? actual = error; actual is not null; actual = actual.InnerException)
            mensajes.Add(actual.Message);
        return string.Join(" | ", mensajes);
    }
}

/// <summary>Fábrica con un juego de cuentas a medida, para ejercitar el alta administrativa.</summary>
public sealed class AplicacionDePruebaConCuentas((string Nombre, string Contrasena)[] cuentas) : AplicacionDePrueba
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder constructor)
    {
        base.ConfigureWebHost(constructor);

        // Reemplaza por completo las cuentas que siembra la fábrica base.
        constructor.UseSetting("CuentasIniciales:0:NombreDeUsuario", string.Empty);
        constructor.UseSetting("CuentasIniciales:1:NombreDeUsuario", string.Empty);

        for (var i = 0; i < cuentas.Length; i++)
        {
            constructor.UseSetting($"CuentasIniciales:{i}:NombreDeUsuario", cuentas[i].Nombre);
            constructor.UseSetting($"CuentasIniciales:{i}:Email", $"{cuentas[i].Nombre}@ejemplo.invalido");
            constructor.UseSetting($"CuentasIniciales:{i}:Contrasena", cuentas[i].Contrasena);
        }
    }
}
