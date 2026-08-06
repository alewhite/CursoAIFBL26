namespace MiArchivoMedico.Web.Data;

/// <summary>
/// Marca una entidad como perteneciente a una única cuenta. Toda entidad que la implemente recibe
/// automáticamente un filtro global por <see cref="OwnerId"/> en <see cref="ArchivoMedicoDbContext"/>,
/// de modo que olvidar el <c>Where</c> no sea posible (RNF-53, RNF-08).
/// </summary>
/// <remarks>
/// Un <c>Find</c>/<c>FindAsync</c> por clave primaria NO aplica filtros globales: nunca usarlos para
/// cargar datos médicos.
/// </remarks>
public interface IPropiedadDeUsuario
{
    string OwnerId { get; set; }
}
