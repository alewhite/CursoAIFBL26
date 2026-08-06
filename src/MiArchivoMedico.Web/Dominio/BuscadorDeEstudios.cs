using MiArchivoMedico.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace MiArchivoMedico.Web.Dominio;

/// <summary>
/// Resuelve el listado, la búsqueda de texto y los filtros combinables sobre los estudios del usuario
/// autenticado (RF-15 a RF-17, RF-20 a RF-23).
/// </summary>
/// <remarks>
/// No hay ningún <c>Where</c> por propietario: lo aplica el filtro global del contexto, de modo que el
/// total que se informa tampoco puede incluir estudios ajenos (RNF-53, AC-49).
/// <para>
/// La búsqueda de texto consulta exclusivamente las columnas normalizadas y normaliza el término con la
/// misma función que se usó al guardar. Buscar sobre las columnas originales no cumpliría RNF-55: SQLite
/// no tiene intercalaciones insensibles a acentos.
/// </para>
/// </remarks>
public sealed class BuscadorDeEstudios
{
    /// <summary>Estudios por página (RNF-27, AC-54).</summary>
    public const int TamanoDePagina = 25;

    private readonly ArchivoMedicoDbContext _contexto;

    public BuscadorDeEstudios(ArchivoMedicoDbContext contexto) => _contexto = contexto;

    public async Task<ResultadoDeBusqueda<Estudio>> BuscarAsync(
        CriteriosDeBusqueda criterios, CancellationToken cancelacion = default)
    {
        var consulta = Filtrar(_contexto.Estudios.AsNoTracking(), criterios);

        // El total se calcula sobre la consulta filtrada y antes de paginar: es la cantidad de estudios
        // encontrados, no la de la página en curso (RF-23, AC-37).
        var total = await consulta.CountAsync(cancelacion);

        var pagina = Math.Max(1, criterios.Pagina);

        var elementos = await consulta
            .OrderByDescending(e => e.Fecha)
            .ThenByDescending(e => e.CreadoUtc)
            .Skip((pagina - 1) * TamanoDePagina)
            .Take(TamanoDePagina)
            .Include(e => e.Etiquetas)
            .Include(e => e.Archivos)
            .ToListAsync(cancelacion);

        return new ResultadoDeBusqueda<Estudio>(elementos, total, pagina, TamanoDePagina);
    }

    /// <summary>Instituciones cargadas por el usuario, para poblar el filtro (RF-20).</summary>
    public async Task<IReadOnlyList<string>> InstitucionesAsync(CancellationToken cancelacion = default)
    {
        return await _contexto.Estudios
            .AsNoTracking()
            .Where(e => e.Institucion != null && e.Institucion != string.Empty)
            .Select(e => e.Institucion!)
            .Distinct()
            .OrderBy(institucion => institucion)
            .ToListAsync(cancelacion);
    }

    private static IQueryable<Estudio> Filtrar(
        IQueryable<Estudio> consulta, CriteriosDeBusqueda criterios)
    {
        var texto = NormalizadorDeTexto.Normalizar(criterios.Texto);
        if (texto.Length > 0)
        {
            // Los cinco campos que declara RF-16, todos sobre su columna normalizada.
            consulta = consulta.Where(e =>
                e.TituloNormalizado.Contains(texto)
                || e.DescripcionNormalizada.Contains(texto)
                || e.ProfesionalNormalizado.Contains(texto)
                || e.InstitucionNormalizada.Contains(texto)
                || e.Etiquetas.Any(t => t.TextoNormalizado.Contains(texto)));
        }

        if (criterios.Desde is { } desde)
        {
            consulta = consulta.Where(e => e.Fecha >= desde);
        }

        if (criterios.Hasta is { } hasta)
        {
            consulta = consulta.Where(e => e.Fecha <= hasta);
        }

        var institucion = NormalizadorDeTexto.Normalizar(criterios.Institucion);
        if (institucion.Length > 0)
        {
            // Coincidencia exacta sobre la normalizada: el filtro elige una institución de la lista, no
            // escribe texto libre.
            consulta = consulta.Where(e => e.InstitucionNormalizada == institucion);
        }

        return consulta;
    }
}
