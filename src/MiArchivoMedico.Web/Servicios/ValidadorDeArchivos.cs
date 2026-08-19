using SixLabors.ImageSharp;

namespace MiArchivoMedico.Web.Servicios;

public sealed record ResultadoDeValidacion(bool EsValido, string? Motivo)
{
    public static readonly ResultadoDeValidacion Valido = new(true, null);
    public static ResultadoDeValidacion Rechazado(string motivo) => new(false, motivo);
}

/// <summary>
/// Comprueba extensión, tipo declarado, firma binaria y estructura del formato antes de que el archivo
/// llegue al almacenamiento definitivo (RNF-15, RNF-16, RNF-17). Trabaja sobre el archivo ya recibido
/// en el área de tránsito, leyendo de a poco: un archivo aceptado nunca se materializa entero en
/// memoria.
/// </summary>
public sealed class ValidadorDeArchivos
{
    public const long TamanoMaximoEnBytes = 50L * 1024 * 1024;

    private static readonly Dictionary<string, string[]> TiposPorExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = ["application/pdf"],
        [".jpg"] = ["image/jpeg"],
        [".jpeg"] = ["image/jpeg"],
        [".png"] = ["image/png"],
    };

    public async Task<ResultadoDeValidacion> ValidarAsync(
        string rutaEnTransito, string nombreOriginal, string tipoDeContenidoDeclarado)
    {
        var informacion = new FileInfo(rutaEnTransito);

        if (informacion.Length == 0)
            return ResultadoDeValidacion.Rechazado("El archivo está vacío.");

        if (informacion.Length > TamanoMaximoEnBytes)
            return ResultadoDeValidacion.Rechazado("El archivo supera el máximo de 50 MB.");

        var extension = Path.GetExtension(nombreOriginal);
        if (!TiposPorExtension.TryGetValue(extension, out var tiposAdmitidos))
            return ResultadoDeValidacion.Rechazado("El formato no está admitido. Se aceptan PDF, JPG, JPEG y PNG.");

        if (!tiposAdmitidos.Contains(tipoDeContenidoDeclarado, StringComparer.OrdinalIgnoreCase))
            return ResultadoDeValidacion.Rechazado("El tipo declarado no corresponde a la extensión del archivo.");

        await using var flujo = File.OpenRead(rutaEnTransito);

        var esPdf = extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase);
        var firmaValida = esPdf ? await EsPdfAsync(flujo) : await EsImagenAsync(flujo, extension);
        if (!firmaValida)
            return ResultadoDeValidacion.Rechazado("El contenido del archivo no corresponde a su extensión.");

        flujo.Position = 0;
        var estructuraValida = esPdf ? await PdfEstaCompletoAsync(flujo) : ImagenSeDecodifica(rutaEnTransito);
        return estructuraValida
            ? ResultadoDeValidacion.Valido
            : ResultadoDeValidacion.Rechazado("El archivo está incompleto o dañado.");
    }

    private static async Task<bool> EsPdfAsync(Stream flujo)
    {
        var encabezado = new byte[5];
        return await flujo.ReadAsync(encabezado) == 5 && encabezado.AsSpan().SequenceEqual("%PDF-"u8);
    }

    private static async Task<bool> EsImagenAsync(Stream flujo, string extension)
    {
        var encabezado = new byte[8];
        var leidos = await flujo.ReadAsync(encabezado);
        if (leidos < 8) return false;

        return extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
            ? encabezado.AsSpan().SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })
            : encabezado[0] == 0xFF && encabezado[1] == 0xD8 && encabezado[2] == 0xFF;
    }

    /// <summary>Un PDF sin la marca de fin está truncado (RNF-17, AC-44).</summary>
    private static async Task<bool> PdfEstaCompletoAsync(Stream flujo)
    {
        var aLeer = (int)Math.Min(1024, flujo.Length);
        flujo.Position = flujo.Length - aLeer;

        var cola = new byte[aLeer];
        await flujo.ReadExactlyAsync(cola);
        return System.Text.Encoding.ASCII.GetString(cola).Contains("%%EOF", StringComparison.Ordinal);
    }

    /// <summary>Una imagen que no se decodifica por completo se considera dañada (RNF-17).</summary>
    private static bool ImagenSeDecodifica(string ruta)
    {
        try
        {
            using var imagen = Image.Load(ruta);
            return imagen.Width > 0 && imagen.Height > 0;
        }
        catch (Exception error) when (error is UnknownImageFormatException or InvalidImageContentException or ImageFormatException)
        {
            return false;
        }
    }
}
