using MiArchivoMedico.Tests.Apoyo;

namespace MiArchivoMedico.Tests;

public class CargaParcialTests
{
    private static readonly DateOnly Hoy = DateOnly.FromDateTime(AplicacionDePrueba.MomentoInicial.UtcDateTime);

    [Fact(DisplayName = "AC-90: con tres archivos y el segundo inválido, se guardan los dos válidos")]
    public async Task ArchivoInvalidoEntreTres_GuardaLosValidos()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();

        var respuesta = await cliente.CrearEstudioAsync("Carga mixta", Hoy, archivos:
        [
            ("uno.pdf", ArchivosFicticios.PdfValido(), "application/pdf"),
            ("dos.pdf", ArchivosFicticios.Ejecutable(), "application/pdf"),
            ("tres.png", ArchivosFicticios.Png(), "image/png"),
        ]);

        var estudios = await aplicacion.EstudiosDeAsync(AplicacionDePrueba.UsuarioUno);
        Assert.Single(estudios);
        Assert.Equal(2, estudios[0].Archivos.Count);
        Assert.DoesNotContain(estudios[0].Archivos, a => a.NombreOriginal == "dos.pdf");
        Assert.Equal(2, aplicacion.ArchivosEnAlmacenamiento().Length);

        var html = await respuesta.Content.ReadAsStringAsync();
        Assert.Contains("dos.pdf", html, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "FR-030b: si ningún archivo es válido, el estudio se crea igual y se informa cada rechazo")]
    public async Task TodosLosArchivosInvalidos_CreaElEstudioSinArchivos()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();

        await cliente.CrearEstudioAsync("Todo rechazado", Hoy, archivos:
        [
            ("uno.pdf", ArchivosFicticios.Ejecutable(), "application/pdf"),
            ("dos.pdf", ArchivosFicticios.Vacio(), "application/pdf"),
        ]);

        var estudios = await aplicacion.EstudiosDeAsync(AplicacionDePrueba.UsuarioUno);
        Assert.Single(estudios);
        Assert.Empty(estudios[0].Archivos);
        Assert.Empty(aplicacion.ArchivosEnAlmacenamiento());
    }
}
