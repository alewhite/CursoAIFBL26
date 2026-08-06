using System.Net;
using MiArchivoMedico.Tests.Infraestructura;

namespace MiArchivoMedico.Tests;

/// <summary>
/// El requisito crítico del sistema: un recurso de otra cuenta no se entrega aunque se conozca su
/// identificador (RNF-53, RNF-08).
/// </summary>
public class AislamientoEntreCuentasTests : IAsyncLifetime
{
    private readonly AplicacionDePrueba _app = new();

    public Task InitializeAsync() => _app.InitializeAsync();

    public Task DisposeAsync() => _app.DisposeAsync();

    [Fact(DisplayName = "AC-47: el detalle de un estudio ajeno responde 404 y no entrega metadatos")]
    public async Task ElDetalleDeUnEstudioAjeno_Responde404()
    {
        var (idDeEstudio, _) = await ComoPrimerUsuarioAsync();
        var intruso = await ComoSegundoUsuarioAsync();

        var respuesta = await intruso.GetAsync($"/Estudios/Detalle/{idDeEstudio}");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.DoesNotContain(
            "Hospital Ficticio Reservado", await respuesta.Content.ReadAsStringAsync());
    }

    [Fact(DisplayName = "AC-48: la descarga de un archivo ajeno responde 404")]
    public async Task LaDescargaDeUnArchivoAjeno_Responde404()
    {
        var (_, idDeArchivo) = await ComoPrimerUsuarioAsync();
        var intruso = await ComoSegundoUsuarioAsync();

        foreach (var ruta in new[] { "Ver", "Contenido", "Descargar" })
        {
            var respuesta = await intruso.GetAsync($"/Archivos/{ruta}/{idDeArchivo}");
            Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        }
    }

    [Fact(DisplayName = "AC-49: el listado de una cuenta no incluye los estudios de la otra")]
    public async Task ElListado_NoIncluyeEstudiosAjenos()
    {
        await ComoPrimerUsuarioAsync();

        var segundo = await ComoSegundoUsuarioAsync();
        await segundo.CrearYObtenerIdAsync("Estudio propio del segundo");

        var html = await (await segundo.GetAsync("/Estudios")).Content.ReadAsStringAsync();

        Assert.Contains("Estudio propio del segundo", html);
        Assert.DoesNotContain("Estudio reservado del primero", html);
    }

    [Fact(DisplayName = "AC-63: no hay forma de cambiar el propietario de un estudio")]
    public async Task NoHayFormaDeCambiarElPropietario()
    {
        var (idDeEstudio, _) = await ComoPrimerUsuarioAsync();
        var intruso = await ComoSegundoUsuarioAsync();

        // El detalle del propietario no ofrece ninguna acción de compartir, delegar ni transferir.
        var primero = await ComoPrimerUsuarioClienteAsync();
        var detalle = await (await primero.GetAsync($"/Estudios/Detalle/{idDeEstudio}"))
            .Content.ReadAsStringAsync();

        foreach (var termino in new[] { "compartir", "transferir", "delegar", "OwnerId", "propietario" })
        {
            Assert.DoesNotContain(termino, detalle, StringComparison.OrdinalIgnoreCase);
        }

        // Y una solicitud a mano que intente reasignarlo no encuentra a quién pedírselo.
        var token = await intruso.ObtenerTokenAsync("/Estudios/Crear");
        var reasignacion = await intruso.PostAsync(
            $"/Estudios/Editar/{idDeEstudio}",
            new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("__RequestVerificationToken", token),
                new KeyValuePair<string, string>("Titulo", "Robado"),
                new KeyValuePair<string, string>("Fecha", "2026-01-10"),
                new KeyValuePair<string, string>("OwnerId", "el-intruso"),
            ]));

        Assert.True(
            reasignacion.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden
                or HttpStatusCode.MethodNotAllowed,
            $"La reasignación respondió {(int)reasignacion.StatusCode}.");

        // El estudio sigue perteneciendo al primero, con su título original.
        var despues = await (await primero.GetAsync($"/Estudios/Detalle/{idDeEstudio}"))
            .Content.ReadAsStringAsync();
        Assert.Contains("Estudio reservado del primero", despues);
    }

    private async Task<(Guid IdDeEstudio, Guid IdDeArchivo)> ComoPrimerUsuarioAsync()
    {
        var cliente = await ComoPrimerUsuarioClienteAsync();

        var idDeEstudio = await cliente.CrearYObtenerIdAsync(
            "Estudio reservado del primero",
            institucion: "Hospital Ficticio Reservado",
            archivos: [new ArchivoAEnviar("informe.pdf", "application/pdf", ArchivosFicticios.Pdf())]);

        var idDeArchivo = (await cliente.ObtenerIdsDeArchivosAsync(idDeEstudio)).Single();
        return (idDeEstudio, idDeArchivo);
    }

    private async Task<HttpClient> ComoPrimerUsuarioClienteAsync()
    {
        var cliente = _app.CrearCliente();
        await cliente.IniciarSesionAsync(AplicacionDePrueba.Usuario, AplicacionDePrueba.Contrasena);
        return cliente;
    }

    private async Task<HttpClient> ComoSegundoUsuarioAsync()
    {
        var cliente = _app.CrearCliente();
        await cliente.IniciarSesionAsync(AplicacionDePrueba.OtroUsuario, AplicacionDePrueba.OtraContrasena);
        return cliente;
    }
}
