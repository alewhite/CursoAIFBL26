using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiArchivoMedico.Web.Data;
using MiArchivoMedico.Web.Dominio;
using MiArchivoMedico.Web.Models;
using MiArchivoMedico.Web.Servicios;

namespace MiArchivoMedico.Web.Controllers;

/// <summary>
/// Los filtros globales de <see cref="ArchivoMedicoDbContext"/> ya restringen todo a la cuenta de la
/// sesión, así que acá no se filtra por propietario a mano. Un identificador ajeno simplemente no
/// aparece, y por eso el 404 sale del camino natural del código en vez de un chequeo explícito.
/// </summary>
public class EstudiosController(
    ArchivoMedicoDbContext contexto,
    ServicioDeCargaDeArchivos carga,
    TimeProvider reloj) : Controller
{
    private const string ClaveDeMarcasUsadas = "marcas-de-envio-usadas";

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

    [HttpGet]
    public IActionResult Crear()
    {
        ViewData["Titulo"] = "Nuevo estudio";
        return View(new EstudioFormulario { MarcaDeEnvio = Guid.NewGuid().ToString("N") });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(EstudioFormulario formulario)
    {
        ViewData["Titulo"] = "Nuevo estudio";

        // Un reenvío del mismo formulario no crea un segundo estudio, aunque el navegador no ejecute
        // JavaScript (RNF-66, AC-99).
        if (!ConsumirMarcaDeEnvio(formulario.MarcaDeEnvio))
            return RedirectToAction(nameof(Index));

        if (formulario.Fecha is { } fecha && fecha > DateOnly.FromDateTime(reloj.GetUtcNow().UtcDateTime))
            ModelState.AddModelError(nameof(EstudioFormulario.Fecha),
                "La fecha no puede ser posterior al día de hoy.");

        if (!ModelState.IsValid) return DevolverFormulario(formulario);

        var estudio = new Estudio
        {
            Titulo = formulario.Titulo,
            Fecha = formulario.Fecha!.Value,
            Profesional = formulario.Profesional,
            Institucion = formulario.Institucion,
            Descripcion = formulario.Descripcion,
        };

        foreach (var texto in formulario.EtiquetasSeparadas())
            estudio.Etiquetas.Add(new EtiquetaDeEstudio { Texto = texto });

        // Carga parcial: los archivos válidos se aceptan y cada rechazo se informa junto a su archivo.
        // El rechazo de un archivo no impide crear el estudio (RF-36, AC-90).
        var resultado = await carga.RecibirAsync(formulario.Archivos, yaAsociados: 0);
        foreach (var aceptado in resultado.Aceptados)
            estudio.Archivos.Add(aceptado);

        contexto.Estudios.Add(estudio);
        await contexto.SaveChangesAsync();

        if (resultado.Rechazados.Count > 0)
        {
            formulario.Rechazados = [.. resultado.Rechazados];
            ViewData["EstudioCreado"] = estudio.Id;
            return View(RenovarMarca(formulario));
        }

        TempData["Mensaje"] = "El estudio se guardó correctamente.";
        return RedirectToAction(nameof(Detalle), new { id = estudio.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AgregarArchivos(Guid id, List<IFormFile> archivos)
    {
        var estudio = await contexto.Estudios
            .Include(e => e.Archivos)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (estudio is null) return NoEncontrado();

        var resultado = await carga.RecibirAsync(archivos, estudio.Archivos.Count);
        foreach (var aceptado in resultado.Aceptados)
            estudio.Archivos.Add(aceptado);

        await contexto.SaveChangesAsync();

        if (resultado.Rechazados.Count > 0)
            TempData["Rechazos"] = string.Join(" ", resultado.Rechazados.Select(r => $"{r.NombreOriginal}: {r.Motivo}"));

        return RedirectToAction(nameof(Detalle), new { id });
    }

    private IActionResult DevolverFormulario(EstudioFormulario formulario)
    {
        // Los metadatos vuelven intactos; los archivos hay que readjuntarlos, porque el navegador no
        // permite repoblar un campo de archivo (RNF-67, AC-100).
        // El aviso aparece siempre: el navegador no repuebla un campo de archivo, así que quien
        // había adjuntado algo debe volver a hacerlo, y quien no, debe saberlo antes de guardar.
        formulario.DebeReadjuntarArchivos = true;
        foreach (var archivo in formulario.Archivos)
        {
            formulario.Rechazados.Add(new ArchivoRechazado(
                ServicioDeCargaDeArchivos.SanitizarNombre(archivo.FileName),
                "Volvé a adjuntar este archivo."));
        }

        return View(RenovarMarca(formulario));
    }

    private static EstudioFormulario RenovarMarca(EstudioFormulario formulario)
    {
        formulario.MarcaDeEnvio = Guid.NewGuid().ToString("N");
        formulario.Archivos = [];
        return formulario;
    }

    private bool ConsumirMarcaDeEnvio(string? marca)
    {
        if (string.IsNullOrWhiteSpace(marca)) return true;

        var usadas = HttpContext.Session.GetString(ClaveDeMarcasUsadas)?.Split(';') ?? [];
        if (usadas.Contains(marca, StringComparer.Ordinal)) return false;

        var actualizadas = usadas.TakeLast(20).Append(marca);
        HttpContext.Session.SetString(ClaveDeMarcasUsadas, string.Join(';', actualizadas));
        return true;
    }

    /// <summary>
    /// Respuesta única para recurso ajeno, inexistente o identificador mal formado (RNF-53). No lleva
    /// cuerpo ni mensaje: revelar la diferencia permitiría enumerar los estudios de otras cuentas.
    /// </summary>
    private IActionResult NoEncontrado() => NotFound();
}
