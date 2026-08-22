using System.ComponentModel.DataAnnotations;

namespace MiArchivoMedico.Web.ViewModels;

/// <summary>
/// Modelo para la acción de inicio de sesión (Bloque 3). Las validaciones con DataAnnotations
/// se resuelven en el servidor; los mensajes se localizan desde las resources de ASP.NET Core
/// Identity en español (NFR-03, AC-04).
/// </summary>
public class IniciarSesionViewModel
{
    /// <summary>
    /// Nombre de usuario único. Requerido, máximo 256 caracteres (NFR-02).
    /// Prohibido: [EmailAddress], [MinLength], [StringLength], [RegularExpression], [Compare]
    /// (ver spec línea 296).
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string? NombreDeUsuario { get; set; }

    /// <summary>
    /// Contraseña. Requerido. Tipo DataType.Password para que FormTagHelper emita type="password"
    /// en el input HTML. La contraseña NUNCA se re-renderiza en rechazo: el controlador la setea
    /// a null antes de devolver la vista (O6, AC-06).
    /// </summary>
    [Required]
    [DataType(DataType.Password)]
    public string? Contrasena { get; set; }
}
