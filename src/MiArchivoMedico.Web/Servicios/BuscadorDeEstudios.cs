using Microsoft.EntityFrameworkCore;
using MiArchivoMedico.Web.Data;
using MiArchivoMedico.Web.Dominio;

namespace MiArchivoMedico.Web.Servicios;

/// <summary>
/// Única implementación de búsqueda del proyecto. Consulta las columnas normalizadas y normaliza el
/// término entrante con <see cref="NormalizadorDeTexto.Normalizar"/>, la misma función que usó la
/// interceptación al persistir: si las dos puntas divergen, la búsqueda deja de coincidir (RNF-55).
///
/// No filtra por propietario: eso ya lo hacen los filtros globales del contexto (RNF-53).
/// </summary>
public sealed class BuscadorDeEstudios(ArchivoMedicoDbContext contexto)
{
    public const int EstudiosPorPagina = 25;

    public async Task<PaginaDeEstudios> BuscarAsync(CriterioDeBusqueda criterio)
    {
        var consulta = Aplicar(criterio);

        var total = await consulta.CountAsync();
        var totalDePaginas = Math.Max(1, (int)Math.Ceiling(total / (double)EstudiosPorPagina));

        // Un filtro puede dejar la página actual fuera de rango; se resuelve en una existente y no en
        // una vacía.
        var pagina = Math.Clamp(criterio.Pagina, 1, totalDePaginas);

        var estudios = await consulta
            .OrderByDescending(e => e.Fecha)
            .ThenByDescending(e => e.CreadoEn)
            .Skip((pagina - 1) * EstudiosPorPagina)
            .Take(EstudiosPorPagina)
            .ToListAsync();

        return new PaginaDeEstudios(estudios, total, pagina, totalDePaginas);
    }

    /// <summary>Instituciones presentes en los estudios del propio usuario, para el filtro (RF-38).</summary>
    public async Task<IReadOnlyList<string>> InstitucionesAsync() =>
        await contexto.Estudios
            .Where(e => e.Institucion != null && e.Institucion != string.Empty)
            .Select(e => e.Institucion!)
            .Distinct()
            .OrderBy(i => i)
            .ToListAsync();

    /// <summary>Hay al menos un estudio propio, con independencia del criterio aplicado.</summary>
    public Task<bool> TieneAlgunEstudioAsync() => contexto.Estudios.AnyAsync();

    private IQueryable<Estudio> Aplicar(CriterioDeBusqueda criterio)
    {
        var consulta = contexto.Estudios.AsQueryable();

        var termino = NormalizadorDeTexto.Normalizar(criterio.Termino);
        if (termino.Length > 0)
        {
            var patron = $"%{termino}%";
            consulta = consulta.Where(e =>
                EF.Functions.Like(e.TituloNormalizado, patron)
                || EF.Functions.Like(e.DescripcionNormalizada, patron)
                || EF.Functions.Like(e.ProfesionalNormalizado, patron)
                || EF.Functions.Like(e.InstitucionNormalizada, patron)
                || e.Etiquetas.Any(t => EF.Functions.Like(t.TextoNormalizado, patron)));
        }

        // Ambos extremos incluidos y cada uno opcional por separado (FR-049).
        if (criterio.Desde is { } desde) consulta = consulta.Where(e => e.Fecha >= desde);
        if (criterio.Hasta is { } hasta) consulta = consulta.Where(e => e.Fecha <= hasta);

        if (!string.IsNullOrWhiteSpace(criterio.Institucion))
        {
            // Coincidencia exacta sobre el valor elegido de la lista, no por subcadena.
            var institucion = criterio.Institucion;
            consulta = consulta.Where(e => e.Institucion == institucion);
        }

        return consulta;
    }
}
