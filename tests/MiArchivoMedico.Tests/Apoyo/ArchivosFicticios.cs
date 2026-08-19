using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace MiArchivoMedico.Tests.Apoyo;

/// <summary>
/// Genera los archivos de prueba. Nunca se copia un archivo de un caso real (RNF-10): todo lo que
/// entra a una prueba se fabrica acá.
/// </summary>
public static class ArchivosFicticios
{
    public static byte[] PdfValido()
    {
        var contenido = new StringBuilder();
        contenido.Append("%PDF-1.4\n");
        contenido.Append("1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj\n");
        contenido.Append("2 0 obj<</Type/Pages/Kids[3 0 R]/Count 1>>endobj\n");
        contenido.Append("3 0 obj<</Type/Page/Parent 2 0 R/MediaBox[0 0 200 200]>>endobj\n");
        contenido.Append("trailer<</Root 1 0 R>>\n");
        contenido.Append("%%EOF\n");
        return Encoding.ASCII.GetBytes(contenido.ToString());
    }

    /// <summary>PDF con JavaScript embebido, para comprobar que no se ejecuta al visualizarlo (AC-26).</summary>
    public static byte[] PdfConJavaScript()
    {
        var contenido = new StringBuilder();
        contenido.Append("%PDF-1.4\n");
        contenido.Append("1 0 obj<</Type/Catalog/Pages 2 0 R/OpenAction 4 0 R>>endobj\n");
        contenido.Append("2 0 obj<</Type/Pages/Kids[3 0 R]/Count 1>>endobj\n");
        contenido.Append("3 0 obj<</Type/Page/Parent 2 0 R/MediaBox[0 0 200 200]>>endobj\n");
        contenido.Append("4 0 obj<</Type/Action/S/JavaScript/JS(app.alert\\('prueba'\\);)>>endobj\n");
        contenido.Append("trailer<</Root 1 0 R>>\n");
        contenido.Append("%%EOF\n");
        return Encoding.ASCII.GetBytes(contenido.ToString());
    }

    /// <summary>PDF sin la marca de fin, que no supera la validación estructural (AC-44).</summary>
    public static byte[] PdfTruncado()
    {
        var completo = Encoding.ASCII.GetString(PdfValido());
        return Encoding.ASCII.GetBytes(completo.Replace("%%EOF\n", string.Empty));
    }

    public static byte[] Jpg(int ancho = 32, int alto = 32) => Imagen(ancho, alto, new JpegEncoder());

    public static byte[] Png(int ancho = 32, int alto = 32) => Imagen(ancho, alto, new PngEncoder());

    /// <summary>Binario que simula un ejecutable, para renombrarlo como .pdf y ver si lo rechaza (AC-22).</summary>
    public static byte[] Ejecutable()
    {
        var bytes = new byte[256];
        bytes[0] = (byte)'M';
        bytes[1] = (byte)'Z';
        return bytes;
    }

    public static byte[] Vacio() => [];

    private static byte[] Imagen(int ancho, int alto, IImageEncoder codificador)
    {
        using var imagen = new Image<Rgba32>(ancho, alto);
        using var memoria = new MemoryStream();
        imagen.Save(memoria, codificador);
        return memoria.ToArray();
    }
}
