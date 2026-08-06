using System.Text;

namespace MiArchivoMedico.Web.Archivos;

/// <summary>
/// Sanitiza el nombre original antes de guardarlo como metadato (RNF-23, AC-66).
/// </summary>
/// <remarks>
/// El nombre sanitizado es solo un metadato para mostrar: nunca se usa para construir la ruta física,
/// que es un GUID (RNF-22). El escapado para HTML lo hace Razor al renderizar.
/// </remarks>
public static class SanitizadorDeNombres
{
    private const int LargoMaximo = 255;

    private const string NombreDeReemplazo = "archivo";

    public static string Sanitizar(string? nombreOriginal)
    {
        if (string.IsNullOrWhiteSpace(nombreOriginal))
        {
            return NombreDeReemplazo;
        }

        // La extensión se conserva, así que se separa antes de limpiar el resto.
        var extension = LimpiarExtension(Path.GetExtension(nombreOriginal));
        var cuerpo = Limpiar(Path.GetFileNameWithoutExtension(nombreOriginal));

        if (cuerpo.Length == 0)
        {
            cuerpo = NombreDeReemplazo;
        }

        var disponibleParaCuerpo = LargoMaximo - extension.Length;
        if (cuerpo.Length > disponibleParaCuerpo)
        {
            cuerpo = cuerpo[..disponibleParaCuerpo];
        }

        return cuerpo + extension;
    }

    private static string Limpiar(string texto)
    {
        var constructor = new StringBuilder(texto.Length);

        foreach (var caracter in texto)
        {
            // Separadores de ruta y caracteres de control: fuera (RNF-23).
            if (caracter is '/' or '\\' || char.IsControl(caracter))
            {
                continue;
            }

            constructor.Append(caracter);
        }

        var limpio = constructor.ToString();

        // Las secuencias ".." se eliminan de forma repetida: quitarlas de una sola pasada dejaría pasar
        // "...." , que al colapsar vuelve a formar "..".
        while (limpio.Contains(".."))
        {
            limpio = limpio.Replace("..", string.Empty);
        }

        return limpio.Trim();
    }

    private static string LimpiarExtension(string extension)
    {
        var limpia = Limpiar(extension);
        return limpia.StartsWith('.') ? limpia : string.Empty;
    }
}
