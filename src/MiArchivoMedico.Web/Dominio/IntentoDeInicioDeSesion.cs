namespace MiArchivoMedico.Web.Dominio;

/// <summary>
/// Acumulado que sostiene el bloqueo temporal por intentos fallidos (RNF-60, RNF-65).
///
/// Deliberadamente NO implementa <see cref="IPropiedadDeUsuario"/>: no es un dato médico y debe poder
/// consultarse **sin sesión**, que es justo cuando se evalúa. Someterla al filtro global la volvería
/// invisible en el único momento en que hace falta.
///
/// El contador va contra el nombre ingresado, exista o no una cuenta con ese nombre, para que un
/// nombre inexistente no se distinga de uno real por su comportamiento.
/// </summary>
public class IntentoDeInicioDeSesion
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string NombreDeUsuarioNormalizado { get; set; } = string.Empty;
    public int Fallos { get; set; }
    public DateTimeOffset UltimoFalloEn { get; set; }
}
