using MiArchivoMedico.Web.Servicios;

namespace MiArchivoMedico.Web.Dominio;

/// <summary>Término libre asociado a un estudio. Es una fila por estudio, no una entidad compartida.</summary>
public class EtiquetaDeEstudio : IPropiedadDeUsuario, ITieneColumnasNormalizadas
{
    public const int LargoMaximoTexto = 50;

    public Guid Id { get; set; } = Guid.NewGuid();
    public string OwnerId { get; set; } = string.Empty;

    public Guid EstudioId { get; set; }
    public Estudio? Estudio { get; set; }

    public string Texto { get; set; } = string.Empty;
    public string TextoNormalizado { get; set; } = string.Empty;

    public void RecalcularNormalizados() => TextoNormalizado = NormalizadorDeTexto.Normalizar(Texto);
}
