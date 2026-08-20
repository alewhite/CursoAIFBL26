using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MiArchivoMedico.Tests;

/// <summary>
/// Tests de la persistencia SQLite (Bloque 2 de FEAT-001a): esquema aplicado por migraciones,
/// modo WAL, ubicación fuera del árbol de la aplicación y los cuatro fallos de arranque E2.1 a
/// E2.4.
/// </summary>
public class PersistenciaTests
{
    [Fact]
    public void Aplica_Las_Migraciones_Y_Crea_El_Esquema()
    {
        using var fabrica = new AppFactory();

        _ = fabrica.Services;

        Assert.True(
            File.Exists(fabrica.RutaDeLaBase),
            $"La aplicación no creó el archivo de base en '{fabrica.RutaDeLaBase}'.");

        var tablas = TextosDeLaBase(
            fabrica.RutaDeLaBase,
            "SELECT name FROM sqlite_master WHERE type = 'table';");

        Assert.Contains("AspNetUsers", tablas);
        Assert.Contains("__EFMigrationsHistory", tablas);

        // ADR-001: el esquema llega por migraciones y no por `EnsureCreated`. Una base creada con
        // `EnsureCreated` tendría `AspNetUsers` y el historial vacío, así que esta aserción es la
        // que distingue una estrategia de la otra.
        var migraciones = TextosDeLaBase(
            fabrica.RutaDeLaBase,
            "SELECT MigrationId FROM __EFMigrationsHistory;");

        Assert.NotEmpty(migraciones);
    }

    [Fact]
    public void Base_Opera_En_Modo_Wal()
    {
        using var fabrica = new AppFactory();

        _ = fabrica.Services;

        Assert.True(
            File.Exists(fabrica.RutaDeLaBase),
            $"La aplicación no creó el archivo de base en '{fabrica.RutaDeLaBase}'.");

        // El modo de journal es una propiedad persistida en el encabezado del archivo: se lee
        // desde una conexión nueva, sobre el archivo real que dejó la aplicación.
        var modo = EscalarDeLaBase(fabrica.RutaDeLaBase, "PRAGMA journal_mode;") as string;

        Assert.Equal("wal", modo, ignoreCase: true);
    }

    [Fact]
    public void Base_Reside_Fuera_Del_Arbol_De_La_Aplicacion()
    {
        using var fabrica = new AppFactory();

        var entorno = fabrica.Services.GetRequiredService<IWebHostEnvironment>();
        var ruta = Path.GetFullPath(fabrica.RutaDeLaBase);

        // Que el archivo exista es lo que prueba que la aplicación usó realmente esta ubicación y
        // no otra: sin esta aserción, las dos de abajo se cumplirían con la base en cualquier lado.
        Assert.True(
            File.Exists(ruta),
            $"La aplicación no creó el archivo de base en '{ruta}'.");

        Assert.False(
            EstaDentroDe(ruta, entorno.ContentRootPath),
            $"La base '{ruta}' quedó dentro del ContentRoot '{entorno.ContentRootPath}'.");

        if (!string.IsNullOrEmpty(entorno.WebRootPath))
        {
            Assert.False(
                EstaDentroDe(ruta, entorno.WebRootPath),
                $"La base '{ruta}' quedó dentro del WebRoot '{entorno.WebRootPath}'.");
        }

        // M-3: además de que la ruta configurada esté afuera, el árbol de la aplicación no debe
        // contener ningún archivo de base, ni siquiera uno creado por descuido con otro nombre.
        Assert.Empty(BasesDeDatosBajo(entorno.ContentRootPath));
    }

    [Fact]
    public void Crea_El_Directorio_Con_Permisos_Restrictivos()
    {
        if (OperatingSystem.IsWindows())
        {
            // Omitido explícitamente: Windows no tiene modos POSIX y M-9 acota la verificación a
            // las plataformas que los soportan.
            return;
        }

        using var fabrica = new AppFactory();

        _ = fabrica.Services;

        Assert.True(
            Directory.Exists(fabrica.DirectorioDeLaBase),
            $"La aplicación no creó el directorio de la base '{fabrica.DirectorioDeLaBase}'.");

        var permisos = new DirectoryInfo(fabrica.DirectorioDeLaBase).UnixFileMode;

        Assert.Equal(
            UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute,
            permisos);
    }

    [Fact]
    public void Arranque_Falla_Si_Falta_La_Ruta_De_La_Base()
    {
        using var fabrica = AppFactory.SinRutaDeBase();

        var excepcion = ArranqueQueFalla(fabrica);

        // E2.1: el mensaje nombra la clave ausente. No hay valor que pueda filtrar porque no lo hay.
        Assert.Contains(AppFactory.ClaveDeLaRutaDeLaBase, excepcion.ToString(), StringComparison.Ordinal);

        // El arranque abortó sin dejar ninguna base creada en una ubicación por defecto. Se barre
        // el árbol de la aplicación y no el directorio temporal de la fábrica: sin la clave
        // declarada la aplicación nunca supo de ese temporal, así que está vacío por construcción
        // y comprobarlo no descartaría nada. Un valor por defecto tolerante dejaría la base bajo
        // el ContentRoot, que es exactamente lo que este barrido detecta (M-3).
        Assert.Empty(BasesDeDatosBajo(RaizDeLaAplicacion()));
    }

    [Fact]
    public void Arranque_Falla_Si_La_Ruta_Cae_Dentro_Del_Arbol_De_La_Aplicacion()
    {
        var rutaProhibida = Path.Combine(RaizDeLaAplicacion(), "datos-de-prueba", "archivo.db");
        using var fabrica = AppFactory.ConRutaDeBase(rutaProhibida);

        var excepcion = ArranqueQueFalla(fabrica);

        // E2.2: el mensaje nombra la regla incumplida.
        Assert.Contains("dentro del árbol de la aplicación", excepcion.ToString(), StringComparison.Ordinal);

        // La regla se evalúa ANTES de tocar el disco: rechazar después de haber creado el
        // directorio dejaría basura dentro del repositorio en cada arranque fallido.
        Assert.False(
            Directory.Exists(Path.GetDirectoryName(rutaProhibida)),
            $"El arranque creó '{Path.GetDirectoryName(rutaProhibida)}' antes de rechazar la ruta.");
    }

    [Fact]
    public void Arranque_Falla_Si_No_Puede_Crear_El_Directorio()
    {
        using var fabrica = new AppFactory();

        // Un archivo ocupa el nombre que la aplicación necesita para el directorio de la base.
        File.WriteAllText(fabrica.DirectorioDeLaBase, "este archivo ocupa el nombre del directorio");

        var excepcion = ArranqueQueFalla(fabrica);

        // E2.3: la excepción del sistema de archivos se propaga tal cual y aborta el arranque.
        Assert.Contains(CadenaDe(excepcion), interna => interna is IOException);
        Assert.Contains(fabrica.DirectorioDeLaBase, excepcion.ToString(), StringComparison.Ordinal);

        Assert.True(
            File.Exists(fabrica.DirectorioDeLaBase),
            "El arranque reemplazó el archivo que ocupaba el nombre del directorio de la base.");
    }

    [Fact]
    public void Arranque_Falla_Si_La_Migracion_No_Puede_Aplicarse()
    {
        const string marcadorDeContenido = "MarcadorDeContenidoDeLaBase";

        using var fabrica = new AppFactory();
        Directory.CreateDirectory(fabrica.DirectorioDeLaBase);

        // Base preexistente e inconsistente: es un archivo SQLite válido —así que abrirla en WAL
        // funciona— pero ya tiene una tabla `AspNetUsers` ajena al esquema de Identity y ningún
        // historial de migraciones, de modo que la migración inicial no puede aplicarse.
        EjecutarSobreLaBase(
            fabrica.RutaDeLaBase,
            "CREATE TABLE AspNetUsers (Marcador TEXT NOT NULL);",
            $"INSERT INTO AspNetUsers (Marcador) VALUES ('{marcadorDeContenido}');");

        SqliteConnection.ClearAllPools();

        var excepcion = ArranqueQueFalla(fabrica);
        var texto = excepcion.ToString();

        // E2.4: la excepción del motor se propaga y aborta el arranque.
        Assert.Contains(CadenaDe(excepcion), interna => interna is SqliteException);
        Assert.Contains("AspNetUsers", texto, StringComparison.Ordinal);

        // ...y no arrastra contenido de la base, que sería un dato médico en cuanto exista uno.
        Assert.DoesNotContain(marcadorDeContenido, texto, StringComparison.Ordinal);
    }

    /// <summary>
    /// Raíz del árbol de la aplicación. Se obtiene de un host que sí arranca porque un arranque
    /// abortado nunca llega a exponer su entorno.
    /// </summary>
    private static string RaizDeLaAplicacion()
    {
        using var fabricaValida = new AppFactory();

        return fabricaValida.Services.GetRequiredService<IWebHostEnvironment>().ContentRootPath;
    }

    /// <summary>Archivos de base —incluidos los auxiliares `-wal` y `-shm`— bajo una raíz.</summary>
    private static IReadOnlyList<string> BasesDeDatosBajo(string raiz) =>
        Directory.EnumerateFiles(raiz, "*.db*", SearchOption.AllDirectories).ToArray();

    /// <summary>Arranca el host esperando que aborte, y devuelve la excepción que lo abortó.</summary>
    private static Exception ArranqueQueFalla(AppFactory fabrica)
    {
        var excepcion = Record.Exception(() => _ = fabrica.Services);

        Assert.True(
            excepcion is not null,
            "El arranque completó sin lanzar ninguna excepción: se esperaba que abortara.");

        return excepcion!;
    }

    /// <summary>Recorre la excepción y todas sus internas, incluidas las agregadas.</summary>
    private static IEnumerable<Exception> CadenaDe(Exception raiz)
    {
        var pendientes = new Stack<Exception>();
        pendientes.Push(raiz);

        while (pendientes.Count > 0)
        {
            var actual = pendientes.Pop();
            yield return actual;

            if (actual is AggregateException agregada)
            {
                foreach (var interna in agregada.InnerExceptions)
                {
                    pendientes.Push(interna);
                }
            }
            else if (actual.InnerException is not null)
            {
                pendientes.Push(actual.InnerException);
            }
        }
    }

    private static bool EstaDentroDe(string ruta, string raiz)
    {
        var raizNormalizada =
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(raiz)) + Path.DirectorySeparatorChar;

        return Path.GetFullPath(ruta).StartsWith(raizNormalizada, ComparacionDeRutas);
    }

    /// <summary>En macOS y Windows el sistema de archivos no distingue mayúsculas de minúsculas.</summary>
    private static StringComparison ComparacionDeRutas =>
        OperatingSystem.IsLinux() ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

    private static IReadOnlyList<string> TextosDeLaBase(string ruta, string consulta)
    {
        using var conexion = AbrirBaseExistente(ruta);
        using var comando = conexion.CreateCommand();
        comando.CommandText = consulta;

        var textos = new List<string>();
        using var lector = comando.ExecuteReader();
        while (lector.Read())
        {
            textos.Add(lector.GetString(0));
        }

        return textos;
    }

    private static object? EscalarDeLaBase(string ruta, string consulta)
    {
        using var conexion = AbrirBaseExistente(ruta);
        using var comando = conexion.CreateCommand();
        comando.CommandText = consulta;

        return comando.ExecuteScalar();
    }

    private static void EjecutarSobreLaBase(string ruta, params string[] sentencias)
    {
        using var conexion = new SqliteConnection(
            new SqliteConnectionStringBuilder { DataSource = ruta }.ToString());

        conexion.Open();

        foreach (var sentencia in sentencias)
        {
            using var comando = conexion.CreateCommand();
            comando.CommandText = sentencia;
            comando.ExecuteNonQuery();
        }
    }

    /// <summary>Abre la base sin crearla: si el archivo no existe, el test debe fallar, no inventarla.</summary>
    private static SqliteConnection AbrirBaseExistente(string ruta)
    {
        var conexion = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = ruta,
            Mode = SqliteOpenMode.ReadWrite,
        }.ToString());

        conexion.Open();
        return conexion;
    }
}
