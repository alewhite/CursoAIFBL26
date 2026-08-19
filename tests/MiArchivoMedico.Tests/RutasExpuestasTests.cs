using MiArchivoMedico.Tests.Apoyo;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace MiArchivoMedico.Tests;

/// <summary>
/// Enumera las rutas que la aplicación registra realmente, en lugar de una lista escrita a mano que
/// envejece. Verifica ausencias: lo que no debe existir (AC-50, AC-63, RNF-39).
/// </summary>
public class RutasExpuestasTests(AplicacionDePrueba aplicacion) : IClassFixture<AplicacionDePrueba>
{
    private static readonly string[] PalabrasProhibidas =
        ["registr", "signup", "crearcuenta", "nuevacuenta", "exportar", "importar", "restaurar",
         "transferir", "compartir", "delegar", "cambiarpropietario"];

    [Fact(DisplayName = "AC-50, AC-63, RNF-39: ninguna ruta ofrece alta de cuenta, cambio de propietario ni exportación")]
    public void RutasRegistradas_NoIncluyenLasProhibidas()
    {
        var patrones = ObtenerPatrones();

        var encontradas = patrones
            .Where(p => PalabrasProhibidas.Any(x => p.Contains(x, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        Assert.True(encontradas.Count == 0,
            $"La aplicación expone rutas que no debería: {string.Join(", ", encontradas)}");
    }

    [Fact(DisplayName = "AC-50: una solicitud a una ruta de registro responde 404")]
    public async Task RutaDeRegistro_Responde404()
    {
        // Se autentica primero a propósito: sin sesión, el desafío de autenticación se adelanta al 404
        // y devuelve una redirección al ingreso. Esa respuesta es más estricta —no revela siquiera si
        // la ruta existe— pero taparía lo que este criterio quiere comprobar, que es que la ruta no
        // existe para nadie.
        var cliente = aplicacion.CrearClienteSinRedirecciones();
        await cliente.IniciarSesionAsync(AplicacionDePrueba.UsuarioUno, AplicacionDePrueba.Contrasena);

        foreach (var ruta in new[] { "/Cuenta/Registrar", "/Cuenta/Registro", "/Identity/Account/Register" })
        {
            var respuesta = await cliente.GetAsync(ruta);
            Assert.Equal(System.Net.HttpStatusCode.NotFound, respuesta.StatusCode);
        }
    }

    private IReadOnlyCollection<string> ObtenerPatrones()
    {
        var fuentes = aplicacion.Services.GetRequiredService<IEnumerable<EndpointDataSource>>();
        return fuentes
            .SelectMany(f => f.Endpoints)
            .OfType<RouteEndpoint>()
            .Select(e => e.RoutePattern.RawText ?? string.Empty)
            .Where(p => p.Length > 0)
            .ToList();
    }
}
