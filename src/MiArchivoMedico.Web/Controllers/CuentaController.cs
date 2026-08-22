using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MiArchivoMedico.Web.Accounts;
using MiArchivoMedico.Web.ViewModels;

namespace MiArchivoMedico.Web.Controllers;

/// <summary>
/// Controlador de autenticación: inicio de sesión, cierre de sesión y página privada (Bloque 2
/// de FEAT-001b). La FallbackPolicy requiere autenticación en todas las acciones excepto las
/// marcadas con `[AllowAnonymous]` (AC-03, NFR-07, FR-02, FR-03, FR-04).
/// </summary>
public class CuentaController(SignInManager<AppUser> signInManager, UserManager<AppUser> _)
    : Controller
{
    /// <summary>
    /// GET /Cuenta/IniciarSesion — Formulario de login. Es la única acción `[AllowAnonymous]` del
    /// sistema (NFR-07). Devuelve una vista con un ViewModel vacío.
    /// </summary>
    [AllowAnonymous]
    public IActionResult IniciarSesion()
    {
        var modelo = new IniciarSesionViewModel();
        return View(modelo);
    }

    /// <summary>
    /// POST /Cuenta/IniciarSesion — Procesa el intento de login. Modelo válido → prueba credenciales.
    /// Exitoso → redirige a la página privada (302 a `/`).
    /// Fallido (usuario inexistente, contraseña incorrecta, cuenta bloqueada, etc.) → re-renderiza
    /// la forma con un mensaje único e indistinguible (AC-04, NFR-03, E2.1, E2.2, E2.3).
    /// El `ReturnUrl`, si existe, es ignorado completamente (anti-open-redirect).
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> IniciarSesion(IniciarSesionViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        // Intentar autenticación
        var resultado = await signInManager.PasswordSignInAsync(
            userName: modelo.NombreDeUsuario ?? string.Empty,
            password: modelo.Contrasena ?? string.Empty,
            isPersistent: false,
            lockoutOnFailure: false);

        // Un único branch: éxito o fracaso. No se ramifica sobre IsLockedOut, IsNotAllowed,
        // RequiresTwoFactor, etc. (E2.3, Sup_FEAT-001b §Bloque 2).
        if (resultado.Succeeded)
        {
            return RedirectToAction(nameof(Privada));
        }

        // Fracaso: usuario inexistente, contraseña incorrecta, o cualquier otro motivo.
        // Mensaje único e indistinguible entre casos (AC-04).
        ModelState.AddModelError(string.Empty, "Nombre de usuario o contraseña no válidos");

        // La contraseña nunca se re-renderiza en el HTML de la respuesta de rechazo (O6, AC-06,
        // E3.1). FormTagHelper con [DataType(Password)] no renderiza el value del input, pero para
        // ser explícito y defensivo, se limpia el modelo.
        modelo.Contrasena = null;

        return View(modelo);
    }

    /// <summary>
    /// POST /Cuenta/CerrarSesion — Invalida la sesión actual y redirige al login.
    /// Exige autenticación (FallbackPolicy). Solo existe la variante POST, nunca GET.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CerrarSesion()
    {
        await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        return RedirectToAction(nameof(IniciarSesion));
    }

    /// <summary>
    /// GET / → Privada() — Página privada del usuario autenticado. Se mapea a `/` en la
    /// ruta por defecto (Program.cs). Exige autenticación (FallbackPolicy). Lee `User.Identity.Name`
    /// del `ClaimsPrincipal` sin consultar la base de datos y devuelve la vista con el nombre
    /// de la cuenta (AC-06, FR-04). No contiene datos médicos ni datos de otras cuentas.
    /// La directiva [ResponseCache] previene que el navegador cachee esta página autenticada (O5).
    /// </summary>
    [Route("/")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult Privada()
    {
        var nombreDeUsuario = User.Identity!.Name;
        var modelo = new IniciarSesionViewModel
        {
            NombreDeUsuario = nombreDeUsuario,
        };

        return View(modelo);
    }
}
