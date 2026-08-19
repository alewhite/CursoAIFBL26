using MiArchivoMedico.Tests.Apoyo;

namespace MiArchivoMedico.Tests;

public class ValidacionDeArchivosTests
{
    private static readonly DateOnly Hoy = DateOnly.FromDateTime(AplicacionDePrueba.MomentoInicial.UtcDateTime);

    [Fact(DisplayName = "AC-20: un PDF válido de menos de 50 MB se acepta")]
    public Task PdfValido_SeAcepta() =>
        ComprobarAceptadoAsync("informe.pdf", ArchivosFicticios.PdfValido(), "application/pdf");

    [Fact(DisplayName = "AC-74: un JPG válido se acepta y queda asociado al estudio")]
    public Task JpgValido_SeAcepta() =>
        ComprobarAceptadoAsync("placa.jpg", ArchivosFicticios.Jpg(), "image/jpeg");

    [Fact(DisplayName = "AC-75: un PNG válido se acepta y queda asociado al estudio")]
    public Task PngValido_SeAcepta() =>
        ComprobarAceptadoAsync("placa.png", ArchivosFicticios.Png(), "image/png");

    [Fact(DisplayName = "AC-12: un informe PDF y dos imágenes quedan agrupados en el mismo estudio")]
    public async Task TresArchivos_QuedanEnElMismoEstudio()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();

        await cliente.CrearEstudioAsync("Estudio con tres archivos", Hoy, archivos:
        [
            ("informe.pdf", ArchivosFicticios.PdfValido(), "application/pdf"),
            ("placa1.jpg", ArchivosFicticios.Jpg(), "image/jpeg"),
            ("placa2.png", ArchivosFicticios.Png(), "image/png"),
        ]);

        var estudios = await aplicacion.EstudiosDeAsync(AplicacionDePrueba.UsuarioUno);
        Assert.Single(estudios);
        Assert.Equal(3, estudios[0].Archivos.Count);
    }

    [Fact(DisplayName = "AC-22: un ejecutable renombrado con extensión .pdf se rechaza")]
    public Task EjecutableRenombrado_SeRechaza() =>
        ComprobarRechazadoAsync("informe.pdf", ArchivosFicticios.Ejecutable(), "application/pdf");

    [Fact(DisplayName = "AC-23: un archivo .jpg cuya firma corresponde a otro formato se rechaza")]
    public Task FirmaIncoherente_SeRechaza() =>
        ComprobarRechazadoAsync("placa.jpg", ArchivosFicticios.PdfValido(), "image/jpeg");

    [Fact(DisplayName = "AC-24: un archivo de 0 bytes se rechaza")]
    public Task ArchivoVacio_SeRechaza() =>
        ComprobarRechazadoAsync("vacio.pdf", ArchivosFicticios.Vacio(), "application/pdf");

    [Fact(DisplayName = "AC-44: un PDF truncado sin la marca de fin se rechaza")]
    public Task PdfTruncado_SeRechaza() =>
        ComprobarRechazadoAsync("truncado.pdf", ArchivosFicticios.PdfTruncado(), "application/pdf");

    [Fact(DisplayName = "AC-21: un archivo de más de 50 MB se rechaza antes de almacenarlo")]
    public async Task ArchivoDemasiadoGrande_SeRechaza()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();

        var grande = new byte[51 * 1024 * 1024];
        ArchivosFicticios.PdfValido().CopyTo(grande, 0);

        await cliente.CrearEstudioAsync("Estudio con archivo enorme", Hoy, archivos:
            [("enorme.pdf", grande, "application/pdf")]);

        var estudios = await aplicacion.EstudiosDeAsync(AplicacionDePrueba.UsuarioUno);
        Assert.True(estudios.Count == 0 || estudios[0].Archivos.Count == 0);
        Assert.Empty(aplicacion.ArchivosEnAlmacenamiento());
    }

    [Fact(DisplayName = "AC-27: un archivo rechazado no existe en el almacenamiento definitivo")]
    public async Task ArchivoRechazado_NoQuedaEnElAlmacenamiento()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();

        await cliente.CrearEstudioAsync("Estudio con ejecutable", Hoy, archivos:
            [("malicioso.pdf", ArchivosFicticios.Ejecutable(), "application/pdf")]);

        Assert.Empty(aplicacion.ArchivosEnAlmacenamiento());
        var transito = Path.Combine(aplicacion.RutaDeAlmacenamiento, "transito");
        Assert.True(!Directory.Exists(transito) || Directory.GetFiles(transito).Length == 0,
            "El área de tránsito debe quedar limpia después de un rechazo.");
    }

    private static async Task ComprobarAceptadoAsync(string nombre, byte[] contenido, string tipo)
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();

        await cliente.CrearEstudioAsync("Estudio con un archivo", Hoy, archivos: [(nombre, contenido, tipo)]);

        var estudios = await aplicacion.EstudiosDeAsync(AplicacionDePrueba.UsuarioUno);
        Assert.Single(estudios);
        Assert.Single(estudios[0].Archivos);
        Assert.Single(aplicacion.ArchivosEnAlmacenamiento());
    }

    private static async Task ComprobarRechazadoAsync(string nombre, byte[] contenido, string tipo)
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();

        await cliente.CrearEstudioAsync("Estudio con archivo invalido", Hoy, archivos: [(nombre, contenido, tipo)]);

        var estudios = await aplicacion.EstudiosDeAsync(AplicacionDePrueba.UsuarioUno);
        Assert.True(estudios.Count == 0 || estudios[0].Archivos.Count == 0,
            "El archivo inválido no debía quedar asociado a ningún estudio.");
        Assert.Empty(aplicacion.ArchivosEnAlmacenamiento());
    }
}
