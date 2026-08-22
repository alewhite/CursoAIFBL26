using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace MiArchivoMedico.Tests;

/// <summary>
/// Tests de la superficie HTTP (Bloque 3 de FEAT-001a). Este sub-ticket no mapea ninguna ruta: el
/// alta es un procedimiento administrativo del arranque y no existe camino en la interfaz para
/// crear, cambiar ni recuperar una contraseña (FR-01, AC-02, RNF-54 del maestro).
/// </summary>
public class SuperficieHttpTests
{
    /// <summary>
    /// Rutas que publicaría `Microsoft.AspNetCore.Identity.UI` con solo referenciarlo, y que el
    /// `.csproj` tiene prohibido traer.
    /// </summary>
    private static readonly string[] RutasProhibidas =
    [
        "/Identity/Account/Register",
        "/Identity/Account/ForgotPassword",
        "/Identity/Account/ResetPassword",
        "/Identity/Account/Manage",
    ];

    [Fact(Skip = "Bloque 2: requiere CuentaController para resolver el patrón de routing")]
    public async Task Rutas_De_Registro_Y_De_Contrasena_Responden_404()
    {
        var ana = ConfiguracionDeAltas.Alta("ana");

        using var fabrica = new AppFactory();
        using var arranque = ConfiguracionDeAltas.Arranque(fabrica, ana);

        // Con una cuenta ya sembrada hay algo que la solicitud podría modificar: sobre una base
        // vacía, "no se creó ni modificó ninguna cuenta" se cumpliría por construcción.
        var antes = Assert.Single(ConfiguracionDeAltas.Cuentas(arranque.Services));

        using var cliente = arranque.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        foreach (var ruta in RutasProhibidas)
        {
            var consulta = await cliente.GetAsync(ruta);
            Assert.Equal(HttpStatusCode.NotFound, consulta.StatusCode);

            // También por POST: un formulario de registro o de restablecimiento llegaría así, y un
            // 404 solo en GET dejaría el verbo que de verdad escribe sin comprobar.
            using var formulario = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["UserName"] = "intrusa",
                ["Password"] = "contrasena-declarada-de-intrusa-2026",
                ["Email"] = "intrusa@ejemplo.invalid",
            });

            var envio = await cliente.PostAsync(ruta, formulario);
            Assert.Equal(HttpStatusCode.NotFound, envio.StatusCode);
        }

        var despues = Assert.Single(ConfiguracionDeAltas.Cuentas(arranque.Services));

        Assert.Equal(antes.Id, despues.Id);
        Assert.Equal(antes.UserName, despues.UserName);
        Assert.Equal(antes.PasswordHash, despues.PasswordHash);
    }
}
