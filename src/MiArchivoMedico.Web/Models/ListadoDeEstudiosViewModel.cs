using System.ComponentModel.DataAnnotations;
using MiArchivoMedico.Web.Data;
using MiArchivoMedico.Web.Dominio;

namespace MiArchivoMedico.Web.Models;

public class ListadoDeEstudiosViewModel
{
    [Display(Name = "Buscar")]
    public string? Texto { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Desde")]
    public DateOnly? Desde { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Hasta")]
    public DateOnly? Hasta { get; set; }

    [Display(Name = "Institución")]
    public string? Institucion { get; set; }

    public int Pagina { get; set; } = 1;

    public ResultadoDeBusqueda<Estudio> Resultado { get; set; } =
        new([], 0, 1, BuscadorDeEstudios.TamanoDePagina);

    public IReadOnlyList<string> Instituciones { get; set; } = [];

    public CriteriosDeBusqueda ACriterios() => new(Texto, Desde, Hasta, Institucion, Pagina);

    public bool HayFiltrosAplicados => ACriterios().HayFiltrosAplicados;

    /// <summary>Los filtros vigentes, para que los enlaces de paginación no los pierdan.</summary>
    public Dictionary<string, string?> ComoRuta(int? pagina = null) => new()
    {
        ["Texto"] = Texto,
        ["Desde"] = Desde?.ToString("yyyy-MM-dd"),
        ["Hasta"] = Hasta?.ToString("yyyy-MM-dd"),
        ["Institucion"] = Institucion,
        ["Pagina"] = (pagina ?? Pagina).ToString(),
    };
}
