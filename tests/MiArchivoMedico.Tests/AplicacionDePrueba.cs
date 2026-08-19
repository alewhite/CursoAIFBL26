using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace MiArchivoMedico.Tests;

/// <summary>
/// Levanta la aplicación real sobre una base SQLite descartable y un almacenamiento temporal, con el
/// tiempo simulado para poder ejercitar expiraciones y ventanas de bloqueo sin esperarlas.
/// La configuración se pasa con UseSetting y no con ConfigureAppConfiguration, porque Program lee la
/// cadena de conexión durante CreateBuilder, antes de que corran los callbacks de la fábrica.
/// </summary>
public class AplicacionDePrueba : WebApplicationFactory<Program>
{
    public const string UsuarioUno = "ana.prueba";
    public const string UsuarioDos = "bruno.prueba";
    public const string Contrasena = "contrasena-de-prueba-larga";

    public static readonly DateTimeOffset MomentoInicial = new(2026, 1, 15, 9, 0, 0, TimeSpan.Zero);

    private readonly string _carpeta = Path.Combine(
        Path.GetTempPath(), "archivo-medico-pruebas", Guid.NewGuid().ToString("N"));

    public FakeTimeProvider Reloj { get; } = new(MomentoInicial);

    public string RutaDeAlmacenamiento => Path.Combine(_carpeta, "almacenamiento");
    private string RutaDeBase => Path.Combine(_carpeta, "archivo-medico.db");

    protected override void ConfigureWebHost(IWebHostBuilder constructor)
    {
        Directory.CreateDirectory(RutaDeAlmacenamiento);

        constructor.UseSetting("ConnectionStrings:ArchivoMedico", $"Data Source={RutaDeBase}");
        constructor.UseSetting("Almacenamiento:Ruta", RutaDeAlmacenamiento);
        constructor.UseSetting("Almacenamiento:ClaveBase64", Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
        constructor.UseSetting("Almacenamiento:CupoTotalEnBytes", (20L * 1024 * 1024 * 1024).ToString());

        constructor.UseSetting("CuentasIniciales:0:NombreDeUsuario", UsuarioUno);
        constructor.UseSetting("CuentasIniciales:0:Email", "ana@ejemplo.invalido");
        constructor.UseSetting("CuentasIniciales:0:Contrasena", Contrasena);
        constructor.UseSetting("CuentasIniciales:1:NombreDeUsuario", UsuarioDos);
        constructor.UseSetting("CuentasIniciales:1:Email", "bruno@ejemplo.invalido");
        constructor.UseSetting("CuentasIniciales:1:Contrasena", Contrasena);

        constructor.ConfigureServices(servicios =>
        {
            servicios.RemoveAll<TimeProvider>();
            servicios.AddSingleton<TimeProvider>(Reloj);

            // El manejador de la cookie también toma el tiempo simulado, para que la expiración por
            // inactividad y el tope absoluto sean observables sin esperar.
            servicios.AddSingleton<IPostConfigureOptions<CookieAuthenticationOptions>>(
                new RelojDeCookie(Reloj));
        });
    }

    protected override void Dispose(bool liberando)
    {
        base.Dispose(liberando);
        if (!liberando) return;
        try { if (Directory.Exists(_carpeta)) Directory.Delete(_carpeta, recursive: true); }
        catch (IOException) { /* la base puede seguir tomada; el temporal se limpia solo */ }
    }

    private sealed class RelojDeCookie(TimeProvider reloj) : IPostConfigureOptions<CookieAuthenticationOptions>
    {
        public void PostConfigure(string? nombre, CookieAuthenticationOptions opciones) =>
            opciones.TimeProvider = reloj;
    }
}
