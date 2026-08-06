using SixLabors.ImageSharp;

namespace MiArchivoMedico.Web.Archivos;

public sealed record ResultadoDeValidacion(bool EsValido, string? Error, string TipoMime)
{
    public static ResultadoDeValidacion Ok(string tipoMime) => new(true, null, tipoMime);

    public static ResultadoDeValidacion Rechazado(string error) => new(false, error, string.Empty);
}

/// <summary>
/// Valida un archivo antes de moverlo al almacenamiento definitivo: tamaño, extensión, tipo MIME, firma
/// binaria y estructura del formato declarado (RNF-14 a RNF-17).
/// </summary>
public sealed class ValidadorDeArchivos
{
    /// <summary>Tamaño máximo por archivo (RNF-14, AC-21).</summary>
    public const long TamanoMaximoEnBytes = 50L * 1024 * 1024;

    private static readonly byte[] FirmaPdf = "%PDF-"u8.ToArray();
    private static readonly byte[] FirmaJpeg = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] FirmaPng = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>Formatos permitidos: extensión, tipos MIME aceptables y MIME canónico (RF-08).</summary>
    private static readonly Dictionary<string, (string[] MimesAceptados, string MimeCanonico)> Permitidos =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = (["application/pdf"], "application/pdf"),
            [".jpg"] = (["image/jpeg", "image/jpg"], "image/jpeg"),
            [".jpeg"] = (["image/jpeg", "image/jpg"], "image/jpeg"),
            [".png"] = (["image/png"], "image/png"),
        };

    /// <summary>
    /// Valida el contenido ya volcado a <paramref name="rutaTemporal"/>. La validación se hace sobre el
    /// archivo en tránsito: al almacenamiento definitivo solo llega lo aceptado (RNF-21, AC-27).
    /// </summary>
    public async Task<ResultadoDeValidacion> ValidarAsync(
        string rutaTemporal,
        string nombreOriginalDeclarado,
        string tipoMimeDeclarado,
        CancellationToken cancelacion = default)
    {
        var informacion = new FileInfo(rutaTemporal);

        if (informacion.Length == 0)
        {
            return ResultadoDeValidacion.Rechazado("El archivo está vacío.");
        }

        if (informacion.Length > TamanoMaximoEnBytes)
        {
            return ResultadoDeValidacion.Rechazado(
                $"El archivo supera el máximo de {TamanoMaximoEnBytes / (1024 * 1024)} MB.");
        }

        var extension = Path.GetExtension(nombreOriginalDeclarado);
        if (string.IsNullOrEmpty(extension) || !Permitidos.TryGetValue(extension, out var permitido))
        {
            return ResultadoDeValidacion.Rechazado(
                "Formato no permitido. Se aceptan PDF, JPG, JPEG y PNG.");
        }

        // El MIME declarado por el navegador no alcanza por sí solo, pero un desacuerdo ya es motivo de
        // rechazo: la firma binaria decide después (RNF-15).
        if (!permitido.MimesAceptados.Contains(tipoMimeDeclarado, StringComparer.OrdinalIgnoreCase))
        {
            return ResultadoDeValidacion.Rechazado(
                "El tipo declarado del archivo no coincide con su extensión.");
        }

        await using var flujo = File.OpenRead(rutaTemporal);

        var firmaEsperada = permitido.MimeCanonico switch
        {
            "application/pdf" => FirmaPdf,
            "image/jpeg" => FirmaJpeg,
            _ => FirmaPng,
        };

        if (!await CoincideLaFirmaAsync(flujo, firmaEsperada, cancelacion))
        {
            // Cubre el ejecutable renombrado a .pdf y el .jpg que en realidad es otro formato
            // (RNF-16, AC-22, AC-23).
            return ResultadoDeValidacion.Rechazado(
                "El contenido del archivo no corresponde a su extensión.");
        }

        var estructuraValida = permitido.MimeCanonico == "application/pdf"
            ? await TieneMarcaDeFinDePdfAsync(flujo, cancelacion)
            : await SeDecodificaComoImagenAsync(flujo, cancelacion);

        return estructuraValida
            ? ResultadoDeValidacion.Ok(permitido.MimeCanonico)
            : ResultadoDeValidacion.Rechazado("El archivo está incompleto o dañado.");
    }

    private static async Task<bool> CoincideLaFirmaAsync(
        Stream flujo, byte[] firma, CancellationToken cancelacion)
    {
        flujo.Position = 0;

        var leidos = new byte[firma.Length];
        if (await flujo.ReadAtLeastAsync(leidos, firma.Length, throwOnEndOfStream: false, cancelacion)
            < firma.Length)
        {
            return false;
        }

        return leidos.AsSpan().SequenceEqual(firma);
    }

    /// <summary>
    /// Un PDF sin la marca <c>%%EOF</c> está truncado (RNF-17, AC-44). Se busca en la cola porque el
    /// estándar admite hasta 1024 bytes de relleno después de la marca.
    /// </summary>
    private static async Task<bool> TieneMarcaDeFinDePdfAsync(Stream flujo, CancellationToken cancelacion)
    {
        const int bytesDeCola = 2048;

        var largo = (int)Math.Min(bytesDeCola, flujo.Length);
        flujo.Position = flujo.Length - largo;

        var cola = new byte[largo];
        await flujo.ReadExactlyAsync(cola, cancelacion);

        return cola.AsSpan().IndexOf("%%EOF"u8) >= 0;
    }

    /// <summary>
    /// Decodifica la imagen completa, que es lo que RNF-17 exige: una firma válida seguida de datos
    /// truncados pasa la comprobación de encabezado pero no la decodificación.
    /// </summary>
    private static async Task<bool> SeDecodificaComoImagenAsync(Stream flujo, CancellationToken cancelacion)
    {
        flujo.Position = 0;

        try
        {
            using var imagen = await Image.LoadAsync(flujo, cancelacion);
            return imagen.Width > 0 && imagen.Height > 0;
        }
        catch (Exception excepcion) when (excepcion is ImageFormatException or InvalidImageContentException
                                              or NotSupportedException)
        {
            // La excepción no se propaga ni se registra: su mensaje puede incluir el nombre del archivo,
            // que es un metadato médico (RNF-09).
            return false;
        }
    }
}
