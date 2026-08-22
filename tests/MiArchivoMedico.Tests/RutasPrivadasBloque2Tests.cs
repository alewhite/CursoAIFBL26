using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MiArchivoMedico.Web.Controllers;
using MiArchivoMedico.Web.ViewModels;
using Xunit;

namespace MiArchivoMedico.Tests;

/// <summary>
/// Tests de Bloque 2: CuentaController (login, logout, página privada). Ejercitan la lógica de
/// autenticación del controlador sin depender de vistas HTML (que son Bloque 3).
/// AC-03, AC-04, AC-05, AC-06, NFR-01, NFR-07, FR-02, FR-03, FR-04.
/// </summary>
public class RutasPrivadasBloque2Tests : IDisposable
{
    private readonly AppFactory fabrica = new();

    [Fact]
    public void GET_IniciarSesion_Devuelve_ViewResult_Con_ViewModel_Vacio()
    {
        using var arranque = ConfiguracionDeAltas.Arranque(fabrica);
        var (controller, alcance) = CrearControlador(arranque);
        using (alcance)
        {
            var resultado = controller.IniciarSesion();

            Assert.IsType<ViewResult>(resultado);
            var viewResult = (ViewResult)resultado;
            Assert.NotNull(viewResult.Model);
            Assert.IsType<IniciarSesionViewModel>(viewResult.Model);

            var modelo = (IniciarSesionViewModel)viewResult.Model;
            Assert.Null(modelo.NombreDeUsuario);
            Assert.Null(modelo.Contrasena);
        }
    }

    // POST IniciarSesion exitoso se prueba en Bloque 4 (tests de integración HTTP con vistas HTML)
    // Requiere SignInManager.SignInWithClaimsAsync que inyecta HttpContext de sesión

    [Fact]
    public async Task POST_IniciarSesion_Fallido_Usuario_Inexistente_Devuelve_ViewResult_Con_Error()
    {
        var ana = ConfiguracionDeAltas.Alta("ana");

        using var arranque = ConfiguracionDeAltas.Arranque(fabrica, ana);
        var (controller, alcance) = CrearControlador(arranque);
        using (alcance)
        {
            var modelo = new IniciarSesionViewModel
            {
                NombreDeUsuario = "usuario-que-no-existe",
                Contrasena = "cual-sea",
            };

            var resultado = await controller.IniciarSesion(modelo);

            Assert.IsType<ViewResult>(resultado);
            Assert.False(controller.ModelState.IsValid);
            Assert.Single(controller.ModelState.Values.Where(v => v.Errors.Any()));
            Assert.Equal("Nombre de usuario o contraseña no válidos",
                controller.ModelState.Values.First(v => v.Errors.Any()).Errors.First().ErrorMessage);
        }
    }

    [Fact]
    public async Task POST_IniciarSesion_Fallido_Contrasena_Incorrecta_Devuelve_ViewResult_Con_Mismo_Error()
    {
        var ana = ConfiguracionDeAltas.Alta("ana");

        using var arranque = ConfiguracionDeAltas.Arranque(fabrica, ana);
        var (controller, alcance) = CrearControlador(arranque);
        using (alcance)
        {
            var modelo = new IniciarSesionViewModel
            {
                NombreDeUsuario = ana.UserName,
                Contrasena = "contrasena-incorrecta",
            };

            var resultado = await controller.IniciarSesion(modelo);

            Assert.IsType<ViewResult>(resultado);
            Assert.False(controller.ModelState.IsValid);
            Assert.Equal("Nombre de usuario o contraseña no válidos",
                controller.ModelState.Values.First(v => v.Errors.Any()).Errors.First().ErrorMessage);
        }
    }

    [Fact]
    public async Task POST_IniciarSesion_ModelState_Invalido_Devuelve_ViewResult()
    {
        using var arranque = ConfiguracionDeAltas.Arranque(fabrica);
        var (controller, alcance) = CrearControlador(arranque);
        using (alcance)
        {
            var modelo = new IniciarSesionViewModel
            {
                NombreDeUsuario = null,
                Contrasena = null,
            };

            controller.ModelState.AddModelError("NombreDeUsuario", "Required");

            var resultado = await controller.IniciarSesion(modelo);

            Assert.IsType<ViewResult>(resultado);
        }
    }

    // POST CerrarSesion se prueba en Bloque 4 (tests de integración HTTP con vistas HTML)
    // Requiere RedirectToAction que inyecta UrlHelper desde ActionContext

    [Fact]
    public void GET_Privada_Con_Usuario_Autenticado_Devuelve_ViewResult()
    {
        var ana = ConfiguracionDeAltas.Alta("ana");

        using var arranque = ConfiguracionDeAltas.Arranque(fabrica, ana);
        var (controller, alcance) = CrearControladorAutenticado(arranque, ana.UserName);
        using (alcance)
        {
            var resultado = controller.Privada();

            Assert.IsType<ViewResult>(resultado);
            var viewResult = (ViewResult)resultado;
            Assert.NotNull(viewResult.Model);
            Assert.IsType<IniciarSesionViewModel>(viewResult.Model);

            var modelo = (IniciarSesionViewModel)viewResult.Model;
            Assert.Equal(ana.UserName, modelo.NombreDeUsuario);
        }
    }

    private static (CuentaController controller, IServiceScope alcance) CrearControlador(WebApplicationFactory<Program> arranque)
    {
        var alcance = arranque.Services.CreateScope();

        var signInManager = alcance.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.SignInManager<MiArchivoMedico.Web.Accounts.AppUser>>();
        var userManager = alcance.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<MiArchivoMedico.Web.Accounts.AppUser>>();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = alcance.ServiceProvider,
        };

        var controllerContext = new ControllerContext
        {
            HttpContext = httpContext,
        };

        var controller = new CuentaController(signInManager, userManager)
        {
            ControllerContext = controllerContext,
        };

        return (controller, alcance);
    }

    private static (CuentaController controller, IServiceScope alcance) CrearControladorAutenticado(WebApplicationFactory<Program> arranque, string nombreDeUsuario)
    {
        var alcance = arranque.Services.CreateScope();

        var signInManager = alcance.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.SignInManager<MiArchivoMedico.Web.Accounts.AppUser>>();
        var userManager = alcance.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<MiArchivoMedico.Web.Accounts.AppUser>>();

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name, nombreDeUsuario),
        };

        var identity = new ClaimsIdentity(claims, "TestScheme");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = principal,
            RequestServices = alcance.ServiceProvider,
        };

        var controllerContext = new ControllerContext
        {
            HttpContext = httpContext,
        };

        var controller = new CuentaController(signInManager, userManager)
        {
            ControllerContext = controllerContext,
        };

        return (controller, alcance);
    }

    public void Dispose()
    {
        fabrica?.Dispose();
    }
}
