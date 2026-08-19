using System.Net;
using MiArchivoMedico.Tests.Apoyo;

namespace MiArchivoMedico.Tests;

/// <summary>
/// El token y la sesión son condiciones acumulativas: un token vencido rechaza aunque haya sesión, y
/// la ausencia de sesión rechaza aunque el token siga vigente (RNF-06, RNF-07, AC-08, AC-84).
/// </summary>
public class TokenDeArchivoTests
{
    [Fact(DisplayName = "AC-08: pasados 5 minutos, el token de acceso al archivo deja de servir")]
    public async Task TokenVencido_RechazaLaEntrega()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();
        var archivo = await EntregaDeArchivosTests.SembrarArchivoAsync(aplicacion, cliente);
        var ruta = await EntregaDeArchivosTests.RutaDeContenidoAsync(cliente, archivo);

        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync(ruta)).StatusCode);

        aplicacion.Reloj.Advance(TimeSpan.FromMinutes(6));

        var vencido = await cliente.GetAsync(ruta);
        Assert.Equal(HttpStatusCode.NotFound, vencido.StatusCode);
    }

    [Fact(DisplayName = "AC-84: sin sesión, la misma ruta no entrega el archivo ni con el token vigente")]
    public async Task TokenVigenteSinSesion_NoEntregaElArchivo()
    {
        using var aplicacion = new AplicacionDePrueba();
        var conSesion = await aplicacion.ClienteAutenticadoAsync();
        var archivo = await EntregaDeArchivosTests.SembrarArchivoAsync(aplicacion, conSesion);
        var ruta = await EntregaDeArchivosTests.RutaDeContenidoAsync(conSesion, archivo);

        var sinSesion = aplicacion.CrearClienteSinRedirecciones();

        var deInmediato = await sinSesion.GetAsync(ruta);
        Assert.NotEqual(HttpStatusCode.OK, deInmediato.StatusCode);

        aplicacion.Reloj.Advance(TimeSpan.FromMinutes(6));

        var masTarde = await sinSesion.GetAsync(ruta);
        Assert.NotEqual(HttpStatusCode.OK, masTarde.StatusCode);
    }

    [Fact(DisplayName = "RNF-07: una ruta de contenido sin token no entrega el archivo")]
    public async Task SinToken_NoEntregaElArchivo()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();
        var archivo = await EntregaDeArchivosTests.SembrarArchivoAsync(aplicacion, cliente);

        var respuesta = await cliente.GetAsync($"/Archivos/Contenido/{archivo}");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Fact(DisplayName = "RNF-07: un token emitido para otro archivo no sirve")]
    public async Task TokenDeOtroArchivo_NoSirve()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();
        var archivo = await EntregaDeArchivosTests.SembrarArchivoAsync(aplicacion, cliente);
        var ruta = await EntregaDeArchivosTests.RutaDeContenidoAsync(cliente, archivo);

        var token = ruta[(ruta.IndexOf("?t=", StringComparison.Ordinal) + 3)..];
        var otro = await cliente.GetAsync($"/Archivos/Contenido/{Guid.NewGuid()}?t={token}");

        Assert.Equal(HttpStatusCode.NotFound, otro.StatusCode);
    }
}
