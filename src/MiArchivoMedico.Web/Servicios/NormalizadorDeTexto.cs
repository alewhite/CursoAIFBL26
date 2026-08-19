using System.Globalization;
using System.Text;

namespace MiArchivoMedico.Web.Servicios;

/// <summary>
/// Única función de normalización del proyecto (RNF-55). Se usa en las dos puntas: al persistir una
/// columna normalizada y al normalizar el término ingresado antes de buscar. Si las dos puntas no usan
/// esta misma función, la búsqueda deja de coincidir.
/// </summary>
public static class NormalizadorDeTexto
{
    public static string Normalizar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return string.Empty;

        var sinAcentos = new StringBuilder(texto.Length);
        foreach (var caracter in texto.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caracter) != UnicodeCategory.NonSpacingMark)
                sinAcentos.Append(caracter);
        }

        var colapsado = sinAcentos.ToString().Normalize(NormalizationForm.FormC);
        return string.Join(' ', colapsado.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
