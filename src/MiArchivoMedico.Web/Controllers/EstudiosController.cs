using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiArchivoMedico.Web.Data;

namespace MiArchivoMedico.Web.Controllers;

/// <summary>
/// Los filtros globales de <see cref="ArchivoMedicoDbContext"/> ya restringen todo a la cuenta de la
/// sesión, así que acá no se filtra por propietario a mano. Un identificador ajeno simplemente no
/// aparece, y por eso el 404 sale del camino natural del código en vez de un chequeo explícito.
/// </summary>
public class EstudiosController(ArchivoMedicoDbContext contexto) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewData["Titulo"] = "Mis estudios";
        var estudios = await contexto.Estudios
            .OrderByDescending(e => e.Fecha)
            .ThenByDescending(e => e.CreadoEn)
            .ToListAsync();

        return View(estudios);
    }

    [HttpGet]
    public async Task<IActionResult> Detalle(Guid id)
    {
        var estudio = await contexto.Estudios
            .Include(e => e.Etiquetas)
            .Include(e => e.Archivos)
            .FirstOrDefaultAsync(e => e.Id == id);

        // Ajeno, inexistente y mal formado responden lo mismo: 404 sin cuerpo que los distinga.
        if (estudio is null) return NoEncontrado();

        ViewData["Titulo"] = "Detalle del estudio";
        return View(estudio);
    }

    /// <summary>
    /// Respuesta única para recurso ajeno, inexistente o identificador mal formado (RNF-53). No lleva
    /// cuerpo ni mensaje: revelar la diferencia permitiría enumerar los estudios de otras cuentas.
    /// </summary>
    private IActionResult NoEncontrado() => NotFound();
}
