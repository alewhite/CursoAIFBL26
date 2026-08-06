using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace MiArchivoMedico.Tests.Infraestructura;

/// <summary>
/// Aplicación levantada en memoria sobre una base SQLite descartable y un reloj controlable.
/// Todos los datos son ficticios (RNF-10).
/// </summary>
public sealed class AplicacionDePrueba : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string Usuario = "paciente.uno";
    public const string Contrasena = "Ficticia-2026-uno";
    public const string OtroUsuario = "paciente.dos";
    public const string OtraContrasena = "Ficticia-2026-dos";

    private readonly string _rutaBase = Path.Combine(
        Path.GetTempPath(), $"archivo-medico-tests-{Guid.NewGuid():N}");

    public FakeTimeProvider Reloj { get; } = new(new DateTimeOffset(2026, 1, 15, 9, 0, 0, TimeSpan.Zero));

    /// <summary>Cuentas sembradas por omisión. Los tests de cupo agregan las que necesiten.</summary>
    public List<(string Usuario, string Email, string Contrasena)> CuentasIniciales { get; } =
    [
        (Usuario, "paciente.uno@ejemplo.invalid", Contrasena),
        (OtroUsuario, "paciente.dos@ejemplo.invalid", OtraContrasena),
    ];

    public Task InitializeAsync()
    {
        Directory.CreateDirectory(_rutaBase);
        return Task.CompletedTask;
    }

    public new Task DisposeAsync()
    {
        base.Dispose();
        if (Directory.Exists(_rutaBase))
        {
            Directory.Delete(_rutaBase, recursive: true);
        }

        return Task.CompletedTask;
    }

    /// <summary>Cliente que conserva cookies y habla https, para que la cookie Secure viaje de vuelta.</summary>
    public HttpClient CrearCliente() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
        HandleCookies = true,
    });

    /// <summary>Ejecuta una acción con los servicios de la aplicación, en su propio alcance.</summary>
    public async Task EnAlcanceAsync(Func<IServiceProvider, Task> accion)
    {
        using var alcance = Services.CreateScope();
        await accion(alcance.ServiceProvider);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");

        // UseSetting y no ConfigureAppConfiguration: con hosting mínimo, Program lee la cadena de conexión
        // durante CreateBuilder, antes de que corran los callbacks de configuración de la fábrica.
        builder.UseSetting(
            "ConnectionStrings:ArchivoMedico",
            $"Data Source={Path.Combine(_rutaBase, "archivo-medico.db")}");

        for (var i = 0; i < CuentasIniciales.Count; i++)
        {
            builder.UseSetting($"CuentasIniciales:{i}:NombreDeUsuario", CuentasIniciales[i].Usuario);
            builder.UseSetting($"CuentasIniciales:{i}:Email", CuentasIniciales[i].Email);
            builder.UseSetting($"CuentasIniciales:{i}:Contrasena", CuentasIniciales[i].Contrasena);
        }

        builder.ConfigureServices(servicios =>
        {
            servicios.Replace(ServiceDescriptor.Singleton<TimeProvider>(Reloj));

            // El manejador de cookies también debe usar el reloj falso: sin esto, la expiración por
            // inactividad y la duración absoluta no se pueden ejercitar en una prueba.
            servicios.AddSingleton<IPostConfigureOptions<CookieAuthenticationOptions>>(
                new RelojDeCookies(Reloj));
        });
    }

    private sealed class RelojDeCookies : IPostConfigureOptions<CookieAuthenticationOptions>
    {
        private readonly TimeProvider _reloj;

        public RelojDeCookies(TimeProvider reloj) => _reloj = reloj;

        public void PostConfigure(string? name, CookieAuthenticationOptions options)
        {
            if (name == IdentityConstants.ApplicationScheme)
            {
                options.TimeProvider = _reloj;
            }
        }
    }
}
