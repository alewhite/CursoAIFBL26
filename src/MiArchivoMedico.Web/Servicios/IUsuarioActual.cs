using System.Security.Claims;

namespace MiArchivoMedico.Web.Servicios;

/// <summary>
/// Identidad de la cuenta de la solicitud en curso. <see cref="Id"/> vale null cuando no hay sesión, y
/// como ninguna fila iguala a null, una consulta sin autenticar devuelve vacío en lugar de devolver
/// todo (RNF-53).
/// </summary>
public interface IUsuarioActual
{
    string? Id { get; }
}

public sealed class UsuarioActual(IHttpContextAccessor accesor) : IUsuarioActual
{
    public string? Id => accesor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
}

/// <summary>Usuario actual fijo, para procesos sin solicitud HTTP como la siembra al arrancar.</summary>
public sealed class UsuarioActualFijo(string? id) : IUsuarioActual
{
    public string? Id { get; } = id;
}
