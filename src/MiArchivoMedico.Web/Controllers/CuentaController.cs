using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MiArchivoMedico.Web.Dominio;
using MiArchivoMedico.Web.Models;
using MiArchivoMedico.Web.Servicios;

namespace MiArchivoMedico.Web.Controllers;

public class CuentaController(
    SignInManager<Usuario> ingreso,
    UserManager<Usuario> administrador,
    IPasswordHasher<Usuario> hasher,
    ControlDeIntentosDeInicioDeSesion intentos,
    TimeProvider reloj,
    ILogger<CuentaController> registro) : Controller
{
    /// <summary>
    /// Mensaje único para cuenta inexistente, contraseña incorrecta y cuenta bloqueada. Distinguirlos
    /// permitiría enumerar cuentas (RNF-13, RNF-60, RNF-65).
    /// </summary>
    private const string MensajeDeRechazo = "No pudimos iniciar tu sesión. Revisá tus datos e intentá de nuevo.";

    /// <summary>Claim con el momento del ingreso, base del tope absoluto de 24 horas (RNF-05).</summary>
    public const string ClaimDeInicioDeSesion = "inicio-de-sesion";

    /// <summary>
    /// Hash señuelo con el que se verifica una contraseña cuando la cuenta no existe, para que la
    /// demora sea comparable y el comportamiento observable no delate la ausencia (RNF-65).
    /// </summary>
    private static readonly Lazy<string> HashSenuelo = new(() => string.Empty);

    [AllowAnonymous]
    [HttpGet]
    public IActionResult InicioDeSesion()
    {
        ViewData["Titulo"] = "Iniciar sesión";
        return View(new InicioDeSesionFormulario());
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> InicioDeSesion(InicioDeSesionFormulario formulario)
    {
        ViewData["Titulo"] = "Iniciar sesión";
        if (!ModelState.IsValid) return View(formulario);

        var usuario = await administrador.FindByNameAsync(formulario.NombreDeUsuario);

        if (await intentos.EstaBloqueadoAsync(formulario.NombreDeUsuario))
        {
            // Se verifica igual contra el señuelo para no acortar la respuesta durante el bloqueo.
            VerificarContraSenuelo(formulario.Contrasena);
            await intentos.RegistrarFalloAsync(formulario.NombreDeUsuario);
            return Rechazar(formulario);
        }

        if (usuario is null)
        {
            VerificarContraSenuelo(formulario.Contrasena);
            await intentos.RegistrarFalloAsync(formulario.NombreDeUsuario);
            return Rechazar(formulario);
        }

        var verificacion = hasher.VerifyHashedPassword(usuario, usuario.PasswordHash ?? string.Empty, formulario.Contrasena);
        if (verificacion == PasswordVerificationResult.Failed)
        {
            await intentos.RegistrarFalloAsync(formulario.NombreDeUsuario);
            return Rechazar(formulario);
        }

        if (verificacion == PasswordVerificationResult.SuccessRehashNeeded)
        {
            // Credencial válida derivada con parámetros por debajo del mínimo vigente: se recalcula.
            usuario.PasswordHash = hasher.HashPassword(usuario, formulario.Contrasena);
            await administrador.UpdateAsync(usuario);
        }

        await intentos.ReiniciarAsync(formulario.NombreDeUsuario);

        // Renueva la marca de seguridad: invalida la sesión que la cuenta tuviera en otro dispositivo
        // y cierra de paso la fijación de sesión (RNF-68).
        await administrador.UpdateSecurityStampAsync(usuario);

        await ingreso.SignInWithClaimsAsync(usuario, isPersistent: false,
            [new Claim(ClaimDeInicioDeSesion, reloj.GetUtcNow().UtcTicks.ToString())]);

        registro.LogInformation("Inicio de sesión correcto para la cuenta {CuentaId}.", usuario.Id);
        return RedirectToAction("Index", "Estudios");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CerrarSesion()
    {
        await ingreso.SignOutAsync();
        return RedirectToAction(nameof(InicioDeSesion));
    }

    private IActionResult Rechazar(InicioDeSesionFormulario formulario)
    {
        // Nunca se registra el nombre tecleado: podría ser un dato identificable (RNF-09).
        registro.LogWarning("Intento de inicio de sesión rechazado.");
        formulario.Contrasena = string.Empty;
        ModelState.AddModelError(string.Empty, MensajeDeRechazo);
        return View(formulario);
    }

    private void VerificarContraSenuelo(string contrasena)
    {
        var senuelo = HashSenuelo.Value;
        if (senuelo.Length == 0)
        {
            // Se deriva una vez con el mismo costo que una verificación real.
            _ = hasher.HashPassword(new Usuario { UserName = "senuelo" }, contrasena);
            return;
        }

        _ = hasher.VerifyHashedPassword(new Usuario { UserName = "senuelo" }, senuelo, contrasena);
    }
}
