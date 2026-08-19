using System.Net;
using MiArchivoMedico.Tests.Apoyo;

namespace MiArchivoMedico.Tests;

public class AutenticacionTests(AplicacionDePrueba aplicacion) : IClassFixture<AplicacionDePrueba>
{
    [Fact(DisplayName = "AC-01: sin sesión, la página de estudios redirige al ingreso y no muestra datos médicos")]
    public async Task SinSesion_ListadoRedirigeAlIngreso()
    {
        var cliente = aplicacion.CrearClienteSinRedirecciones();

        var respuesta = await cliente.GetAsync("/Estudios");

        Assert.True(respuesta.RedirigeAlIngreso(), $"Se esperaba redirección al ingreso y llegó {respuesta.StatusCode}.");
        Assert.DoesNotContain("estudio", await respuesta.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "AC-02: sin sesión, la URL interna de un archivo no entrega el archivo")]
    public async Task SinSesion_ArchivoNoSeEntrega()
    {
        var cliente = aplicacion.CrearClienteSinRedirecciones();

        var respuesta = await cliente.GetAsync($"/Archivos/Contenido/{Guid.NewGuid()}");

        Assert.True(
            respuesta.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
                || respuesta.RedirigeAlIngreso(),
            $"Se esperaba 401, 403 o redirección al ingreso y llegó {respuesta.StatusCode}.");
    }

    [Fact(DisplayName = "AC-03: con credenciales válidas, el usuario accede al listado de estudios")]
    public async Task CredencialesValidas_AccedeAlListado()
    {
        var cliente = aplicacion.CrearClienteSinRedirecciones();

        var ingreso = await cliente.IniciarSesionAsync(AplicacionDePrueba.UsuarioUno, AplicacionDePrueba.Contrasena);
        Assert.Equal(HttpStatusCode.Redirect, ingreso.StatusCode);

        var listado = await cliente.GetAsync("/Estudios");
        Assert.Equal(HttpStatusCode.OK, listado.StatusCode);
    }

    [Fact(DisplayName = "AC-04: el mensaje es idéntico para usuario inexistente y para contraseña incorrecta")]
    public async Task CredencialesInvalidas_MensajeIdentico()
    {
        var clienteA = aplicacion.CrearClienteSinRedirecciones();
        var clienteB = aplicacion.CrearClienteSinRedirecciones();

        var usuarioInexistente = await clienteA.IniciarSesionAsync("nadie.existe", AplicacionDePrueba.Contrasena);
        var contrasenaIncorrecta = await clienteB.IniciarSesionAsync(AplicacionDePrueba.UsuarioUno, "otra-contrasena-larga");

        Assert.Equal(HttpStatusCode.OK, usuarioInexistente.StatusCode);
        Assert.Equal(HttpStatusCode.OK, contrasenaIncorrecta.StatusCode);

        var mensajeA = ExtraerMensajeDeError(await usuarioInexistente.Content.ReadAsStringAsync());
        var mensajeB = ExtraerMensajeDeError(await contrasenaIncorrecta.Content.ReadAsStringAsync());

        Assert.False(string.IsNullOrWhiteSpace(mensajeA), "No se encontró el mensaje de error en la respuesta.");
        Assert.Equal(mensajeA, mensajeB);
    }

    [Fact(DisplayName = "AC-05: al cerrar sesión, la sesión deja de ser válida")]
    public async Task CerrarSesion_InvalidaLaSesion()
    {
        var cliente = aplicacion.CrearClienteSinRedirecciones();
        await cliente.IniciarSesionAsync(AplicacionDePrueba.UsuarioUno, AplicacionDePrueba.Contrasena);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/Estudios")).StatusCode);

        await cliente.CerrarSesionAsync();

        Assert.True((await cliente.GetAsync("/Estudios")).RedirigeAlIngreso());
    }

    internal static string ExtraerMensajeDeError(string html)
    {
        var marca = "aviso error";
        var inicio = html.IndexOf(marca, StringComparison.OrdinalIgnoreCase);
        if (inicio < 0) return string.Empty;
        var cierre = html.IndexOf('>', inicio);
        var fin = html.IndexOf("</", cierre, StringComparison.Ordinal);
        return cierre < 0 || fin < 0 ? string.Empty : html[(cierre + 1)..fin].Trim();
    }
}
