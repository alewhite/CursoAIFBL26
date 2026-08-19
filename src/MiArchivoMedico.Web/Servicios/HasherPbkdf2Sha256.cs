using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace MiArchivoMedico.Web.Servicios;

/// <summary>
/// Reemplaza al hasher de Identity, que deriva con PBKDF2-HMAC-SHA512: ninguna de las tres
/// combinaciones que admite RNF-03. Este usa PBKDF2-HMAC-SHA256 con el formato V3 de Identity.
/// Falla al construirse si las iteraciones quedan por debajo del mínimo, para que la garantía no
/// dependa de que alguien recuerde configurarlo.
/// </summary>
public sealed class HasherPbkdf2Sha256 : IPasswordHasher<Dominio.Usuario>
{
    public const int IteracionesMinimas = 100_000;
    private const int LargoDeSal = 16;
    private const int LargoDeClave = 32;

    private readonly int _iteraciones;

    public HasherPbkdf2Sha256(IOptions<PasswordHasherOptions> opciones)
    {
        _iteraciones = opciones.Value.IterationCount;
        if (_iteraciones < IteracionesMinimas)
            throw new InvalidOperationException(
                $"PasswordHasherOptions.IterationCount es {_iteraciones} y RNF-03 exige al menos {IteracionesMinimas}.");
    }

    public string HashPassword(Dominio.Usuario usuario, string password)
    {
        var sal = RandomNumberGenerator.GetBytes(LargoDeSal);
        var clave = Rfc2898DeriveBytes.Pbkdf2(password, sal, _iteraciones, HashAlgorithmName.SHA256, LargoDeClave);

        // Formato V3 de Identity: 0x01 | prf | iteraciones | largo de sal | sal | subclave (big-endian)
        var salida = new byte[13 + sal.Length + clave.Length];
        salida[0] = 0x01;
        EscribirBigEndian(salida.AsSpan(1), (uint)KeyDerivationPrfSha256);
        EscribirBigEndian(salida.AsSpan(5), (uint)_iteraciones);
        EscribirBigEndian(salida.AsSpan(9), (uint)sal.Length);
        sal.CopyTo(salida, 13);
        clave.CopyTo(salida, 13 + sal.Length);
        return Convert.ToBase64String(salida);
    }

    public PasswordVerificationResult VerifyHashedPassword(
        Dominio.Usuario usuario, string hashedPassword, string providedPassword)
    {
        byte[] almacenado;
        try { almacenado = Convert.FromBase64String(hashedPassword); }
        catch (FormatException) { return PasswordVerificationResult.Failed; }

        if (almacenado.Length < 13 || almacenado[0] != 0x01) return PasswordVerificationResult.Failed;

        var prf = LeerBigEndian(almacenado.AsSpan(1));
        var iteraciones = (int)LeerBigEndian(almacenado.AsSpan(5));
        var largoDeSal = (int)LeerBigEndian(almacenado.AsSpan(9));
        if (prf != KeyDerivationPrfSha256 || largoDeSal < 8 || almacenado.Length <= 13 + largoDeSal)
            return PasswordVerificationResult.Failed;

        var sal = almacenado.AsSpan(13, largoDeSal).ToArray();
        var esperado = almacenado.AsSpan(13 + largoDeSal).ToArray();
        var calculado = Rfc2898DeriveBytes.Pbkdf2(
            providedPassword, sal, iteraciones, HashAlgorithmName.SHA256, esperado.Length);

        if (!CryptographicOperations.FixedTimeEquals(calculado, esperado))
            return PasswordVerificationResult.Failed;

        // Credencial válida pero derivada con menos iteraciones que el mínimo vigente: se recalcula
        // de forma transparente en este mismo ingreso.
        return iteraciones < _iteraciones
            ? PasswordVerificationResult.SuccessRehashNeeded
            : PasswordVerificationResult.Success;
    }

    private const uint KeyDerivationPrfSha256 = 1;

    private static void EscribirBigEndian(Span<byte> destino, uint valor)
    {
        destino[0] = (byte)(valor >> 24);
        destino[1] = (byte)(valor >> 16);
        destino[2] = (byte)(valor >> 8);
        destino[3] = (byte)valor;
    }

    private static uint LeerBigEndian(ReadOnlySpan<byte> origen) =>
        ((uint)origen[0] << 24) | ((uint)origen[1] << 16) | ((uint)origen[2] << 8) | origen[3];
}
