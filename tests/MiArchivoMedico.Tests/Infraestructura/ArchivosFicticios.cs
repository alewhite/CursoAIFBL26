using System.Security.Cryptography;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace MiArchivoMedico.Tests.Infraestructura;

/// <summary>
/// Genera los archivos de prueba. Nunca se copia un archivo de un caso real: todo se construye acá
/// (RNF-10).
/// </summary>
public static class ArchivosFicticios
{
    public static byte[] Pdf(string texto = "estudio ficticio")
    {
        // PDF mínimo pero estructuralmente completo: encabezado, un objeto por cada pieza obligatoria,
        // tabla de referencias cruzadas y marca de fin.
        var cuerpo =
            $"""
             %PDF-1.4
             1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj
             2 0 obj<</Type/Pages/Kids[3 0 R]/Count 1>>endobj
             3 0 obj<</Type/Page/Parent 2 0 R/MediaBox[0 0 200 200]>>endobj
             trailer<</Root 1 0 R/Size 4>>
             % {texto}
             %%EOF
             """;

        return System.Text.Encoding.ASCII.GetBytes(cuerpo);
    }

    /// <summary>PDF con un objeto /JavaScript embebido, para verificar que no se ejecuta (AC-26).</summary>
    public static byte[] PdfConJavaScript()
    {
        var cuerpo =
            """
            %PDF-1.4
            1 0 obj<</Type/Catalog/Pages 2 0 R/OpenAction 4 0 R>>endobj
            2 0 obj<</Type/Pages/Kids[3 0 R]/Count 1>>endobj
            3 0 obj<</Type/Page/Parent 2 0 R/MediaBox[0 0 200 200]>>endobj
            4 0 obj<</Type/Action/S/JavaScript/JS(app.alert\('ejecutado'\);)>>endobj
            trailer<</Root 1 0 R/Size 5>>
            %%EOF
            """;

        return System.Text.Encoding.ASCII.GetBytes(cuerpo);
    }

    /// <summary>PDF sin la marca <c>%%EOF</c>: el caso de AC-44.</summary>
    public static byte[] PdfTruncado()
    {
        var completo = Pdf();
        var marca = "%%EOF"u8;
        var posicion = completo.AsSpan().LastIndexOf(marca);
        return completo[..posicion];
    }

    public static byte[] Jpg(int ancho = 40, int alto = 40) => Imagen(ancho, alto, new JpegEncoder());

    public static byte[] Png(int ancho = 40, int alto = 40) => Imagen(ancho, alto, new PngEncoder());

    /// <summary>Un ejecutable ELF, para renombrarlo a .pdf y verificar el rechazo (AC-22).</summary>
    public static byte[] Ejecutable()
    {
        var bytes = new byte[512];
        bytes[0] = 0x7F;
        bytes[1] = (byte)'E';
        bytes[2] = (byte)'L';
        bytes[3] = (byte)'F';
        RandomNumberGenerator.Fill(bytes.AsSpan(4));
        return bytes;
    }

    /// <summary>Contenido que supera el máximo de 50 MB (AC-21).</summary>
    public static byte[] PdfDemasiadoGrande()
    {
        var relleno = new byte[52L * 1024 * 1024];
        var encabezado = Pdf();
        encabezado.CopyTo(relleno, 0);
        "%%EOF"u8.CopyTo(relleno.AsSpan(relleno.Length - 5));
        return relleno;
    }

    public static string HashDe(byte[] contenido) =>
        Convert.ToHexString(SHA256.HashData(contenido)).ToLowerInvariant();

    private static byte[] Imagen(int ancho, int alto, IImageEncoder codificador)
    {
        using var imagen = new Image<Rgba32>(ancho, alto);

        for (var x = 0; x < ancho; x++)
        {
            for (var y = 0; y < alto; y++)
            {
                imagen[x, y] = new Rgba32((byte)(x * 6 % 256), (byte)(y * 6 % 256), 128);
            }
        }

        using var memoria = new MemoryStream();
        imagen.Save(memoria, codificador);
        return memoria.ToArray();
    }
}
