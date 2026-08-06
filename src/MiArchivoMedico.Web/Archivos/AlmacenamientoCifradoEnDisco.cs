using System.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace MiArchivoMedico.Web.Archivos;

/// <summary>
/// Almacenamiento en una carpeta del servidor, con cada archivo cifrado con AES-256 (RNF-02, AC-57).
/// </summary>
/// <remarks>
/// Formato en disco: [IV de 16 bytes][contenido cifrado con AES-256-CBC]. El cifrado es en flujo, de modo
/// que un archivo de 50 MB no se materializa en memoria.
/// <para>
/// No hay MAC sobre el texto cifrado: la integridad la aporta el SHA-256 del contenido en claro que se
/// guarda con el metadato (RNF-19), y que es lo que verifica una manipulación del archivo en disco. Un MAC
/// detectaría la manipulación antes de descifrar en lugar de después; para el MVP, con el almacenamiento en
/// el propio servidor, se aceptó la versión simple.
/// </para>
/// </remarks>
public sealed class AlmacenamientoCifradoEnDisco : IAlmacenamientoDeArchivos
{
    private const int LargoDeIvEnBytes = 16;

    private readonly string _raiz;
    private readonly byte[] _clave;

    public AlmacenamientoCifradoEnDisco(IOptions<OpcionesDeAlmacenamiento> opciones)
    {
        var valor = opciones.Value;

        if (string.IsNullOrWhiteSpace(valor.Ruta))
        {
            throw new InvalidOperationException(
                $"Falta '{OpcionesDeAlmacenamiento.Seccion}:Ruta': la carpeta de archivos médicos debe " +
                "estar configurada y ubicarse fuera de toda carpeta pública.");
        }

        _raiz = valor.Ruta;
        _clave = valor.ResolverClave();

        Directory.CreateDirectory(_raiz);
    }

    public async Task GuardarAsync(Guid id, string rutaDeOrigen, CancellationToken cancelacion = default)
    {
        using var aes = CrearAes();
        aes.GenerateIV();

        await using var origen = File.OpenRead(rutaDeOrigen);
        await using var destino = File.Create(RutaDe(id));

        await destino.WriteAsync(aes.IV, cancelacion);

        await using var cifrador = new CryptoStream(
            destino, aes.CreateEncryptor(), CryptoStreamMode.Write, leaveOpen: true);

        await origen.CopyToAsync(cifrador, cancelacion);
        await cifrador.FlushFinalBlockAsync(cancelacion);
    }

    public async Task<Stream> AbrirAsync(Guid id, CancellationToken cancelacion = default)
    {
        var origen = File.OpenRead(RutaDe(id));

        try
        {
            var iv = new byte[LargoDeIvEnBytes];
            await origen.ReadExactlyAsync(iv, cancelacion);

            using var aes = CrearAes();
            aes.IV = iv;

            // El CryptoStream toma la propiedad del archivo: cerrarlo cierra los dos.
            return new CryptoStream(origen, aes.CreateDecryptor(), CryptoStreamMode.Read);
        }
        catch
        {
            await origen.DisposeAsync();
            throw;
        }
    }

    public Task EliminarAsync(Guid id, CancellationToken cancelacion = default)
    {
        File.Delete(RutaDe(id));
        return Task.CompletedTask;
    }

    public bool Existe(Guid id) => File.Exists(RutaDe(id));

    private Aes CrearAes()
    {
        var aes = Aes.Create();
        aes.KeySize = 256;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = _clave;
        return aes;
    }

    /// <summary>El nombre físico es solo el GUID: nada derivado del nombre original (RNF-22, AC-65).</summary>
    private string RutaDe(Guid id) => Path.Combine(_raiz, $"{id:N}.bin");
}
