using MiArchivoMedico.Web.Archivos;
using MiArchivoMedico.Web.Data;
using MiArchivoMedico.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MiArchivoMedico.Web.Controllers;

/// <summary>
/// Gestión de estudios. Ninguna consulta filtra por propietario a mano: el filtro global del contexto lo
/// hace por todas, de modo que un identificador ajeno simplemente no existe para esta sesión
/// (RNF-53, AC-47).
/// </summary>
[Authorize]
public class EstudiosController : Controller
{
    private readonly ArchivoMedicoDbContext _contexto;
    private readonly ServicioDeCargaDeArchivos _carga;
    private readonly IAlmacenamientoDeArchivos _almacenamiento;
    private readonly TimeProvider _reloj;

    public EstudiosController(
        ArchivoMedicoDbContext contexto,
        ServicioDeCargaDeArchivos carga,
        IAlmacenamientoDeArchivos almacenamiento,
        TimeProvider reloj)
    {
        _contexto = contexto;
        _carga = carga;
        _almacenamiento = almacenamiento;
        _reloj = reloj;
    }

    /// <summary>Listado del más reciente al más antiguo (RF-15, AC-28). La búsqueda llega en la Feature 3.</summary>
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancelacion)
    {
        var estudios = await _contexto.Estudios
            .AsNoTracking()
            .Include(e => e.Etiquetas)
            .Include(e => e.Archivos)
            .OrderByDescending(e => e.Fecha)
            .ThenByDescending(e => e.CreadoUtc)
            .ToListAsync(cancelacion);

        return View(estudios);
    }

    [HttpGet]
    public async Task<IActionResult> Detalle(Guid id, CancellationToken cancelacion)
    {
        var estudio = await BuscarAsync(id, cancelacion);
        return estudio is null ? NotFound() : View(estudio);
    }

    [HttpGet]
    public IActionResult Crear() => View(new EstudioFormularioViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(EstudioFormularioViewModel modelo, CancellationToken cancelacion)
    {
        var estudio = new Estudio { Id = Guid.NewGuid(), CreadoUtc = _reloj.GetUtcNow() };

        // Los archivos se validan aunque los metadatos ya hayan fallado: el usuario tiene que ver de una
        // sola vez todo lo que debe corregir, con cada error junto a su origen (RNF-32, AC-80).
        var resultado = await _carga.CargarAsync(estudio, modelo.Archivos, cancelacion);

        if (!ModelState.IsValid || resultado.Rechazados.Count > 0)
        {
            // Nada se persiste si algo falló: el usuario corrige y reenvía el formulario completo, en
            // lugar de quedar con un estudio a medio cargar.
            await DescartarAsync(resultado.Aceptados, cancelacion);
            AgregarErroresDeArchivos(resultado.Rechazados);
            return View(modelo);
        }

        Volcar(modelo, estudio);
        estudio.Archivos.AddRange(resultado.Aceptados);

        _contexto.Estudios.Add(estudio);
        await _contexto.SaveChangesAsync(cancelacion);

        return RedirectToAction(nameof(Detalle), new { id = estudio.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Editar(Guid id, CancellationToken cancelacion)
    {
        var estudio = await BuscarAsync(id, cancelacion);
        if (estudio is null)
        {
            return NotFound();
        }

        return View(new EstudioFormularioViewModel
        {
            Id = estudio.Id,
            Titulo = estudio.Titulo,
            Fecha = estudio.Fecha,
            Profesional = estudio.Profesional,
            Institucion = estudio.Institucion,
            Descripcion = estudio.Descripcion,
            Etiquetas = string.Join(", ", estudio.Etiquetas.Select(t => t.Texto)),
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(
        Guid id, EstudioFormularioViewModel modelo, CancellationToken cancelacion)
    {
        if (!ModelState.IsValid)
        {
            modelo.Id = id;
            return View(modelo);
        }

        var estudio = await BuscarAsync(id, cancelacion);
        if (estudio is null)
        {
            return NotFound();
        }

        Volcar(modelo, estudio);

        // Editar metadatos no toca los archivos: su contenido y su hash quedan intactos (RNF-18, AC-14).
        if (modelo.Archivos.Count > 0)
        {
            var resultado = await _carga.CargarAsync(estudio, modelo.Archivos, cancelacion);

            if (resultado.Rechazados.Count > 0)
            {
                await DescartarAsync(resultado.Aceptados, cancelacion);
                AgregarErroresDeArchivos(resultado.Rechazados);
                modelo.Id = id;
                return View(modelo);
            }

            estudio.Archivos.AddRange(resultado.Aceptados);
        }

        await _contexto.SaveChangesAsync(cancelacion);

        return RedirectToAction(nameof(Detalle), new { id });
    }

    /// <summary>Confirmación explícita antes de eliminar (RF-13, RNF-33, AC-17, AC-18).</summary>
    [HttpGet]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancelacion)
    {
        var estudio = await BuscarAsync(id, cancelacion);
        return estudio is null ? NotFound() : View(estudio);
    }

    [HttpPost]
    [ActionName("Eliminar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarConfirmado(Guid id, CancellationToken cancelacion)
    {
        var estudio = await BuscarAsync(id, cancelacion);
        if (estudio is null)
        {
            return NotFound();
        }

        // Primero la base y después el disco: si el borrado físico falla, no queda un estudio apuntando a
        // un archivo inexistente, sino a lo sumo un archivo huérfano que ya nadie referencia.
        var ids = estudio.Archivos.Select(a => a.Id).ToList();

        _contexto.Estudios.Remove(estudio);
        await _contexto.SaveChangesAsync(cancelacion);

        foreach (var idDeArchivo in ids)
        {
            await _almacenamiento.EliminarAsync(idDeArchivo, cancelacion);
        }

        return RedirectToAction(nameof(Index));
    }

    private Task<Estudio?> BuscarAsync(Guid id, CancellationToken cancelacion) =>
        _contexto.Estudios
            .Include(e => e.Etiquetas)
            .Include(e => e.Archivos)
            .FirstOrDefaultAsync(e => e.Id == id, cancelacion);

    private void Volcar(EstudioFormularioViewModel modelo, Estudio estudio)
    {
        estudio.Titulo = modelo.Titulo.Trim();
        estudio.Fecha = modelo.Fecha!.Value;
        estudio.Profesional = Limpiar(modelo.Profesional);
        estudio.Institucion = Limpiar(modelo.Institucion);
        estudio.Descripcion = Limpiar(modelo.Descripcion);

        estudio.Etiquetas.Clear();
        estudio.Etiquetas.AddRange(modelo.EtiquetasSeparadas()
            .Select(texto => new EtiquetaDeEstudio { EstudioId = estudio.Id, Texto = texto }));

        static string? Limpiar(string? valor) =>
            string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
    }

    /// <summary>Los errores se anclan al campo del archivo que los produjo, no a un aviso general (AC-80).</summary>
    private void AgregarErroresDeArchivos(IReadOnlyList<ArchivoRechazado> rechazados)
    {
        foreach (var rechazado in rechazados)
        {
            ModelState.AddModelError($"Archivos[{rechazado.Indice}]", rechazado.Mensaje);
        }
    }

    private async Task DescartarAsync(
        IReadOnlyList<ArchivoDeEstudio> aceptados, CancellationToken cancelacion)
    {
        foreach (var archivo in aceptados)
        {
            await _almacenamiento.EliminarAsync(archivo.Id, cancelacion);
        }
    }
}
