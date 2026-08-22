using System.Net;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace MiArchivoMedico.Tests;

/// <summary>
/// Tests de humo del esqueleto de la solución (Bloque 1 de FEAT-001a).
/// </summary>
public class ArranqueTests
{
    [Fact]
    public void Host_Arranca_Y_Resuelve_El_Contenedor()
    {
        // `AppFactory` y no `WebApplicationFactory<Program>` a secas: desde el Bloque 2 la
        // aplicación aborta el arranque si no le declaran la ruta de la base, y este test verifica
        // que el host arranca, no que la configuración falte.
        using var fabrica = new AppFactory();

        // A propósito no se solicita ninguna ruta: FEAT-001b instalará autorización por defecto
        // en todas las rutas, y un smoke test apoyado en una ruta anónima obligaría a aquel
        // ticket a "arreglar" este test, que es como se erosiona una invariante de privacidad.
        //
        // Acceder a `Services` es lo que construye el host: si la composición fuera inválida
        // (E1.3), la excepción se propaga acá y el test falla. `GetRequiredService` lanza por sí
        // mismo cuando el servicio no está registrado, así que no lleva una aserción encima.
        var servicios = fabrica.Services;

        // Afirmar el nombre de aplicación detecta que la fábrica haya quedado atada al ensamblado
        // equivocado, un modo de falla real de `WebApplicationFactory<Program>`.
        var entorno = servicios.GetRequiredService<IHostEnvironment>();
        Assert.Equal("MiArchivoMedico.Web", entorno.ApplicationName);

        using var alcance = servicios.CreateScope();
        alcance.ServiceProvider.GetRequiredService<IConfiguration>();
    }

    [Fact]
    public void Ejecuta_Sobre_Net8()
    {
        Assert.Equal(8, Environment.Version.Major);

        var marco = typeof(Program).Assembly.GetCustomAttribute<TargetFrameworkAttribute>();
        Assert.NotNull(marco);
        Assert.Equal(".NETCoreApp,Version=v8.0", marco.FrameworkName);

        // Ni el runtime en ejecución ni el TFM rompen si `global.json` desaparece: el primero lo
        // fija el host que ya arrancó y el segundo el .csproj. El riesgo que E1.1 dice mitigar
        // —que la máquina resuelva un SDK distinto de 8.0.x— solo lo cubre el archivo mismo, así
        // que se afirma su contenido real y no un efecto que sobreviviría a su borrado.
        var rutaGlobalJson = Path.Combine(Repositorio.RaizDelRepositorio(), "global.json");
        Assert.True(File.Exists(rutaGlobalJson), $"No se encontró global.json en '{rutaGlobalJson}'.");

        using var documento = JsonDocument.Parse(File.ReadAllText(rutaGlobalJson));

        Assert.True(
            documento.RootElement.TryGetProperty("sdk", out var sdk),
            "global.json no declara la sección 'sdk'.");

        Assert.True(
            sdk.TryGetProperty("version", out var version),
            "global.json no fija 'sdk.version'.");

        var textoDeVersion = version.GetString();
        Assert.True(
            textoDeVersion is not null && textoDeVersion.StartsWith("8.", StringComparison.Ordinal),
            $"global.json fija el SDK en '{textoDeVersion}', que no pertenece a la serie 8.x.");

        Assert.True(
            sdk.TryGetProperty("rollForward", out var avance),
            "global.json no fija 'sdk.rollForward'.");

        Assert.Equal("latestFeature", avance.GetString());
    }

    [Fact]
    public void Repositorio_Ignora_Artefactos_De_Build_Y_Bases()
    {
        var rutaGitignore = Path.Combine(Repositorio.RaizDelRepositorio(), ".gitignore");
        Assert.True(File.Exists(rutaGitignore), $"No se encontró .gitignore en '{rutaGitignore}'.");

        var patrones = File.ReadAllLines(rutaGitignore)
            .Select(linea => linea.Trim())
            .Where(linea => linea.Length > 0 && !linea.StartsWith('#'))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var esperado in new[] { "bin/", "obj/", "*.db", "*.db-wal", "*.db-shm" })
        {
            Assert.True(
                patrones.Contains(esperado),
                $"El .gitignore no ignora el patrón '{esperado}'.");
        }
    }

    [Fact]
    public async Task Fuera_De_Desarrollo_No_Expone_Pagina_De_Error_Detallada()
    {
        // La excepción la provoca un controlador que vive SOLO en el proceso de tests (E1.4 de
        // FEAT-001b): ControladorDePruebasDeExcepcion se registra via AddApplicationPart del
        // ensamblado de tests. La aplicación no expone ninguna ruta ni ninguna clave de
        // configuración para esto. Aun así se ejercita el pipeline real —el manejador genérico
        // que arma `Program.cs`— porque el controlador queda por delante de la autorización en
        // el enrutado (tiene [AllowAnonymous]).
        using var fabricaConBase = new AppFactory();
        using var fabrica = fabricaConBase
            .WithWebHostBuilder(constructor =>
            {
                constructor.UseEnvironment("Production");
            });

        using var cliente = fabrica.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var respuesta = await cliente.PostAsync("/Pruebas/De-Excepcion/Provocar", null);
        var cuerpo = await respuesta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, respuesta.StatusCode);

        // Aserción positiva: sin ella, un 500 con cuerpo vacío satisfaría todos los
        // `DoesNotContain` de abajo. Fija además que el mensaje es genérico.
        Assert.Equal("Se produjo un error inesperado.", cuerpo);

        foreach (var filtracion in new[]
                 {
                     ControladorDePruebasDeExcepcion.MarcadorDeExcepcion,
                     nameof(InvalidOperationException),
                     "stack",
                     "MiArchivoMedico.Web",
                     "at Program",
                 })
        {
            Assert.DoesNotContain(filtracion, cuerpo, StringComparison.OrdinalIgnoreCase);
        }
    }
}
