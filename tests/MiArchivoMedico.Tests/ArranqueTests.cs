namespace MiArchivoMedico.Tests;

/// <summary>
/// El rechazo del arranque ocurre antes de que la aplicación quede disponible, así que no puede
/// ejercitarse con la fábrica habitual: se construye el host directamente (AC-83).
/// </summary>
public class ArranqueTests
{
    [Fact(DisplayName = "AC-83: sin clave de cifrado, la aplicación no arranca")]
    public void SinClaveDeCifrado_ElArranqueFalla()
    {
        var error = Assert.ThrowsAny<Exception>(() => ConstruirAplicacion(clave: null));

        Assert.Contains("ClaveBase64", DesenrollarMensaje(error), StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "RNF-62: una clave que no decodifica a 32 bytes impide el arranque")]
    public void ClaveDeLargoIncorrecto_ElArranqueFalla()
    {
        var error = Assert.ThrowsAny<Exception>(
            () => ConstruirAplicacion(Convert.ToBase64String(new byte[16])));

        Assert.Contains("32", DesenrollarMensaje(error));
    }

    [Fact(DisplayName = "RNF-62: sin cadena de conexión, la aplicación no arranca")]
    public void SinCadenaDeConexion_ElArranqueFalla()
    {
        var error = Assert.ThrowsAny<Exception>(
            () => ConstruirAplicacion(Convert.ToBase64String(new byte[32]), cadenaDeConexion: null));

        Assert.Contains("ArchivoMedico", DesenrollarMensaje(error), StringComparison.OrdinalIgnoreCase);
    }

    private static void ConstruirAplicacion(string? clave, string? cadenaDeConexion = "Data Source=:memory:")
    {
        using var fabrica = new AplicacionSinConfiguracion(clave, cadenaDeConexion);
        _ = fabrica.Services;              // fuerza la construcción del host
    }

    private static string DesenrollarMensaje(Exception error)
    {
        var mensajes = new List<string>();
        for (Exception? actual = error; actual is not null; actual = actual.InnerException)
            mensajes.Add(actual.Message);
        return string.Join(" | ", mensajes);
    }

    private sealed class AplicacionSinConfiguracion(string? clave, string? cadenaDeConexion)
        : Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder constructor)
        {
            constructor.UseSetting("ConnectionStrings:ArchivoMedico", cadenaDeConexion ?? string.Empty);
            constructor.UseSetting("Almacenamiento:Ruta", Path.Combine(Path.GetTempPath(), "arranque-prueba"));
            constructor.UseSetting("Almacenamiento:ClaveBase64", clave ?? string.Empty);
        }
    }
}
