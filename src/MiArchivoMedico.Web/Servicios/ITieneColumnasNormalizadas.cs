namespace MiArchivoMedico.Web.Servicios;

/// <summary>
/// La implementa toda entidad con columnas normalizadas. El recálculo lo dispara la interceptación de
/// SaveChanges, nunca un controlador, de modo que ningún camino de escritura pueda saltearlo.
/// </summary>
public interface ITieneColumnasNormalizadas
{
    void RecalcularNormalizados();
}
