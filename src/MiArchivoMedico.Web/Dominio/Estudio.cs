using MiArchivoMedico.Web.Servicios;

namespace MiArchivoMedico.Web.Dominio;

/// <summary>
/// Unidad de organización del repositorio. Pertenece a exactamente una cuenta y nunca cambia de
/// propietario (RNF-57).
/// </summary>
public class Estudio : IPropiedadDeUsuario, ITieneColumnasNormalizadas
{
    public const int LargoMaximoTitulo = 200;
    public const int LargoMaximoProfesional = 200;
    public const int LargoMaximoInstitucion = 200;
    public const int LargoMaximoDescripcion = 2000;

    public Guid Id { get; set; } = Guid.NewGuid();
    public string OwnerId { get; set; } = string.Empty;

    public string Titulo { get; set; } = string.Empty;
    public string TituloNormalizado { get; set; } = string.Empty;

    /// <summary>Fecha de calendario sin hora, no posterior al día en curso (RF-37).</summary>
    public DateOnly Fecha { get; set; }

    public string? Profesional { get; set; }
    public string ProfesionalNormalizado { get; set; } = string.Empty;

    public string? Institucion { get; set; }
    public string InstitucionNormalizada { get; set; } = string.Empty;

    public string? Descripcion { get; set; }
    public string DescripcionNormalizada { get; set; } = string.Empty;

    public DateTimeOffset CreadoEn { get; set; }

    public List<ArchivoDeEstudio> Archivos { get; set; } = [];
    public List<EtiquetaDeEstudio> Etiquetas { get; set; } = [];

    public void RecalcularNormalizados()
    {
        TituloNormalizado = NormalizadorDeTexto.Normalizar(Titulo);
        ProfesionalNormalizado = NormalizadorDeTexto.Normalizar(Profesional);
        InstitucionNormalizada = NormalizadorDeTexto.Normalizar(Institucion);
        DescripcionNormalizada = NormalizadorDeTexto.Normalizar(Descripcion);
    }
}
