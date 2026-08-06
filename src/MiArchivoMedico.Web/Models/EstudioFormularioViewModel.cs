using System.ComponentModel.DataAnnotations;

namespace MiArchivoMedico.Web.Models;

public class EstudioFormularioViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "El título es obligatorio.")]   // RF-34, AC-10
    [StringLength(200)]
    [Display(Name = "Título")]
    public string Titulo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresá una fecha válida.")]   // RF-35, AC-11
    [DataType(DataType.Date)]
    [Display(Name = "Fecha")]
    public DateOnly? Fecha { get; set; }

    [StringLength(200)]
    [Display(Name = "Profesional")]
    public string? Profesional { get; set; }

    [StringLength(200)]
    [Display(Name = "Institución")]
    public string? Institucion { get; set; }

    [StringLength(2000)]
    [Display(Name = "Descripción")]
    public string? Descripcion { get; set; }

    /// <summary>Etiquetas separadas por coma: un solo campo, para no agregar pasos al alta (RNF-31).</summary>
    [Display(Name = "Etiquetas (separadas por coma)")]
    public string? Etiquetas { get; set; }

    [Display(Name = "Archivos")]
    public List<IFormFile> Archivos { get; set; } = [];

    public IEnumerable<string> EtiquetasSeparadas() =>
        (Etiquetas ?? string.Empty)
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase);
}
