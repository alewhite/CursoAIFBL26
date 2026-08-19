using Microsoft.AspNetCore.DataProtection;

namespace MiArchivoMedico.Web.Servicios;

/// <summary>
/// Emite un token firmado con vencimiento propio para la ruta de contenido de un archivo (RNF-07).
///
/// **No es una credencial**: la ruta exige además sesión válida del propietario. Un token vigente sin
/// sesión no entrega nada, y una sesión válida con token vencido tampoco (AC-08, AC-84). El token
/// tampoco se persiste: la protección de datos de la plataforma lo firma y lo verifica sin guardar
/// estado, así que no hay tabla que limpiar (research.md §4).
/// </summary>
public sealed class GeneradorDeTokenDeArchivo(IDataProtectionProvider proveedor, TimeProvider reloj)
{
    public static readonly TimeSpan Vigencia = TimeSpan.FromMinutes(5);

    private readonly IDataProtector _protector = proveedor.CreateProtector("MiArchivoMedico.AccesoAArchivo");

    public string Emitir(Guid archivoId)
    {
        var vence = reloj.GetUtcNow().Add(Vigencia).UtcTicks;
        return _protector.Protect($"{archivoId:N}|{vence}");
    }

    /// <summary>Verifica firma, vencimiento y que el token corresponda a ese archivo y no a otro.</summary>
    public bool EsValido(string? token, Guid archivoId)
    {
        if (string.IsNullOrWhiteSpace(token)) return false;

        string contenido;
        try { contenido = _protector.Unprotect(token); }
        catch (System.Security.Cryptography.CryptographicException) { return false; }

        var partes = contenido.Split('|');
        if (partes.Length != 2) return false;
        if (!Guid.TryParseExact(partes[0], "N", out var identificador) || identificador != archivoId) return false;
        if (!long.TryParse(partes[1], out var vence)) return false;

        return reloj.GetUtcNow().UtcTicks < vence;
    }
}
