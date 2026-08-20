using Microsoft.Data.Sqlite;

namespace MiArchivoMedico.Web.Data;

/// <summary>
/// Resuelve, valida y prepara la ubicación del archivo de base SQLite (NFR-04, AC-06, M-3, M-9).
/// La ruta llega siempre por configuración externa: la aplicación no define ninguna por defecto.
/// </summary>
public sealed class SqliteLocation
{
    /// <summary>Clave de configuración externa que declara la ruta del archivo de base.</summary>
    public const string ClaveDeConfiguracion = "MiArchivoMedico:Database:Path";

    /// <summary>Permisos del directorio de la base en plataformas POSIX (M-9).</summary>
    private const UnixFileMode PermisosDelDirectorio =
        UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;

    private SqliteLocation(string cadenaDeConexion)
    {
        CadenaDeConexion = cadenaDeConexion;
    }

    /// <summary>Cadena de conexión hacia el archivo de base. No contiene secretos.</summary>
    public string CadenaDeConexion { get; }

    /// <summary>
    /// Resuelve la ubicación declarada en la configuración, la valida y deja el directorio creado.
    /// Aborta el arranque si la clave falta (E2.1), si la ruta cae dentro del árbol de la
    /// aplicación (E2.2) o si el directorio no puede crearse (E2.3).
    /// </summary>
    public static SqliteLocation Resolver(IConfiguration configuracion, IWebHostEnvironment entorno)
    {
        var valorDeclarado = configuracion[ClaveDeConfiguracion];

        // E2.1: sin valor por defecto. Uno tolerante dejaría la base bajo el ContentRoot y además
        // instalaría el antipatrón contrario al que exigirá la clave de cifrado cuando llegue.
        if (string.IsNullOrWhiteSpace(valorDeclarado))
        {
            throw new InvalidOperationException(
                $"Falta la clave de configuración '{ClaveDeConfiguracion}', que declara la ruta " +
                "del archivo de base de datos. La aplicación no define ninguna ruta por defecto.");
        }

        var rutaDelArchivo = Path.GetFullPath(valorDeclarado.Trim());

        // Una ruta sin directorio solo puede ser la raíz del sistema de archivos: se resuelve
        // contra sí misma, no hay nada que crear y el motor rechazará abrirla como base.
        var directorio = Path.GetDirectoryName(rutaDelArchivo) ?? rutaDelArchivo;

        // E2.2: se valida ANTES de tocar el disco. Rechazar después de haber creado el directorio
        // dejaría basura dentro del árbol de la aplicación en cada arranque fallido.
        RechazarSiEstaDentroDelArbol(rutaDelArchivo, entorno.ContentRootPath, "ContentRoot");
        RechazarSiEstaDentroDelArbol(rutaDelArchivo, entorno.WebRootPath, "WebRoot");

        PrepararDirectorio(directorio);

        var cadenaDeConexion = new SqliteConnectionStringBuilder
        {
            DataSource = rutaDelArchivo,
        }.ToString();

        return new SqliteLocation(cadenaDeConexion);
    }

    /// <summary>
    /// Deja la base operando en modo WAL (AC-06). El modo de journal se persiste en el encabezado
    /// del archivo, así que fijarlo una vez al arrancar alcanza para todas las conexiones.
    /// </summary>
    public void PrepararEnModoWal()
    {
        using var conexion = new SqliteConnection(CadenaDeConexion);
        conexion.Open();

        using var comando = conexion.CreateCommand();
        comando.CommandText = "PRAGMA journal_mode=WAL;";
        var modo = comando.ExecuteScalar() as string;

        if (!string.Equals(modo, "wal", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"La base de datos no pudo abrirse en modo WAL: el motor informó '{modo}'.");
        }
    }

    private static void RechazarSiEstaDentroDelArbol(string rutaDelArchivo, string? raiz, string nombreDeLaRaiz)
    {
        if (string.IsNullOrEmpty(raiz) || !EstaDentroDe(rutaDelArchivo, raiz))
        {
            return;
        }

        throw new InvalidOperationException(
            $"La ruta '{rutaDelArchivo}' declarada en '{ClaveDeConfiguracion}' cae dentro del " +
            $"árbol de la aplicación ({nombreDeLaRaiz}: '{Path.GetFullPath(raiz)}'). La base de " +
            "datos debe residir fuera del repositorio y de toda carpeta pública del servidor.");
    }

    private static bool EstaDentroDe(string ruta, string raiz)
    {
        // Comparación textual sobre rutas absolutas: no resuelve enlaces simbólicos. Quedan fuera
        // del alcance de esta regla porque la ruta la declara por configuración el administrador
        // técnico, que es un actor de confianza —no hay entrada de usuario final acá—, y porque un
        // enlace que igual dejara una base dentro del repositorio ya está cubierto por dos redes
        // aparte: `.gitignore` impide versionar `*.db*` y el barrido de los tests falla si aparece
        // cualquier archivo de base bajo el árbol de la aplicación (M-3).
        var raizNormalizada =
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(raiz)) + Path.DirectorySeparatorChar;

        return ruta.StartsWith(raizNormalizada, ComparacionDeRutas);
    }

    /// <summary>En macOS y Windows el sistema de archivos no distingue mayúsculas de minúsculas.</summary>
    private static StringComparison ComparacionDeRutas =>
        OperatingSystem.IsLinux() ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

    private static void PrepararDirectorio(string directorio)
    {
        if (Directory.Exists(directorio))
        {
            return;
        }

        // E2.3: si no puede crearse, la excepción del sistema de archivos se propaga y aborta el
        // arranque.
        Directory.CreateDirectory(directorio);

        if (!OperatingSystem.IsWindows())
        {
            // M-9: se fija el modo después de crear el directorio porque el `umask` del proceso
            // recorta los permisos que recibe `mkdir`, y acá el 0700 tiene que ser exacto.
            // Cubre solo el directorio que crea la aplicación: uno preexistente conserva los
            // permisos que le haya dado el operador, porque el retorno temprano de arriba ni
            // siquiera llega hasta acá. Endurecer un directorio ajeno al arrancar sería cambiar
            // permisos que la aplicación no eligió; asegurarlo es parte del despliegue.
            File.SetUnixFileMode(directorio, PermisosDelDirectorio);
        }
    }
}
