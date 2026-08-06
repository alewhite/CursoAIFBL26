using System.Net;
using MiArchivoMedico.Tests.Infraestructura;
using MiArchivoMedico.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MiArchivoMedico.Tests;

/// <summary>Validación de archivos antes del almacenamiento definitivo (RNF-14 a RNF-23, RNF-61).</summary>
public class ValidacionDeArchivosTests : IAsyncLifetime
{
    private readonly AplicacionDePrueba _app = new();

    public Task InitializeAsync() => _app.InitializeAsync();

    public Task DisposeAsync() => _app.DisposeAsync();

    [Theory(DisplayName = "AC-20, AC-74, AC-75: PDF, JPG y PNG válidos se aceptan")]
    [InlineData("informe.pdf", "application/pdf")]
    [InlineData("placa.jpg", "image/jpeg")]
    [InlineData("placa.png", "image/png")]
    public async Task LosFormatosPermitidos_SeAceptan(string nombre, string mime)
    {
        var cliente = await ClienteAutenticadoAsync();
        var contenido = mime switch
        {
            "application/pdf" => ArchivosFicticios.Pdf(),
            "image/jpeg" => ArchivosFicticios.Jpg(),
            _ => ArchivosFicticios.Png(),
        };

        var id = await cliente.CrearYObtenerIdAsync(
            "Estudio con archivo valido",
            archivos: [new ArchivoAEnviar(nombre, mime, contenido)]);

        Assert.Single(await cliente.ObtenerIdsDeArchivosAsync(id));
    }

    [Fact(DisplayName = "AC-21: un archivo de más de 50 MB se rechaza antes de almacenarse")]
    public async Task UnArchivoDemasiadoGrande_SeRechaza()
    {
        await AssertRechazadoAsync(
            new ArchivoAEnviar("enorme.pdf", "application/pdf", ArchivosFicticios.PdfDemasiadoGrande()),
            "supera el máximo");
    }

    [Fact(DisplayName = "AC-22: un ejecutable renombrado a .pdf se rechaza")]
    public async Task UnEjecutableRenombrado_SeRechaza()
    {
        await AssertRechazadoAsync(
            new ArchivoAEnviar("informe.pdf", "application/pdf", ArchivosFicticios.Ejecutable()),
            "no corresponde a su extensión");
    }

    [Fact(DisplayName = "AC-23: un .jpg cuya firma es de otro formato se rechaza")]
    public async Task UnJpgConFirmaAjena_SeRechaza()
    {
        await AssertRechazadoAsync(
            new ArchivoAEnviar("placa.jpg", "image/jpeg", ArchivosFicticios.Png()),
            "no corresponde a su extensión");
    }

    [Fact(DisplayName = "AC-24: un archivo de 0 bytes se rechaza")]
    public async Task UnArchivoVacio_SeRechaza()
    {
        await AssertRechazadoAsync(
            new ArchivoAEnviar("vacio.pdf", "application/pdf", []),
            "está vacío");
    }

    [Fact(DisplayName = "AC-44: un PDF truncado sin %%EOF se rechaza")]
    public async Task UnPdfTruncado_SeRechaza()
    {
        await AssertRechazadoAsync(
            new ArchivoAEnviar("truncado.pdf", "application/pdf", ArchivosFicticios.PdfTruncado()),
            "incompleto o dañado");
    }

    [Fact(DisplayName = "RNF-17: una imagen con firma válida pero datos truncados se rechaza")]
    public async Task UnaImagenTruncada_SeRechaza()
    {
        var completa = ArchivosFicticios.Png(200, 200);

        // Se conserva la firma PNG y se corta el resto: pasa el control de encabezado, no la decodificación.
        var truncada = completa[..(completa.Length / 2)];

        await AssertRechazadoAsync(
            new ArchivoAEnviar("placa.png", "image/png", truncada),
            "incompleto o dañado");
    }

    [Fact(DisplayName = "AC-25: se registra el hash SHA-256 del archivo cargado")]
    public async Task SeRegistraElHashSha256()
    {
        var cliente = await ClienteAutenticadoAsync();
        var contenido = ArchivosFicticios.Pdf("hash conocido");

        var id = await cliente.CrearYObtenerIdAsync(
            "Estudio con hash",
            archivos: [new ArchivoAEnviar("informe.pdf", "application/pdf", contenido)]);

        await _app.EnAlcanceAsync(async servicios =>
        {
            var contexto = servicios.GetRequiredService<ArchivoMedicoDbContext>();
            var almacenado = await contexto.Archivos
                .IgnoreQueryFilters()
                .Where(a => a.EstudioId == id)
                .Select(a => a.HashSha256)
                .SingleAsync();

            Assert.Equal(ArchivosFicticios.HashDe(contenido), almacenado);
        });
    }

    [Fact(DisplayName = "AC-27: un archivo rechazado no queda en el almacenamiento definitivo")]
    public async Task UnArchivoRechazado_NoQuedaEnElAlmacenamiento()
    {
        var cliente = await ClienteAutenticadoAsync();

        // Un archivo válido junto a uno inválido: el válido tampoco debe quedar, porque el alta no se
        // completó.
        await cliente.CrearEstudioAsync(
            "Estudio con un archivo malo",
            archivos:
            [
                new ArchivoAEnviar("bueno.pdf", "application/pdf", ArchivosFicticios.Pdf()),
                new ArchivoAEnviar("malo.pdf", "application/pdf", ArchivosFicticios.Ejecutable()),
            ]);

        var enDisco = Directory.Exists(_app.RutaDeAlmacenamiento)
            ? Directory.GetFiles(_app.RutaDeAlmacenamiento)
            : [];

        Assert.Empty(enDisco);
    }

    [Fact(DisplayName = "AC-65: el nombre físico es un GUID sin rastro del nombre original")]
    public async Task ElNombreFisico_EsUnGuid()
    {
        var cliente = await ClienteAutenticadoAsync();

        var id = await cliente.CrearYObtenerIdAsync(
            "Estudio con nombre fisico",
            archivos: [new ArchivoAEnviar("informe.pdf", "application/pdf", ArchivosFicticios.Pdf())]);

        var idDeArchivo = (await cliente.ObtenerIdsDeArchivosAsync(id)).Single();
        var enDisco = Directory.GetFiles(_app.RutaDeAlmacenamiento).Select(Path.GetFileName).ToList();

        Assert.Equal([$"{idDeArchivo:N}.bin"], enDisco);
        Assert.DoesNotContain(enDisco, nombre => nombre!.Contains("informe"));
    }

    [Fact(DisplayName = "AC-66: el nombre original se sanitiza y conserva la extensión")]
    public async Task ElNombreOriginal_SeSanitiza()
    {
        var cliente = await ClienteAutenticadoAsync();

        var id = await cliente.CrearYObtenerIdAsync(
            "Estudio con nombre hostil",
            archivos: [
                new ArchivoAEnviar("../../etc/passwd.pdf", "application/pdf", ArchivosFicticios.Pdf())]);

        await _app.EnAlcanceAsync(async servicios =>
        {
            var contexto = servicios.GetRequiredService<ArchivoMedicoDbContext>();
            var nombre = await contexto.Archivos
                .IgnoreQueryFilters()
                .Where(a => a.EstudioId == id)
                .Select(a => a.NombreOriginal)
                .SingleAsync();

            Assert.DoesNotContain("..", nombre);
            Assert.DoesNotContain("/", nombre);
            Assert.DoesNotContain("\\", nombre);
            Assert.EndsWith(".pdf", nombre);
        });

        // Y se muestra escapado, no interpretado como marcado.
        var detalle = await cliente.GetAsync($"/Estudios/Detalle/{id}");
        Assert.DoesNotContain("../../", await detalle.Content.ReadAsStringAsync());
    }

    [Fact(DisplayName = "AC-70: con 20 archivos, el archivo 21 se rechaza informando el límite")]
    public async Task ConVeinteArchivos_ElSiguienteSeRechaza()
    {
        var cliente = await ClienteAutenticadoAsync();

        var veinte = Enumerable.Range(0, 20)
            .Select(i => new ArchivoAEnviar($"informe-{i}.pdf", "application/pdf", ArchivosFicticios.Pdf($"n {i}")))
            .ToArray();

        var id = await cliente.CrearYObtenerIdAsync("Estudio lleno", archivos: veinte);
        Assert.Equal(20, (await cliente.ObtenerIdsDeArchivosAsync(id)).Length);

        var respuesta = await cliente.EditarEstudioAsync(
            id, "Estudio lleno",
            archivos: [new ArchivoAEnviar("extra.pdf", "application/pdf", ArchivosFicticios.Pdf("extra"))]);

        var html = WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync());
        Assert.Contains("límite de 20 archivos por estudio", html);
        Assert.Equal(20, (await cliente.ObtenerIdsDeArchivosAsync(id)).Length);
    }

    private async Task<HttpClient> ClienteAutenticadoAsync()
    {
        var cliente = _app.CrearCliente();
        await cliente.IniciarSesionAsync(AplicacionDePrueba.Usuario, AplicacionDePrueba.Contrasena);
        return cliente;
    }

    private async Task AssertRechazadoAsync(ArchivoAEnviar archivo, string fragmentoDelMensaje)
    {
        var cliente = await ClienteAutenticadoAsync();

        var respuesta = await cliente.CrearEstudioAsync("Estudio con archivo invalido", archivos: [archivo]);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);   // vuelve al formulario
        var html = WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync());
        Assert.Contains(fragmentoDelMensaje, html);

        var enDisco = Directory.Exists(_app.RutaDeAlmacenamiento)
            ? Directory.GetFiles(_app.RutaDeAlmacenamiento)
            : [];
        Assert.Empty(enDisco);
    }
}
