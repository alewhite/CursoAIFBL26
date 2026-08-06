using MiArchivoMedico.Web.Dominio;

namespace MiArchivoMedico.Web.Data;

/// <summary>
/// Estudio médico: un conjunto de metadatos cargados a mano más los archivos asociados
/// (RF-33, RF-29 a RF-32, RF-07).
/// </summary>
public class Estudio : IPropiedadDeUsuario
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string OwnerId { get; set; } = string.Empty;

    public string Titulo { get; set; } = string.Empty;

    public DateOnly Fecha { get; set; }

    public string? Profesional { get; set; }

    public string? Institucion { get; set; }

    public string? Descripcion { get; set; }

    public DateTimeOffset CreadoUtc { get; set; }

    public List<EtiquetaDeEstudio> Etiquetas { get; set; } = [];

    public List<ArchivoDeEstudio> Archivos { get; set; } = [];

    // Columnas normalizadas para búsqueda insensible a mayúsculas y acentos (RNF-55). Se recalculan
    // siempre desde los campos originales; nunca se escriben a mano.
    public string TituloNormalizado { get; private set; } = string.Empty;

    public string ProfesionalNormalizado { get; private set; } = string.Empty;

    public string InstitucionNormalizada { get; private set; } = string.Empty;

    public string DescripcionNormalizada { get; private set; } = string.Empty;

    /// <summary>
    /// Recalcula las columnas normalizadas. Se invoca desde <c>SaveChanges</c>, de modo que no exista
    /// ningún camino que guarde un estudio con la normalización desactualizada.
    /// </summary>
    public void RecalcularNormalizados()
    {
        TituloNormalizado = NormalizadorDeTexto.Normalizar(Titulo);
        ProfesionalNormalizado = NormalizadorDeTexto.Normalizar(Profesional);
        InstitucionNormalizada = NormalizadorDeTexto.Normalizar(Institucion);
        DescripcionNormalizada = NormalizadorDeTexto.Normalizar(Descripcion);
    }
}
