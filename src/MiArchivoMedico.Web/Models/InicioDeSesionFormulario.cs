using System.ComponentModel.DataAnnotations;

namespace MiArchivoMedico.Web.Models;

public sealed class InicioDeSesionFormulario
{
    [Required(ErrorMessage = "Ingresá tu nombre de usuario.")]
    [Display(Name = "Nombre de usuario")]
    public string NombreDeUsuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresá tu contraseña.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Contrasena { get; set; } = string.Empty;
}
