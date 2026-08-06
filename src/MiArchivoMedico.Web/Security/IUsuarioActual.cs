using System.Security.Claims;

namespace MiArchivoMedico.Web.Security;

/// <summary>
/// Identidad del usuario autenticado en la solicitud en curso. Es la fuente del filtro global
/// por propietario (RNF-53).
/// </summary>
public interface IUsuarioActual
{
    /// <summary>Identificador técnico del usuario autenticado, o <c>null</c> si no hay sesión.</summary>
    string? Id { get; }
}

public sealed class UsuarioActual : IUsuarioActual
{
    private readonly IHttpContextAccessor _accessor;

    public UsuarioActual(IHttpContextAccessor accessor) => _accessor = accessor;

    /// <summary>
    /// Sin sesión devuelve <c>null</c>: ninguna fila con propietario puede igualar ese valor, de modo que
    /// una consulta sin autenticar no devuelve datos médicos (RNF-51).
    /// </summary>
    public string? Id => _accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
}
