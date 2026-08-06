using MiArchivoMedico.Web.Dominio;

namespace MiArchivoMedico.Web.Data;

/// <summary>Etiqueta de texto libre asociada a un estudio (RF-32).</summary>
public class EtiquetaDeEstudio : IPropiedadDeUsuario
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid EstudioId { get; set; }

    /// <summary>
    /// Redundante con el del estudio, y a propósito: sin esta columna una consulta directa sobre etiquetas
    /// quedaría fuera del filtro global por propietario (RNF-53).
    /// </summary>
    public string OwnerId { get; set; } = string.Empty;

    public string Texto { get; set; } = string.Empty;

    public string TextoNormalizado { get; private set; } = string.Empty;

    public void RecalcularNormalizados() => TextoNormalizado = NormalizadorDeTexto.Normalizar(Texto);
}
