using System.ComponentModel.DataAnnotations;

namespace MiArchivoMedico.Web.Models;

public class InicioDeSesionViewModel
{
    [Required(ErrorMessage = "Ingresá tu usuario.")]
    [Display(Name = "Usuario")]
    public string NombreDeUsuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresá tu contraseña.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Contrasena { get; set; } = string.Empty;
}
