using MiArchivoMedico.Web.Archivos;
using MiArchivoMedico.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;

namespace MiArchivoMedico.Web.Controllers;

/// <summary>
/// Entrega de archivos médicos. No hay URL pública ni permanente: cada solicitud pasa por autenticación y
/// por el filtro de propietario, y un identificador ajeno responde 404 (RNF-06 a RNF-08, AC-02, AC-48, AC-84).
/// </summary>
[Authorize]
public class ArchivosController : Controller
{
    private readonly ArchivoMedicoDbContext _contexto;
    private readonly IAlmacenamientoDeArchivos _almacenamiento;

    public ArchivosController(ArchivoMedicoDbContext contexto, IAlmacenamientoDeArchivos almacenamiento)
    {
        _contexto = contexto;
        _almacenamiento = almacenamiento;
    }

    /// <summary>Pantalla que renderiza el archivo dentro de la aplicación (RF-11, AC-15).</summary>
    [HttpGet]
    public async Task<IActionResult> Ver(Guid id, CancellationToken cancelacion)
    {
        var archivo = await BuscarAsync(id, cancelacion);
        return archivo is null ? NotFound() : View(archivo);
    }

    /// <summary>
    /// Contenido para incrustar. Se sirve con una CSP que apaga todo contenido activo, de modo que un PDF
    /// con JavaScript embebido no lo ejecute (RNF-20, AC-26).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Contenido(Guid id, CancellationToken cancelacion)
    {
        var archivo = await BuscarAsync(id, cancelacion);
        if (archivo is null)
        {
            return NotFound();
        }

        AplicarCabecerasDeAislamiento();

        var flujo = await _almacenamiento.AbrirAsync(archivo.Id, cancelacion);
        return File(flujo, archivo.TipoMime);
    }

    /// <summary>Descarga individual (RF-12, AC-16).</summary>
    [HttpGet]
    public async Task<IActionResult> Descargar(Guid id, CancellationToken cancelacion)
    {
        var archivo = await BuscarAsync(id, cancelacion);
        if (archivo is null)
        {
            return NotFound();
        }

        AplicarCabecerasDeAislamiento();

        var flujo = await _almacenamiento.AbrirAsync(archivo.Id, cancelacion);
        return File(flujo, archivo.TipoMime, archivo.NombreOriginal);
    }

    private Task<ArchivoDeEstudio?> BuscarAsync(Guid id, CancellationToken cancelacion) =>
        _contexto.Archivos
            .AsNoTracking()
            .Include(a => a.Estudio)
            .FirstOrDefaultAsync(a => a.Id == id, cancelacion);

    private void AplicarCabecerasDeAislamiento()
    {
        // 'sandbox' sin tokens deshabilita scripts, formularios y plugins en el documento servido;
        // 'nosniff' impide que el navegador reinterprete el tipo declarado.
        Response.Headers["Content-Security-Policy"] = "default-src 'none'; sandbox; base-uri 'none'";
        Response.Headers[HeaderNames.XContentTypeOptions] = "nosniff";

        // Nada de caché compartida: el contenido es médico y la respuesta es específica de la sesión.
        Response.Headers[HeaderNames.CacheControl] = "no-store, private";
    }
}
