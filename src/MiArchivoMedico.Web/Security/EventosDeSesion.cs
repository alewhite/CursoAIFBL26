using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;

namespace MiArchivoMedico.Web.Security;

/// <summary>
/// Duración absoluta de la sesión (RNF-05, AC-07). La expiración por inactividad la resuelve la cookie
/// deslizante de 30 minutos (RNF-04, AC-06); el tope absoluto necesita recordar cuándo se inició la sesión.
/// </summary>
/// <remarks>
/// El instante de inicio vive en <see cref="Microsoft.AspNetCore.Authentication.AuthenticationProperties"/>,
/// no en un claim: la validación del sello de seguridad reemplaza el principal pero conserva las
/// propiedades, de modo que un refresco no reinicia el tope de 24 horas.
/// </remarks>
public sealed class EventosDeSesion : CookieAuthenticationEvents
{
    public const string ClaveInicioSesion = "sesion:inicio-utc";

    public static readonly TimeSpan DuracionAbsoluta = TimeSpan.FromHours(24);

    private readonly TimeProvider _reloj;

    public EventosDeSesion(TimeProvider reloj) => _reloj = reloj;

    public override Task SigningIn(CookieSigningInContext context)
    {
        if (!context.Properties.Items.ContainsKey(ClaveInicioSesion))
        {
            context.Properties.Items[ClaveInicioSesion] =
                _reloj.GetUtcNow().ToString("O", CultureInfo.InvariantCulture);
        }

        return base.SigningIn(context);
    }

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        if (!EstaDentroDeLaDuracionAbsoluta(context))
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            return;
        }

        // Delegar en Identity para que el cierre de sesión y los cambios de credenciales invaliden
        // las cookies vigentes (RNF-12, AC-05).
        await SecurityStampValidator.ValidatePrincipalAsync(context);
    }

    private bool EstaDentroDeLaDuracionAbsoluta(CookieValidatePrincipalContext context)
    {
        // Una cookie sin la marca de inicio no se puede acotar: se rechaza en lugar de asumir que es reciente.
        if (!context.Properties.Items.TryGetValue(ClaveInicioSesion, out var valor) ||
            !DateTimeOffset.TryParse(valor, CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out var inicio))
        {
            return false;
        }

        return _reloj.GetUtcNow() < inicio + DuracionAbsoluta;
    }
}
