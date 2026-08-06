namespace MiArchivoMedico.Web.Archivos;

/// <summary>
/// Almacenamiento de archivos médicos. La abstracción existe para poder reemplazar el proveedor sin tocar
/// las reglas del dominio; el identificador es siempre el GUID del archivo, nunca una ruta.
/// </summary>
public interface IAlmacenamientoDeArchivos
{
    /// <summary>Cifra el contenido de <paramref name="rutaDeOrigen"/> y lo guarda bajo <paramref name="id"/>.</summary>
    Task GuardarAsync(Guid id, string rutaDeOrigen, CancellationToken cancelacion = default);

    /// <summary>Abre el contenido descifrado. El llamador es responsable de cerrar el flujo.</summary>
    Task<Stream> AbrirAsync(Guid id, CancellationToken cancelacion = default);

    Task EliminarAsync(Guid id, CancellationToken cancelacion = default);

    bool Existe(Guid id);
}
