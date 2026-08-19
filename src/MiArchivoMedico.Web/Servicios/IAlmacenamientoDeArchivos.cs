namespace MiArchivoMedico.Web.Servicios;

/// <summary>
/// Única abstracción con interfaz del proyecto. Existe porque el PRD exige poder reemplazar el
/// proveedor de almacenamiento sin tocar las reglas de dominio, no por prolijidad.
/// </summary>
public interface IAlmacenamientoDeArchivos
{
    /// <summary>Guarda el contenido cifrado bajo el identificador dado y devuelve los bytes ocupados.</summary>
    Task<long> GuardarAsync(Guid identificador, Stream contenido, CancellationToken cancelacion = default);

    Task<Stream> AbrirAsync(Guid identificador, CancellationToken cancelacion = default);

    Task EliminarAsync(Guid identificador, CancellationToken cancelacion = default);

    bool Existe(Guid identificador);

    /// <summary>Bytes ocupados por todos los archivos, para el control del cupo compartido.</summary>
    long EspacioOcupadoEnBytes();
}
