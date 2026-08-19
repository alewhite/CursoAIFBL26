using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using MiArchivoMedico.Web.Data;
using MiArchivoMedico.Web.Servicios;

namespace MiArchivoMedico.Web.Controllers;

/// <summary>
/// Entrega de archivos médicos. El filtro global ya restringe a la cuenta de la sesión, así que un
/// identificador ajeno simplemente no aparece y responde 404 igual que uno inexistente (RNF-08).
/// El token y la sesión son condiciones acumulativas, nunca alternativas.
/// </summary>
public class ArchivosController(
    ArchivoMedicoDbContext contexto,
    IAlmacenamientoDeArchivos almacenamiento,
    GeneradorDeTokenDeArchivo tokens) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Ver(Guid id)
    {
        var archivo = await contexto.Archivos.FirstOrDefaultAsync(a => a.Id == id);
        if (archivo is null) return NotFound();

        ViewData["Titulo"] = "Ver archivo";
        ViewData["Token"] = tokens.Emitir(archivo.Id);
        return View(archivo);
    }

    [HttpGet]
    public async Task<IActionResult> Contenido(Guid id, [FromQuery] string? t)
    {
        var archivo = await contexto.Archivos.FirstOrDefaultAsync(a => a.Id == id);
        if (archivo is null || !tokens.EsValido(t, id)) return NotFound();

        AplicarCabecerasDeAislamiento();
        var flujo = await almacenamiento.AbrirAsync(archivo.Id);
        return File(flujo, archivo.TipoDeContenido);
    }

    [HttpGet]
    public async Task<IActionResult> Descargar(Guid id, [FromQuery] string? t)
    {
        var archivo = await contexto.Archivos.FirstOrDefaultAsync(a => a.Id == id);
        if (archivo is null || !tokens.EsValido(t, id)) return NotFound();

        AplicarCabecerasDeAislamiento();
        var flujo = await almacenamiento.AbrirAsync(archivo.Id);
        // El nombre ya vino sanitizado desde la carga (RNF-23).
        return File(flujo, archivo.TipoDeContenido, archivo.NombreOriginal);
    }

    /// <summary>
    /// Impide que el navegador adivine el tipo y que ejecute cualquier contenido activo que el archivo
    /// pudiera traer, como el JavaScript embebido de un PDF (RNF-20, AC-26).
    /// </summary>
    private void AplicarCabecerasDeAislamiento()
    {
        Response.Headers[HeaderNames.XContentTypeOptions] = "nosniff";
        Response.Headers[HeaderNames.ContentSecurityPolicy] =
            "sandbox; default-src 'none'; script-src 'none'; object-src 'none'; base-uri 'none'";
        Response.Headers[HeaderNames.CacheControl] = "no-store";
    }
}
