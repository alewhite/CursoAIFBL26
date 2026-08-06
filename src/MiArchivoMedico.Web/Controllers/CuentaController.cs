using MiArchivoMedico.Web.Data;
using MiArchivoMedico.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MiArchivoMedico.Web.Controllers;

/// <summary>
/// Inicio y cierre de sesión. No expone ninguna acción de registro ni de recuperación de contraseña:
/// el alta y el restablecimiento son administrativos (RNF-54, AC-50).
/// </summary>
[Authorize]
public class CuentaController : Controller
{
    /// <summary>
    /// Único mensaje de fallo del inicio de sesión. Es idéntico para usuario inexistente, contraseña
    /// incorrecta y cuenta bloqueada, para no revelar cuál de los tres ocurrió (RNF-13, RNF-60, AC-04, AC-69).
    /// </summary>
    private const string MensajeDeCredencialesInvalidas = "Usuario o contraseña incorrectos.";

    /// <summary>Ventana dentro de la cual se acumulan los intentos fallidos (RNF-60).</summary>
    private static readonly TimeSpan VentanaDeIntentosFallidos = TimeSpan.FromMinutes(15);

    private readonly SignInManager<UsuarioApp> _inicioDeSesion;
    private readonly UserManager<UsuarioApp> _usuarios;
    private readonly TimeProvider _reloj;

    public CuentaController(
        SignInManager<UsuarioApp> inicioDeSesion,
        UserManager<UsuarioApp> usuarios,
        TimeProvider reloj)
    {
        _inicioDeSesion = inicioDeSesion;
        _usuarios = usuarios;
        _reloj = reloj;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new InicioDeSesionViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(InicioDeSesionViewModel modelo, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var usuario = await _usuarios.FindByNameAsync(modelo.NombreDeUsuario);
        if (usuario is not null)
        {
            await ReiniciarContadorSiVencioLaVentanaAsync(usuario);
        }

        // lockoutOnFailure: true activa el bloqueo tras 5 fallos (RNF-60).
        var resultado = await _inicioDeSesion.PasswordSignInAsync(
            modelo.NombreDeUsuario, modelo.Contrasena, isPersistent: false, lockoutOnFailure: true);

        if (resultado.Succeeded)
        {
            return RedirigirTrasIniciarSesion(returnUrl);
        }

        if (usuario is not null)
        {
            usuario.UltimoIntentoFallidoUtc = _reloj.GetUtcNow();
            await _usuarios.UpdateAsync(usuario);
        }

        // Un solo mensaje para todas las causas de fallo: no se distingue bloqueo de credencial inválida.
        ModelState.AddModelError(string.Empty, MensajeDeCredencialesInvalidas);
        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _inicioDeSesion.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccesoDenegado() => View();

    /// <summary>
    /// Identity acumula los intentos fallidos sin caducidad. RNF-60 los cuenta dentro de una ventana de
    /// 15 minutos: si el último fallo quedó fuera de la ventana y la cuenta no está bloqueada, el contador
    /// arranca de cero.
    /// </summary>
    private async Task ReiniciarContadorSiVencioLaVentanaAsync(UsuarioApp usuario)
    {
        if (usuario.AccessFailedCount == 0 || await _usuarios.IsLockedOutAsync(usuario))
        {
            return;
        }

        var ultimoFallo = usuario.UltimoIntentoFallidoUtc;
        if (ultimoFallo is null || _reloj.GetUtcNow() - ultimoFallo.Value >= VentanaDeIntentosFallidos)
        {
            await _usuarios.ResetAccessFailedCountAsync(usuario);
        }
    }

    private IActionResult RedirigirTrasIniciarSesion(string? returnUrl)
    {
        // Solo URLs locales: un destino externo convertiría el inicio de sesión en un redirector abierto.
        return Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl!)
            : RedirectToAction("Index", "Home");
    }
}
