using Microsoft.AspNetCore.Identity;

namespace MiArchivoMedico.Web.Data;

/// <summary>
/// Cuenta de la aplicación. El alta es administrativa, fuera de la interfaz (RNF-54).
/// </summary>
public class UsuarioApp : IdentityUser
{
    /// <summary>
    /// Instante del último intento de inicio de sesión fallido. Identity acumula los fallos sin caducidad;
    /// RNF-60 exige contarlos dentro de una ventana de 15 minutos, y esa ventana necesita esta marca.
    /// </summary>
    public DateTimeOffset? UltimoIntentoFallidoUtc { get; set; }
}
