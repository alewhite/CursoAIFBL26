namespace MiArchivoMedico.Web.Data;

/// <summary>Archivo asociado a un estudio (RF-07, RF-08).</summary>
public class ArchivoDeEstudio : IPropiedadDeUsuario
{
    /// <summary>
    /// También es el nombre físico en el almacenamiento: un GUID generado por el sistema, sin ninguna
    /// porción derivada del nombre original (RNF-22, AC-65).
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid EstudioId { get; set; }

    /// <summary>Ver la nota de <see cref="EtiquetaDeEstudio.OwnerId"/>: el filtro global lo necesita.</summary>
    public string OwnerId { get; set; } = string.Empty;

    /// <summary>Nombre que subió el usuario, ya sanitizado (RNF-23, AC-66).</summary>
    public string NombreOriginal { get; set; } = string.Empty;

    public string TipoMime { get; set; } = string.Empty;

    public long TamanoEnBytes { get; set; }

    /// <summary>Hash SHA-256 del contenido en claro, en hexadecimal (RNF-19, AC-25).</summary>
    public string HashSha256 { get; set; } = string.Empty;

    public DateTimeOffset CargadoUtc { get; set; }

    public Estudio? Estudio { get; set; }

    public bool EsImagen => TipoMime is "image/jpeg" or "image/png";
}
