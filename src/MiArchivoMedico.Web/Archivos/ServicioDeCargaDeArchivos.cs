using System.Security.Cryptography;
using MiArchivoMedico.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MiArchivoMedico.Web.Archivos;

/// <summary>Un archivo rechazado, con el índice del campo que lo produjo para poder ubicar el error (AC-80).</summary>
public sealed record ArchivoRechazado(int Indice, string Mensaje);

public sealed record ResultadoDeCarga(
    IReadOnlyList<ArchivoDeEstudio> Aceptados,
    IReadOnlyList<ArchivoRechazado> Rechazados);

/// <summary>
/// Recibe los archivos de un estudio: los valida en tránsito, calcula su hash y solo entonces los mueve
/// cifrados al almacenamiento definitivo (RNF-14 a RNF-22, RNF-52, RNF-61).
/// </summary>
public sealed class ServicioDeCargaDeArchivos
{
    /// <summary>Máximo de archivos por estudio (RNF-61, AC-70).</summary>
    public const int MaximoDeArchivosPorEstudio = 20;

    private readonly ArchivoMedicoDbContext _contexto;
    private readonly IAlmacenamientoDeArchivos _almacenamiento;
    private readonly ValidadorDeArchivos _validador;
    private readonly OpcionesDeAlmacenamiento _opciones;
    private readonly TimeProvider _reloj;

    public ServicioDeCargaDeArchivos(
        ArchivoMedicoDbContext contexto,
        IAlmacenamientoDeArchivos almacenamiento,
        ValidadorDeArchivos validador,
        IOptions<OpcionesDeAlmacenamiento> opciones,
        TimeProvider reloj)
    {
        _contexto = contexto;
        _almacenamiento = almacenamiento;
        _validador = validador;
        _opciones = opciones.Value;
        _reloj = reloj;
    }

    public async Task<ResultadoDeCarga> CargarAsync(
        Estudio estudio,
        IReadOnlyList<IFormFile> archivos,
        CancellationToken cancelacion = default)
    {
        var aceptados = new List<ArchivoDeEstudio>();
        var rechazados = new List<ArchivoRechazado>();

        var yaAsociados = estudio.Archivos.Count;
        var espacioDisponible = await CalcularEspacioDisponibleAsync(cancelacion);

        for (var indice = 0; indice < archivos.Count; indice++)
        {
            var archivo = archivos[indice];
            if (archivo.Length == 0 && string.IsNullOrEmpty(archivo.FileName))
            {
                continue;   // campo de archivo vacío: no es un intento de carga
            }

            if (yaAsociados + aceptados.Count >= MaximoDeArchivosPorEstudio)
            {
                rechazados.Add(new ArchivoRechazado(
                    indice,
                    $"Se alcanzó el límite de {MaximoDeArchivosPorEstudio} archivos por estudio."));
                continue;
            }

            if (archivo.Length > espacioDisponible)
            {
                // El aviso no menciona qué cuenta consumió el espacio ni ningún metadato ajeno (AC-64).
                rechazados.Add(new ArchivoRechazado(
                    indice,
                    "Se alcanzó el límite de almacenamiento de la aplicación. No se pueden cargar " +
                    "archivos nuevos hasta liberar espacio."));
                continue;
            }

            var resultado = await ProcesarAsync(estudio, archivo, indice, cancelacion);

            if (resultado.Aceptado is not null)
            {
                aceptados.Add(resultado.Aceptado);
                espacioDisponible -= resultado.Aceptado.TamanoEnBytes;
            }
            else
            {
                rechazados.Add(resultado.Rechazado!);
            }
        }

        return new ResultadoDeCarga(aceptados, rechazados);
    }

    private async Task<(ArchivoDeEstudio? Aceptado, ArchivoRechazado? Rechazado)> ProcesarAsync(
        Estudio estudio, IFormFile archivo, int indice, CancellationToken cancelacion)
    {
        // Área de tránsito: el archivo se valida acá y solo pasa al almacenamiento definitivo si aprueba.
        // Si algo falla, el finally lo borra y no queda rastro (RNF-21, AC-27).
        var rutaTemporal = Path.Combine(Path.GetTempPath(), $"carga-{Guid.NewGuid():N}.tmp");

        try
        {
            await using (var temporal = File.Create(rutaTemporal))
            {
                await archivo.CopyToAsync(temporal, cancelacion);
            }

            var validacion = await _validador.ValidarAsync(
                rutaTemporal, archivo.FileName, archivo.ContentType, cancelacion);

            if (!validacion.EsValido)
            {
                return (null, new ArchivoRechazado(indice, validacion.Error!));
            }

            var entidad = new ArchivoDeEstudio
            {
                EstudioId = estudio.Id,
                NombreOriginal = SanitizadorDeNombres.Sanitizar(archivo.FileName),
                TipoMime = validacion.TipoMime,
                TamanoEnBytes = new FileInfo(rutaTemporal).Length,
                HashSha256 = await CalcularHashAsync(rutaTemporal, cancelacion),
                CargadoUtc = _reloj.GetUtcNow(),
            };

            await _almacenamiento.GuardarAsync(entidad.Id, rutaTemporal, cancelacion);

            return (entidad, null);
        }
        finally
        {
            if (File.Exists(rutaTemporal))
            {
                File.Delete(rutaTemporal);
            }
        }
    }

    /// <summary>Hash SHA-256 del contenido en claro, en hexadecimal (RNF-19, AC-25).</summary>
    private static async Task<string> CalcularHashAsync(string ruta, CancellationToken cancelacion)
    {
        await using var flujo = File.OpenRead(ruta);
        var hash = await SHA256.HashDataAsync(flujo, cancelacion);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// El cupo de 20 GB es de la instalación y se comparte entre las cuentas, sin cuota individual
    /// (RNF-52), así que la suma ignora deliberadamente el filtro por propietario. Es un agregado de bytes:
    /// no expone ningún metadato de otra cuenta.
    /// </summary>
    private async Task<long> CalcularEspacioDisponibleAsync(CancellationToken cancelacion)
    {
        var usado = await _contexto.Archivos
            .IgnoreQueryFilters()
            .SumAsync(a => a.TamanoEnBytes, cancelacion);

        return Math.Max(0, _opciones.CupoTotalEnBytes - usado);
    }
}
