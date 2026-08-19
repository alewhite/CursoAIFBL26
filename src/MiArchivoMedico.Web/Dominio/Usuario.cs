using Microsoft.AspNetCore.Identity;

namespace MiArchivoMedico.Web.Dominio;

/// <summary>
/// Cuenta de un integrante del grupo familiar. Es la raíz de la propiedad de los datos, así que
/// deliberadamente NO implementa <see cref="IPropiedadDeUsuario"/>. No contiene datos médicos.
/// </summary>
public class Usuario : IdentityUser
{
}
