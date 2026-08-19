using MiArchivoMedico.Tests.Apoyo;

namespace MiArchivoMedico.Tests;

public class MetadatosDeEstudioTests
{
    private static readonly DateOnly Hoy = DateOnly.FromDateTime(AplicacionDePrueba.MomentoInicial.UtcDateTime);

    [Fact(DisplayName = "AC-13: el profesional queda almacenado y se muestra en el detalle")]
    public async Task Profesional_SeAlmacenaYSeMuestra() =>
        await ComprobarMetadatoAsync(profesional: "Dra. Rivas", esperado: "Dra. Rivas");

    [Fact(DisplayName = "AC-88: la institución queda almacenada y se muestra en el detalle")]
    public async Task Institucion_SeAlmacenaYSeMuestra() =>
        await ComprobarMetadatoAsync(institucion: "Hospital Central", esperado: "Hospital Central");

    [Fact(DisplayName = "AC-82: la descripción queda almacenada y se muestra en el detalle")]
    public async Task Descripcion_SeAlmacenaYSeMuestra() =>
        await ComprobarMetadatoAsync(descripcion: "control anual de rutina", esperado: "control anual de rutina");

    [Fact(DisplayName = "AC-89: dos etiquetas quedan almacenadas y se muestran en el detalle")]
    public async Task DosEtiquetas_SeAlmacenanYSeMuestran()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();

        await cliente.CrearEstudioAsync("Con etiquetas", Hoy, etiquetas: "cardiología, control");

        var estudios = await aplicacion.EstudiosDeAsync(AplicacionDePrueba.UsuarioUno);
        var etiquetas = estudios[0].Etiquetas.Select(e => e.Texto).OrderBy(t => t).ToList();
        Assert.Equal(["cardiología", "control"], etiquetas);

        var detalle = await (await cliente.GetAsync($"/Estudios/Detalle/{estudios[0].Id}")).Content.ReadAsStringAsync();
        Assert.Contains("cardiolog", detalle, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("control", detalle, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "RNF-55: las columnas normalizadas se calculan al guardar, sin acentos ni mayúsculas")]
    public async Task ColumnasNormalizadas_SeCalculanAlGuardar()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();

        await cliente.CrearEstudioAsync("Ecografía Abdominal", Hoy, institucion: "  Hospital  Central  ");

        var estudio = (await aplicacion.EstudiosDeAsync(AplicacionDePrueba.UsuarioUno))[0];
        Assert.Equal("ecografia abdominal", estudio.TituloNormalizado);
        Assert.Equal("hospital central", estudio.InstitucionNormalizada);
    }

    private static async Task ComprobarMetadatoAsync(
        string? profesional = null, string? institucion = null, string? descripcion = null, string esperado = "")
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();

        await cliente.CrearEstudioAsync("Estudio con metadatos", Hoy, profesional, institucion, descripcion);

        var estudios = await aplicacion.EstudiosDeAsync(AplicacionDePrueba.UsuarioUno);
        Assert.Single(estudios);

        var detalle = await (await cliente.GetAsync($"/Estudios/Detalle/{estudios[0].Id}")).Content.ReadAsStringAsync();
        Assert.Contains(esperado, detalle, StringComparison.Ordinal);
    }
}
