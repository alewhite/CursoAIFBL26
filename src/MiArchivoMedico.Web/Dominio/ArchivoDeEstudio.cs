namespace MiArchivoMedico.Web.Dominio;

/// <summary>
/// Documento adjunto a un estudio. La fila describe el archivo; el contenido vive cifrado en disco
/// bajo un nombre físico que es el propio <see cref="Id"/>, sin ninguna porción del nombre original
/// (RNF-22).
/// </summary>
public class ArchivoDeEstudio : IPropiedadDeUsuario
{
    public const int LargoMaximoNombreOriginal = 255;

    public Guid Id { get; set; } = Guid.NewGuid();
    public string OwnerId { get; set; } = string.Empty;

    public Guid EstudioId { get; set; }
    public Estudio? Estudio { get; set; }

    /// <summary>Nombre original ya sanitizado (RNF-23).</summary>
    public string NombreOriginal { get; set; } = string.Empty;
    public string TipoDeContenido { get; set; } = string.Empty;
    public long TamanoEnBytes { get; set; }

    /// <summary>Huella del contenido en claro, calculada antes de cifrar (RNF-19).</summary>
    public string Sha256 { get; set; } = string.Empty;

    public DateTimeOffset CargadoEn { get; set; }
}
