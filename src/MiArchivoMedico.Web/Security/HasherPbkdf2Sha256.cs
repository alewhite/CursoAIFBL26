using System.Buffers.Binary;
using System.Security.Cryptography;
using MiArchivoMedico.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace MiArchivoMedico.Web.Security;

/// <summary>
/// Hasher de contraseñas PBKDF2-HMAC-SHA256 (RNF-03, AC-76).
/// </summary>
/// <remarks>
/// El <see cref="PasswordHasher{TUser}"/> de Identity usa PBKDF2, pero con HMAC-SHA512 fijo por código:
/// <see cref="PasswordHasherOptions"/> permite cambiar las iteraciones, no la función pseudoaleatoria.
/// RNF-03 enumera tres combinaciones admitidas y HMAC-SHA512 no es ninguna de ellas, así que la derivación
/// se hace acá. Se conserva el formato de Identity V3 —[0x01][prf][iteraciones][largo de sal][sal][subclave],
/// todos los enteros big-endian— para no inventar un formato propio.
/// </remarks>
public sealed class HasherPbkdf2Sha256 : IPasswordHasher<UsuarioApp>
{
    private const byte MarcaDeFormato = 0x01;
    private const uint IdentificadorHmacSha256 = 1;
    private const int LargoDeSalEnBytes = 16;
    private const int LargoDeSubclaveEnBytes = 32;

    /// <summary>Mínimo exigido por RNF-03. Configurar menos es un error de configuración, no un ajuste.</summary>
    private const int IteracionesMinimas = 100_000;

    private readonly int _iteraciones;

    public HasherPbkdf2Sha256(IOptions<PasswordHasherOptions> opciones)
    {
        _iteraciones = opciones.Value.IterationCount;

        if (_iteraciones < IteracionesMinimas)
        {
            throw new InvalidOperationException(
                $"PasswordHasherOptions.IterationCount es {_iteraciones}; RNF-03 exige al menos " +
                $"{IteracionesMinimas}.");
        }
    }

    public string HashPassword(UsuarioApp user, string password)
    {
        ArgumentNullException.ThrowIfNull(password);

        var sal = RandomNumberGenerator.GetBytes(LargoDeSalEnBytes);
        var subclave = Derivar(password, sal, _iteraciones);

        var resultado = new byte[13 + sal.Length + subclave.Length];
        resultado[0] = MarcaDeFormato;
        BinaryPrimitives.WriteUInt32BigEndian(resultado.AsSpan(1, 4), IdentificadorHmacSha256);
        BinaryPrimitives.WriteUInt32BigEndian(resultado.AsSpan(5, 4), (uint)_iteraciones);
        BinaryPrimitives.WriteUInt32BigEndian(resultado.AsSpan(9, 4), (uint)sal.Length);
        sal.CopyTo(resultado.AsSpan(13));
        subclave.CopyTo(resultado.AsSpan(13 + sal.Length));

        return Convert.ToBase64String(resultado);
    }

    public PasswordVerificationResult VerifyHashedPassword(
        UsuarioApp user, string hashedPassword, string providedPassword)
    {
        ArgumentNullException.ThrowIfNull(hashedPassword);
        ArgumentNullException.ThrowIfNull(providedPassword);

        if (!TryDesempaquetar(hashedPassword, out var iteraciones, out var sal, out var subclave))
        {
            return PasswordVerificationResult.Failed;
        }

        var candidata = Derivar(providedPassword, sal, iteraciones);
        if (!CryptographicOperations.FixedTimeEquals(candidata, subclave))
        {
            return PasswordVerificationResult.Failed;
        }

        // Un hash con menos iteraciones que las configuradas se rehashea en el próximo inicio de sesión.
        return iteraciones < _iteraciones
            ? PasswordVerificationResult.SuccessRehashNeeded
            : PasswordVerificationResult.Success;
    }

    private static byte[] Derivar(string contrasena, byte[] sal, int iteraciones) =>
        Rfc2898DeriveBytes.Pbkdf2(
            contrasena, sal, iteraciones, HashAlgorithmName.SHA256, LargoDeSubclaveEnBytes);

    private static bool TryDesempaquetar(
        string hash, out int iteraciones, out byte[] sal, out byte[] subclave)
    {
        iteraciones = 0;
        sal = [];
        subclave = [];

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(hash);
        }
        catch (FormatException)
        {
            return false;
        }

        if (bytes.Length < 13 || bytes[0] != MarcaDeFormato)
        {
            return false;
        }

        if (BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(1, 4)) != IdentificadorHmacSha256)
        {
            return false;
        }

        iteraciones = (int)BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(5, 4));
        var largoDeSal = (int)BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(9, 4));

        if (iteraciones <= 0 || largoDeSal < LargoDeSalEnBytes || bytes.Length <= 13 + largoDeSal)
        {
            return false;
        }

        sal = bytes[13..(13 + largoDeSal)];
        subclave = bytes[(13 + largoDeSal)..];

        return subclave.Length == LargoDeSubclaveEnBytes;
    }
}
