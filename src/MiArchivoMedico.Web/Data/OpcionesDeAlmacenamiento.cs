namespace MiArchivoMedico.Web.Data;

/// <summary>Configuración externa del almacenamiento. Sin clave, la aplicación no arranca (RNF-62).</summary>
public sealed class OpcionesDeAlmacenamiento
{
    public const long CupoPorOmisionEnBytes = 20L * 1024 * 1024 * 1024;

    public string Ruta { get; set; } = string.Empty;
    public string ClaveBase64 { get; set; } = string.Empty;
    public long CupoTotalEnBytes { get; set; } = CupoPorOmisionEnBytes;

    public string RutaDeTransito => Path.Combine(Ruta, "transito");
}

/// <summary>Una cuenta del alta administrativa. El alta vive fuera de la aplicación (RNF-54).</summary>
public sealed class CuentaInicial
{
    public const int LargoMinimoDeContrasena = 12;

    public string NombreDeUsuario { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Contrasena { get; set; } = string.Empty;
}
