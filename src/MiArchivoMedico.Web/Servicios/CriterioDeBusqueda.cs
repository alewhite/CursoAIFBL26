namespace MiArchivoMedico.Web.Servicios;

/// <summary>
/// Lo que el usuario pidió: término, rango de fechas, institución y página. Viaja en el cuerpo de la
/// solicitud y se guarda en la sesión; nunca en la dirección, porque el término es un dato médico
/// (RNF-63).
/// </summary>
public sealed class CriterioDeBusqueda
{
    public string? Termino { get; set; }
    public DateOnly? Desde { get; set; }
    public DateOnly? Hasta { get; set; }
    public string? Institucion { get; set; }
    public int Pagina { get; set; } = 1;

    public bool HayFiltrosAplicados =>
        !string.IsNullOrWhiteSpace(Termino)
        || Desde is not null
        || Hasta is not null
        || !string.IsNullOrWhiteSpace(Institucion);

    public CriterioDeBusqueda EnPagina(int pagina) => new()
    {
        Termino = Termino,
        Desde = Desde,
        Hasta = Hasta,
        Institucion = Institucion,
        Pagina = Math.Max(1, pagina),
    };
}

public sealed record PaginaDeEstudios(
    IReadOnlyList<Dominio.Estudio> Estudios,
    int TotalDeResultados,
    int PaginaActual,
    int TotalDePaginas)
{
    public bool HayAnterior => PaginaActual > 1;
    public bool HaySiguiente => PaginaActual < TotalDePaginas;
}
