using System.ComponentModel.DataAnnotations;
using MiArchivoMedico.Web.Dominio;
using MiArchivoMedico.Web.Servicios;

namespace MiArchivoMedico.Web.Models;

public sealed class EstudioFormulario
{
    [Required(ErrorMessage = "El título es obligatorio.")]
    [StringLength(Estudio.LargoMaximoTitulo, ErrorMessage = "El título no puede superar los 200 caracteres.")]
    public string Titulo { get; set; } = string.Empty;

    [Required(ErrorMessage = "La fecha es obligatoria.")]
    [DataType(DataType.Date)]
    public DateOnly? Fecha { get; set; }

    [StringLength(Estudio.LargoMaximoProfesional, ErrorMessage = "El profesional no puede superar los 200 caracteres.")]
    public string? Profesional { get; set; }

    [StringLength(Estudio.LargoMaximoInstitucion, ErrorMessage = "La institución no puede superar los 200 caracteres.")]
    public string? Institucion { get; set; }

    [StringLength(Estudio.LargoMaximoDescripcion, ErrorMessage = "La descripción no puede superar los 2.000 caracteres.")]
    public string? Descripcion { get; set; }

    /// <summary>Etiquetas separadas por coma, tal como las escribe el usuario.</summary>
    public string? Etiquetas { get; set; }

    /// <summary>Marca de un solo uso que impide que un reenvío cree el estudio dos veces (RNF-66).</summary>
    public string? MarcaDeEnvio { get; set; }

    public List<IFormFile> Archivos { get; set; } = [];

    /// <summary>Rechazos informados por archivo, para mostrarlos junto a su origen (RNF-32).</summary>
    public List<ArchivoRechazado> Rechazados { get; set; } = [];

    /// <summary>Se enciende cuando el formulario vuelve por validación y hay archivos que readjuntar.</summary>
    public bool DebeReadjuntarArchivos { get; set; }

    public IEnumerable<string> EtiquetasSeparadas() =>
        (Etiquetas ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(e => e.Length <= EtiquetaDeEstudio.LargoMaximoTexto)
            .Distinct(StringComparer.OrdinalIgnoreCase);
}
