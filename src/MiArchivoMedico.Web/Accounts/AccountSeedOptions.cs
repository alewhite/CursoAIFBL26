namespace MiArchivoMedico.Web.Accounts;

/// <summary>
/// Altas declaradas por el administrador técnico en la configuración externa (ADR-003). Su origen
/// admitido son variables de entorno o user-secrets: nunca un archivo versionado, porque contiene
/// contraseñas en claro.
/// </summary>
public sealed class AccountSeedOptions
{
    /// <summary>
    /// Sección raíz de la configuración propia de la aplicación. Es la misma que usa la ubicación
    /// de la base (<c>MiArchivoMedico:Database:Path</c>); se enlaza esta sección —y no la lista
    /// directamente— porque es lo que permite que <see cref="Accounts"/> quede poblada por el
    /// enlazador estándar de opciones.
    /// </summary>
    public const string SeccionDeLaAplicacion = "MiArchivoMedico";

    /// <summary>
    /// Clave completa bajo la que el entorno declara la lista de altas, en el formato
    /// <c>MiArchivoMedico:Accounts:0:UserName</c>.
    /// </summary>
    public const string ClaveDeConfiguracion = SeccionDeLaAplicacion + ":" + nameof(Accounts);

    /// <summary>
    /// Altas declaradas, en el orden en que las escribió el administrador técnico. La lista puede
    /// estar vacía: un entorno del que ya se retiraron las contraseñas arranca igual y conserva las
    /// cuentas creadas antes (ADR-003, M-13).
    /// </summary>
    public List<AccountSeedEntry> Accounts { get; set; } = [];
}

/// <summary>
/// Una cuenta declarada en la configuración externa: el nombre de usuario y la contraseña inicial
/// con la que quedará disponible para autenticarse.
/// </summary>
public sealed class AccountSeedEntry
{
    /// <summary>Nombre de usuario declarado. Requerido.</summary>
    public string? UserName { get; set; }

    /// <summary>Contraseña inicial declarada. Requerida; nunca se registra ni se expone.</summary>
    public string? Password { get; set; }

    /// <summary>
    /// Representación textual sin la contraseña (NFR-05, AC-08). Es lo que imprimirían un registro
    /// estructurado o un volcado de diagnóstico que reciban la entrada completa. Se escribe
    /// explícitamente aunque el <c>ToString()</c> de <c>object</c> no filtre nada: el día que esta
    /// clase pase a ser un <c>record</c>, el generado imprimiría TODAS las propiedades —contraseña
    /// incluida— y la filtración entraría sin que nadie la escribiera.
    /// </summary>
    public override string ToString() =>
        $"{nameof(AccountSeedEntry)} {{ {nameof(UserName)} = {UserName} }}";
}
