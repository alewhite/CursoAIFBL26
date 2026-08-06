namespace MiArchivoMedico.Web.Dominio;

/// <summary>
/// Criterios combinables de búsqueda y filtrado (RF-16, RF-17, RF-20, RF-21).
/// </summary>
public sealed record CriteriosDeBusqueda(
    string? Texto = null,
    DateOnly? Desde = null,
    DateOnly? Hasta = null,
    string? Institucion = null,
    int Pagina = 1)
{
    /// <summary>Hay al menos un criterio aplicado, así que tiene sentido ofrecer limpiarlos (RF-22).</summary>
    public bool HayFiltrosAplicados =>
        !string.IsNullOrWhiteSpace(Texto)
        || Desde is not null
        || Hasta is not null
        || !string.IsNullOrWhiteSpace(Institucion);
}

public sealed record ResultadoDeBusqueda<T>(
    IReadOnlyList<T> Elementos,
    int Total,
    int Pagina,
    int TamanoDePagina)
{
    public int TotalDePaginas => Total == 0 ? 1 : (int)Math.Ceiling(Total / (double)TamanoDePagina);

    public bool HayPaginaAnterior => Pagina > 1;

    public bool HayPaginaSiguiente => Pagina < TotalDePaginas;
}
