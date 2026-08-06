using System.Globalization;
using System.Text;

namespace MiArchivoMedico.Web.Dominio;

/// <summary>
/// Normaliza texto libre para búsqueda: minúsculas, sin acentos, sin espacios sobrantes (RNF-55).
/// </summary>
/// <remarks>
/// SQLite no ofrece una intercalación insensible a acentos, así que la normalización se resuelve acá y se
/// persiste en columnas propias. La misma función se aplica al término ingresado antes de consultar:
/// si las dos puntas no usan esta función, la búsqueda deja de coincidir (AC-45, AC-46).
/// </remarks>
public static class NormalizadorDeTexto
{
    public static string Normalizar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return string.Empty;
        }

        var minusculas = texto.Trim().ToLowerInvariant();

        // FormD separa cada letra de sus diacríticos, que se descartan por categoría Unicode: así
        // "cardiología" y "cardiologia" convergen al mismo valor.
        var descompuesto = minusculas.Normalize(NormalizationForm.FormD);

        var constructor = new StringBuilder(descompuesto.Length);
        var espaciosPendientes = false;

        foreach (var caracter in descompuesto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caracter) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsWhiteSpace(caracter))
            {
                // Los espacios internos repetidos colapsan en uno solo.
                espaciosPendientes = constructor.Length > 0;
                continue;
            }

            if (espaciosPendientes)
            {
                constructor.Append(' ');
                espaciosPendientes = false;
            }

            constructor.Append(caracter);
        }

        return constructor.ToString().Normalize(NormalizationForm.FormC);
    }
}
