namespace MiArchivoMedico.Web.Archivos;

/// <summary>
/// Configuración del almacenamiento de archivos médicos. Se resuelve desde configuración externa; si la
/// clave falta, la aplicación falla al arrancar en vez de guardar archivos en claro (RNF-02, RNF-58, RNF-62).
/// </summary>
public sealed class OpcionesDeAlmacenamiento
{
    public const string Seccion = "Almacenamiento";

    /// <summary>Carpeta en disco del servidor, fuera de toda carpeta pública.</summary>
    public string Ruta { get; set; } = string.Empty;

    /// <summary>Clave AES-256 en base64: exactamente 32 bytes.</summary>
    public string ClaveBase64 { get; set; } = string.Empty;

    /// <summary>Cupo total compartido entre las hasta 5 cuentas, sin cuota individual (RNF-52).</summary>
    public long CupoTotalEnBytes { get; set; } = 20L * 1024 * 1024 * 1024;

    /// <summary>
    /// Valida y devuelve la clave. Lanza si falta o es inválida: es el camino que hace fallar el arranque
    /// (AC-83).
    /// </summary>
    public byte[] ResolverClave()
    {
        if (string.IsNullOrWhiteSpace(ClaveBase64))
        {
            throw new InvalidOperationException(
                $"Falta '{Seccion}:ClaveBase64'. Sin la clave de cifrado la aplicación no arranca: " +
                "guardar archivos médicos sin cifrar no es una alternativa aceptable (RNF-02, RNF-62).");
        }

        byte[] clave;
        try
        {
            clave = Convert.FromBase64String(ClaveBase64);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException($"'{Seccion}:ClaveBase64' no es base64 válido.");
        }

        return clave.Length == 32
            ? clave
            : throw new InvalidOperationException(
                $"'{Seccion}:ClaveBase64' debe decodificar a 32 bytes (AES-256); decodificó a {clave.Length}.");
    }
}
