using System.Text.Json;

namespace MiArchivoMedico.Web.Servicios;

/// <summary>
/// Guarda el criterio en la sesión del servidor. Es el lugar donde vive porque RNF-63 prohíbe que el
/// término viaje en la dirección y RF-40 exige conservarlo al paginar y al volver del detalle: sin un
/// lugar del lado del servidor, los dos requisitos son incompatibles (research.md §3).
///
/// Es estado efímero, por usuario y por sesión: no se persiste en la base.
/// </summary>
public sealed class EstadoDeBusqueda(IHttpContextAccessor accesor)
{
    private const string Clave = "criterio-de-busqueda";

    public CriterioDeBusqueda Leer()
    {
        var sesion = accesor.HttpContext?.Session;
        var guardado = sesion?.GetString(Clave);
        if (string.IsNullOrEmpty(guardado)) return new CriterioDeBusqueda();

        try { return JsonSerializer.Deserialize<CriterioDeBusqueda>(guardado) ?? new CriterioDeBusqueda(); }
        catch (JsonException) { return new CriterioDeBusqueda(); }
    }

    public void Guardar(CriterioDeBusqueda criterio) =>
        accesor.HttpContext?.Session.SetString(Clave, JsonSerializer.Serialize(criterio));

    public void Limpiar() => accesor.HttpContext?.Session.Remove(Clave);
}
