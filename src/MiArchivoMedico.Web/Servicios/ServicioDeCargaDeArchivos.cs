using System.Security.Cryptography;
using System.Text;
using MiArchivoMedico.Web.Data;
using MiArchivoMedico.Web.Dominio;

namespace MiArchivoMedico.Web.Servicios;

public sealed record ArchivoRechazado(string NombreOriginal, string Motivo);

public sealed record ResultadoDeCarga(
    IReadOnlyList<ArchivoDeEstudio> Aceptados,
    IReadOnlyList<ArchivoRechazado> Rechazados);

/// <summary>
/// Carga en dos tiempos: el archivo se recibe en el área de tránsito, se valida, se calcula su huella
/// y recién entonces se mueve cifrado al almacenamiento definitivo. Lo rechazado se borra del tránsito
/// y nunca llega al almacenamiento (RNF-21, AC-27).
/// </summary>
public sealed class ServicioDeCargaDeArchivos(
    OpcionesDeAlmacenamiento opciones,
    ValidadorDeArchivos validador,
    IAlmacenamientoDeArchivos almacenamiento,
    ILogger<ServicioDeCargaDeArchivos> registro)
{
    public const int MaximoDeArchivosPorEstudio = 20;

    /// <summary>Purga los restos que un corte pudiera haber dejado en el tránsito.</summary>
    public void PurgarTransito()
    {
        if (!Directory.Exists(opciones.RutaDeTransito)) return;

        foreach (var resto in Directory.EnumerateFiles(opciones.RutaDeTransito))
        {
            try { File.Delete(resto); }
            catch (IOException) { /* lo tomará la próxima purga */ }
        }
    }

    public async Task<ResultadoDeCarga> RecibirAsync(
        IReadOnlyList<IFormFile> archivos,
        int yaAsociados,
        CancellationToken cancelacion = default)
    {
        var aceptados = new List<ArchivoDeEstudio>();
        var rechazados = new List<ArchivoRechazado>();

        Directory.CreateDirectory(opciones.RutaDeTransito);
        var ocupado = almacenamiento.EspacioOcupadoEnBytes();

        foreach (var archivo in archivos)
        {
            if (archivo.Length == 0 && archivo.FileName.Length == 0) continue;

            var nombre = SanitizarNombre(archivo.FileName);

            if (yaAsociados + aceptados.Count >= MaximoDeArchivosPorEstudio)
            {
                rechazados.Add(new ArchivoRechazado(nombre,
                    $"Se alcanzó el límite de {MaximoDeArchivosPorEstudio} archivos por estudio."));
                continue;
            }

            var identificador = Guid.NewGuid();
            var rutaEnTransito = Path.Combine(opciones.RutaDeTransito, $"{identificador:N}.tmp");

            try
            {
                string huella;
                await using (var destino = File.Create(rutaEnTransito))
                {
                    await archivo.CopyToAsync(destino, cancelacion);
                }

                var validacion = await validador.ValidarAsync(rutaEnTransito, nombre, archivo.ContentType ?? string.Empty);
                if (!validacion.EsValido)
                {
                    rechazados.Add(new ArchivoRechazado(nombre, validacion.Motivo!));
                    continue;
                }

                var tamano = new FileInfo(rutaEnTransito).Length;
                if (ocupado + tamano > opciones.CupoTotalEnBytes)
                {
                    // Se rechaza antes de escribir en el almacenamiento definitivo, de modo que el
                    // total nunca supere el cupo (RNF-64). El aviso no menciona ninguna otra cuenta.
                    rechazados.Add(new ArchivoRechazado(nombre,
                        "No hay espacio de almacenamiento disponible para este archivo."));
                    continue;
                }

                await using (var paraHuella = File.OpenRead(rutaEnTransito))
                {
                    huella = Convert.ToHexString(await SHA256.HashDataAsync(paraHuella, cancelacion)).ToLowerInvariant();
                }

                await using (var paraCifrar = File.OpenRead(rutaEnTransito))
                {
                    await almacenamiento.GuardarAsync(identificador, paraCifrar, cancelacion);
                }

                ocupado += tamano;
                aceptados.Add(new ArchivoDeEstudio
                {
                    Id = identificador,
                    NombreOriginal = nombre,
                    TipoDeContenido = archivo.ContentType ?? string.Empty,
                    TamanoEnBytes = tamano,
                    Sha256 = huella,
                });
            }
            finally
            {
                // El tránsito queda limpio pase lo que pase: aceptado, rechazado o con error.
                if (File.Exists(rutaEnTransito))
                {
                    try { File.Delete(rutaEnTransito); }
                    catch (IOException) { registro.LogWarning("No se pudo borrar un archivo del área de tránsito."); }
                }
            }
        }

        return new ResultadoDeCarga(aceptados, rechazados);
    }

    /// <summary>
    /// Sanitiza el nombre original antes de guardarlo como metadato (RNF-23): sin separadores de ruta,
    /// sin caracteres de control, sin secuencias «..», truncado a 255 y conservando la extensión.
    /// El escapado para mostrarlo lo hace la vista.
    /// </summary>
    public static string SanitizarNombre(string nombreOriginal)
    {
        if (string.IsNullOrWhiteSpace(nombreOriginal)) return "archivo";

        var soloNombre = nombreOriginal.Replace('\\', '/');
        soloNombre = soloNombre[(soloNombre.LastIndexOf('/') + 1)..];

        var limpio = new StringBuilder(soloNombre.Length);
        foreach (var caracter in soloNombre)
        {
            if (!char.IsControl(caracter) && !Path.GetInvalidFileNameChars().Contains(caracter))
                limpio.Append(caracter);
        }

        var resultado = limpio.ToString();
        while (resultado.Contains("..", StringComparison.Ordinal))
            resultado = resultado.Replace("..", ".", StringComparison.Ordinal);

        resultado = resultado.Trim('.', ' ');
        if (resultado.Length == 0) return "archivo";

        if (resultado.Length > ArchivoDeEstudio.LargoMaximoNombreOriginal)
        {
            var extension = Path.GetExtension(resultado);
            var cuerpo = resultado[..^extension.Length];
            var disponible = ArchivoDeEstudio.LargoMaximoNombreOriginal - extension.Length;
            resultado = cuerpo[..Math.Max(1, disponible)] + extension;
        }

        return resultado;
    }
}
