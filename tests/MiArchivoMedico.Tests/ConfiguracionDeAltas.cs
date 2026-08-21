using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MiArchivoMedico.Web.Accounts;
using MiArchivoMedico.Web.Data;
using Xunit;

namespace MiArchivoMedico.Tests;

/// <summary>
/// Alta tal como la declara el administrador técnico en la configuración externa. Los datos son
/// ficticios: `AGENTS.md` prohíbe secretos reales en fixtures.
/// </summary>
internal sealed record AltaDeclarada(string UserName, string Password);

/// <summary>
/// Utilidades compartidas por los tests del alta de cuentas: inyectan las altas en memoria —jamás
/// un `appsettings.Test.json` con contraseñas— y leen el estado persistido.
/// </summary>
internal static class ConfiguracionDeAltas
{
    /// <summary>Contraseña sintética de una cuenta de prueba; siempre supera el mínimo de NFR-03.</summary>
    internal static string ContrasenaDe(string usuario) => $"contrasena-declarada-de-{usuario}-2026";

    /// <summary>Alta válida para la cuenta indicada.</summary>
    internal static AltaDeclarada Alta(string usuario) => new(usuario, ContrasenaDe(usuario));

    /// <summary>Host de pruebas con las altas declaradas en la configuración.</summary>
    internal static WebApplicationFactory<Program> Arranque(AppFactory fabrica, params AltaDeclarada[] altas) =>
        ArranqueConRegistro(fabrica, registro: null, altas);

    /// <summary>Host de pruebas con las altas declaradas y, si se pide, el registro capturado.</summary>
    internal static WebApplicationFactory<Program> ArranqueConRegistro(
        AppFactory fabrica,
        RegistroEnMemoria? registro,
        params AltaDeclarada[] altas) =>
        fabrica.WithWebHostBuilder(constructor =>
        {
            constructor.ConfigureAppConfiguration(configuracion =>
                configuracion.AddInMemoryCollection(ClavesDe(altas)));

            if (registro is not null)
            {
                constructor.ConfigureLogging(registros => registros.AddProvider(registro));
            }
        });

    /// <summary>
    /// Host de pruebas con claves de configuración crudas. Permite declarar entradas incompletas
    /// —una sin la clave `Password`, por ejemplo— que <see cref="ClavesDe"/> no puede expresar.
    /// </summary>
    internal static WebApplicationFactory<Program> ArranqueConClaves(
        AppFactory fabrica,
        RegistroEnMemoria? registro,
        params KeyValuePair<string, string?>[] claves) =>
        fabrica.WithWebHostBuilder(constructor =>
        {
            constructor.ConfigureAppConfiguration(configuracion =>
                configuracion.AddInMemoryCollection(claves));

            if (registro is not null)
            {
                constructor.ConfigureLogging(registros => registros.AddProvider(registro));
            }
        });

    /// <summary>Clave de configuración de una propiedad de la entrada declarada en la posición dada.</summary>
    internal static KeyValuePair<string, string?> Clave(int indice, string propiedad, string? valor) =>
        new(
            string.Create(
                CultureInfo.InvariantCulture,
                $"{AccountSeedOptions.ClaveDeConfiguracion}:{indice}:{propiedad}"),
            valor);

    /// <summary>Cuentas persistidas, leídas por el contexto de la propia aplicación.</summary>
    internal static IReadOnlyList<AppUser> Cuentas(IServiceProvider servicios)
    {
        using var alcance = servicios.CreateScope();

        return alcance.ServiceProvider.GetRequiredService<AppDbContext>()
            .Users
            .AsNoTracking()
            .OrderBy(cuenta => cuenta.UserName)
            .ToList();
    }

    /// <summary>Comprueba que la cuenta valida la contraseña que se declaró para ella.</summary>
    internal static async Task<bool> VerificaLaContrasenaAsync(IServiceProvider servicios, AltaDeclarada alta)
    {
        using var alcance = servicios.CreateScope();
        var cuentas = alcance.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var cuenta = await cuentas.FindByNameAsync(alta.UserName);

        return cuenta is not null && await cuentas.CheckPasswordAsync(cuenta, alta.Password);
    }

    /// <summary>Claves de configuración que produciría el entorno para las altas declaradas.</summary>
    private static IEnumerable<KeyValuePair<string, string?>> ClavesDe(IReadOnlyList<AltaDeclarada> altas)
    {
        for (var indice = 0; indice < altas.Count; indice++)
        {
            var prefijo = string.Create(
                CultureInfo.InvariantCulture,
                $"{AccountSeedOptions.ClaveDeConfiguracion}:{indice}");

            yield return new KeyValuePair<string, string?>(
                $"{prefijo}:{nameof(AccountSeedEntry.UserName)}", altas[indice].UserName);

            yield return new KeyValuePair<string, string?>(
                $"{prefijo}:{nameof(AccountSeedEntry.Password)}", altas[indice].Password);
        }
    }
}

/// <summary>
/// Proveedor de registro que retiene en memoria todos los mensajes, de todas las categorías, para
/// que los tests puedan afirmar qué se registró y —sobre todo— qué no (AC-08, NFR-05).
/// </summary>
internal sealed class RegistroEnMemoria : ILoggerProvider
{
    private readonly ConcurrentQueue<string> mensajes = new();

    /// <summary>Mensajes capturados hasta el momento, ya formateados.</summary>
    internal IReadOnlyList<string> Mensajes => mensajes.ToArray();

    public ILogger CreateLogger(string categoryName) => new RegistroDeCategoria(categoryName, mensajes);

    public void Dispose()
    {
        // El proveedor no toma ningún recurso: los mensajes viven en una cola en memoria que el
        // recolector se lleva con la instancia. `ILoggerProvider` obliga a declararlo igual.
    }

    private sealed class RegistroDeCategoria(string categoria, ConcurrentQueue<string> mensajes) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            // Se guarda también la excepción completa: una credencial filtrada por el mensaje de
            // una excepción cuenta igual que una filtrada por el texto del registro.
            mensajes.Enqueue($"{logLevel} {categoria} {formatter(state, exception)} {exception}");
        }
    }
}

/// <summary>
/// Ubicación del árbol del repositorio, compartida por los tests que afirman sobre archivos
/// versionados (`global.json`, `.gitignore`, `appsettings.json`).
/// </summary>
internal static class Repositorio
{
    /// <summary>Sube desde el directorio de ejecución hasta el directorio que contiene `.git`.</summary>
    internal static string RaizDelRepositorio()
    {
        var directorio = new DirectoryInfo(AppContext.BaseDirectory);

        // En un worktree enlazado `.git` es un archivo, no un directorio: de ahí `Path.Exists`.
        while (directorio is not null && !Path.Exists(Path.Combine(directorio.FullName, ".git")))
        {
            directorio = directorio.Parent;
        }

        Assert.NotNull(directorio);
        return directorio.FullName;
    }
}
