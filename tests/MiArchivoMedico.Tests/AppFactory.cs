using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using MiArchivoMedico.Web.Data;

namespace MiArchivoMedico.Tests;

/// <summary>
/// Host de pruebas de la aplicación. Apunta la base a un archivo real dentro de un directorio
/// temporal del sistema operativo —nunca a SQLite en memoria, que no tiene modo WAL y dejaría
/// AC-06 sin probar, ni a un archivo bajo <c>bin/</c>, que sigue dentro del repositorio— y lo
/// borra al liberarse.
/// </summary>
internal sealed class AppFactory : WebApplicationFactory<Program>
{
    /// <summary>
    /// Clave de configuración externa que declara la ubicación del archivo de base. Se toma de
    /// producción y no se repite como literal: si la clave se renombrara, un literal propio dejaría
    /// las aserciones comprobando una cadena que ya no existe, y seguirían pasando.
    /// </summary>
    internal const string ClaveDeLaRutaDeLaBase = SqliteLocation.ClaveDeConfiguracion;

    private readonly bool declararLaRuta;

    /// <summary>Fábrica con una ruta de base válida, fuera del árbol de la aplicación.</summary>
    public AppFactory()
        : this(rutaExplicita: null, declararLaRuta: true)
    {
    }

    private AppFactory(string? rutaExplicita, bool declararLaRuta)
    {
        this.declararLaRuta = declararLaRuta;

        DirectorioTemporal = Path.Combine(
            Path.GetTempPath(),
            "mi-archivo-medico-tests",
            Guid.NewGuid().ToString("n"));

        Directory.CreateDirectory(DirectorioTemporal);

        // El subdirectorio `datos` queda deliberadamente sin crear: crearlo es responsabilidad de
        // la aplicación y es lo que verifica `Crea_El_Directorio_Con_Permisos_Restrictivos`.
        RutaDeLaBase = rutaExplicita ?? Path.Combine(DirectorioTemporal, "datos", "archivo.db");
    }

    /// <summary>Fábrica que no declara la clave de la ruta de la base (E2.1).</summary>
    public static AppFactory SinRutaDeBase() => new(rutaExplicita: null, declararLaRuta: false);

    /// <summary>Fábrica que declara una ruta de base elegida por el test (E2.2, E2.3, E2.4).</summary>
    public static AppFactory ConRutaDeBase(string ruta) => new(ruta, declararLaRuta: true);

    /// <summary>Directorio temporal propio de esta fábrica; se borra al liberarla.</summary>
    public string DirectorioTemporal { get; }

    /// <summary>Ruta del archivo de base que se declara en la configuración.</summary>
    public string RutaDeLaBase { get; }

    /// <summary>Directorio que contiene el archivo de base.</summary>
    public string DirectorioDeLaBase => Path.GetDirectoryName(RutaDeLaBase)!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // El ensamblado de tests expone el controlador de solo-tests que ejercita el manejador de
        // errores fuera de Development (E1.4). SE REGISTRA desde el build de pruebas, nunca desde el
        // ensamblado de la aplicación: la producción no referencia el proyecto de tests (E1.2).
        builder.ConfigureServices(servicios =>
            servicios.AddControllers().AddApplicationPart(typeof(ControladorDePruebasDeExcepcion).Assembly));

        if (!declararLaRuta)
        {
            return;
        }

        // `UseSetting` y no `ConfigureAppConfiguration`: la aplicación resuelve la ubicación de la
        // base ANTES de `builder.Build()` para poder abortar el arranque, y las fuentes agregadas
        // con `ConfigureAppConfiguration` recién se aplican al construir el host, cuando ese
        // código ya se ejecutó. `UseSetting` escribe en la configuración del host, que sí está
        // disponible desde el primer momento en `builder.Configuration`.
        builder.UseSetting(ClaveDeLaRutaDeLaBase, RutaDeLaBase);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            BorrarDirectorioTemporal();
        }
    }

    private void BorrarDirectorioTemporal()
    {
        // El proveedor de SQLite mantiene un pool de conexiones; sin vaciarlo, el archivo sigue
        // abierto y en algunas plataformas no puede borrarse.
        SqliteConnection.ClearAllPools();

        try
        {
            if (Directory.Exists(DirectorioTemporal))
            {
                Directory.Delete(DirectorioTemporal, recursive: true);
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Un temporal que no se pudo borrar —porque quedó un identificador abierto o porque
            // los permisos lo impiden— no debe hacer fallar un test: el sistema operativo lo
            // recicla igual.
        }
    }
}
