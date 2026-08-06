using System.Net;
using MiArchivoMedico.Tests.Infraestructura;
using MiArchivoMedico.Web.Archivos;
using MiArchivoMedico.Web.Dominio;

namespace MiArchivoMedico.Tests;

/// <summary>Cupo de almacenamiento y custodia de la clave de cifrado (RNF-52, RNF-62).</summary>
public class AlmacenamientoTests : IAsyncLifetime
{
    private readonly AplicacionDePrueba _app = new();

    public Task InitializeAsync() => _app.InitializeAsync();

    public Task DisposeAsync() => _app.DisposeAsync();

    [Fact(DisplayName = "AC-55, AC-64: alcanzado el cupo, la carga se rechaza sin revelar nada ajeno")]
    public async Task AlcanzadoElCupo_LaCargaSeRechaza()
    {
        // Cupo diminuto: el primer archivo ya no entra.
        _app.CupoTotalEnBytes = 10;

        var cliente = _app.CrearCliente();
        await cliente.IniciarSesionAsync(AplicacionDePrueba.Usuario, AplicacionDePrueba.Contrasena);

        var respuesta = await cliente.CrearEstudioAsync(
            "Estudio sin espacio",
            archivos: [new ArchivoAEnviar("informe.pdf", "application/pdf", ArchivosFicticios.Pdf())]);

        var html = WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync());

        var inicio = html.IndexOf("límite de almacenamiento", StringComparison.Ordinal);
        Assert.True(inicio >= 0, "No se informó el límite de almacenamiento.");

        // El aviso no nombra ninguna cuenta ni ningún metadato de otro propietario (AC-64). Se inspecciona
        // el mensaje, no la página entera: el resto del HTML es el formulario, no el aviso.
        var aviso = html[(inicio - 100)..html.IndexOf("</span>", inicio, StringComparison.Ordinal)];

        Assert.DoesNotContain(AplicacionDePrueba.OtroUsuario, aviso);
        Assert.DoesNotContain("cuenta", aviso, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "AC-83: sin clave de cifrado, la aplicación no arranca")]
    public async Task SinClaveDeCifrado_LaAplicacionNoArranca()
    {
        _app.ClaveDeCifradoBase64 = string.Empty;

        var excepcion = await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            var cliente = _app.CrearCliente();
            await cliente.GetAsync("/");
        });

        Assert.Contains("ClaveBase64", DescribirCadena(excepcion));
    }

    [Fact(DisplayName = "RNF-02: una clave que no decodifica a 32 bytes se rechaza")]
    public void UnaClaveDeLargoIncorrecto_SeRechaza()
    {
        var opciones = new OpcionesDeAlmacenamiento
        {
            Ruta = "/tmp/no-usada",
            ClaveBase64 = Convert.ToBase64String(new byte[16]),
        };

        var excepcion = Assert.Throws<InvalidOperationException>(() => opciones.ResolverClave());

        Assert.Contains("32 bytes", excepcion.Message);
    }

    [Theory(DisplayName = "RNF-55: la normalización iguala mayúsculas, acentos y espacios sobrantes")]
    [InlineData("  Hospital Central  ", "hospital central")]
    [InlineData("Cardiología", "cardiologia")]
    [InlineData("ECOGRAFÍA   ABDOMINAL", "ecografia abdominal")]
    [InlineData(null, "")]
    public void LaNormalizacion_IgualaLasVariantes(string? entrada, string esperado)
    {
        Assert.Equal(esperado, NormalizadorDeTexto.Normalizar(entrada));
    }

    private static string DescribirCadena(Exception excepcion)
    {
        var textos = new List<string>();

        for (var actual = excepcion; actual is not null; actual = actual.InnerException)
        {
            textos.Add(actual.Message);
        }

        return string.Join(" | ", textos);
    }
}
