using System.Security.Cryptography;
using MiArchivoMedico.Web.Data;

namespace MiArchivoMedico.Web.Servicios;

/// <summary>
/// Cifra con AES-256-CBC en flujo y escribe <c>[IV 16 bytes][contenido cifrado]</c>. El nombre físico
/// es el identificador del archivo, sin ninguna porción del nombre original (RNF-22).
///
/// Se cifra en flujo a propósito: un archivo de hasta 50 MB no se materializa entero en memoria. La
/// detección de alteración la da la huella SHA-256 que exige RNF-19, calculada sobre el contenido en
/// claro antes de cifrar, así que no hace falta un modo autenticado (research.md §5).
/// </summary>
public sealed class AlmacenamientoCifradoEnDisco : IAlmacenamientoDeArchivos
{
    private const int LargoDelVector = 16;
    private readonly byte[] _clave;
    private readonly string _carpeta;

    public AlmacenamientoCifradoEnDisco(OpcionesDeAlmacenamiento opciones)
    {
        _clave = Convert.FromBase64String(opciones.ClaveBase64);
        _carpeta = opciones.Ruta;
        Directory.CreateDirectory(_carpeta);
    }

    public async Task<long> GuardarAsync(Guid identificador, Stream contenido, CancellationToken cancelacion = default)
    {
        var ruta = RutaDe(identificador);

        using var aes = Aes.Create();
        aes.Key = _clave;
        aes.GenerateIV();

        await using (var destino = File.Create(ruta))
        {
            await destino.WriteAsync(aes.IV, cancelacion);
            await using var cifrador = new CryptoStream(destino, aes.CreateEncryptor(), CryptoStreamMode.Write);
            await contenido.CopyToAsync(cifrador, cancelacion);
            await cifrador.FlushFinalBlockAsync(cancelacion);
        }

        return new FileInfo(ruta).Length;
    }

    public async Task<Stream> AbrirAsync(Guid identificador, CancellationToken cancelacion = default)
    {
        var origen = File.OpenRead(RutaDe(identificador));

        var vector = new byte[LargoDelVector];
        await origen.ReadExactlyAsync(vector, cancelacion);

        using var aes = Aes.Create();
        aes.Key = _clave;
        aes.IV = vector;

        return new CryptoStream(origen, aes.CreateDecryptor(), CryptoStreamMode.Read);
    }

    public Task EliminarAsync(Guid identificador, CancellationToken cancelacion = default)
    {
        var ruta = RutaDe(identificador);
        if (File.Exists(ruta)) File.Delete(ruta);
        return Task.CompletedTask;
    }

    public bool Existe(Guid identificador) => File.Exists(RutaDe(identificador));

    public long EspacioOcupadoEnBytes() =>
        Directory.Exists(_carpeta)
            ? Directory.EnumerateFiles(_carpeta, "*", SearchOption.TopDirectoryOnly)
                .Sum(a => new FileInfo(a).Length)
            : 0;

    private string RutaDe(Guid identificador) => Path.Combine(_carpeta, $"{identificador:N}.bin");
}
