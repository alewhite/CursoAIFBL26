namespace MiArchivoMedico.Web.Dominio;

/// <summary>
/// Marca una entidad como perteneciente a una cuenta. Implementarla es el único requisito para que
/// <see cref="Data.ArchivoMedicoDbContext"/> le aplique el filtro global por propietario (RNF-53).
/// No hace falta —ni se debe— filtrar por OwnerId a mano en cada consulta.
/// </summary>
public interface IPropiedadDeUsuario
{
    string OwnerId { get; set; }
}
