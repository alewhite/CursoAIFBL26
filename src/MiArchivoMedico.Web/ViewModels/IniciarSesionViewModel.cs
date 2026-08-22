namespace MiArchivoMedico.Web.ViewModels;

/// <summary>
/// Modelo para la acción de inicio de sesión. Las anotaciones de validación y las reglas de
/// composición de DataAnnotations son responsabilidad de Bloque 3 (vistas y ViewModel final). Este
/// tipo mínimo permite que el controlador compile y reciba el modelo enlazado del form.
/// </summary>
public class IniciarSesionViewModel
{
    public string? NombreDeUsuario { get; set; }

    public string? Contrasena { get; set; }
}
