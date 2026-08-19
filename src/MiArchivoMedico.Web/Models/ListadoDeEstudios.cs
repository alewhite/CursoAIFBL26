using MiArchivoMedico.Web.Servicios;

namespace MiArchivoMedico.Web.Models;

public sealed class ListadoDeEstudios
{
    public required PaginaDeEstudios Pagina { get; init; }
    public required CriterioDeBusqueda Criterio { get; init; }
    public required IReadOnlyList<string> Instituciones { get; init; }

    /// <summary>La cuenta no tiene ningún estudio, con independencia del criterio aplicado.</summary>
    public required bool CuentaSinEstudios { get; init; }

    /// <summary>Hay estudios, pero el criterio no devolvió ninguno.</summary>
    public bool BusquedaSinResultados => !CuentaSinEstudios && Pagina.TotalDeResultados == 0;
}
