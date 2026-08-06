namespace MiArchivoMedico.Web.Data;

/// <summary>
/// Cuenta declarada en la configuración externa (user-secrets o variables de entorno) para el alta
/// administrativa. Nunca se versiona en <c>appsettings.json</c>.
/// </summary>
public sealed class CuentaInicial
{
    public string NombreDeUsuario { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Contrasena { get; set; } = string.Empty;
}
