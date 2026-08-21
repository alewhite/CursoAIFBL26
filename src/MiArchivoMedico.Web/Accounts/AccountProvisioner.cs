using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MiArchivoMedico.Web.Accounts;

/// <summary>
/// Aplica al arrancar las altas declaradas en la configuración externa (FR-01, FR-02, FR-03,
/// ADR-003). La siembra es estrictamente aditiva: una cuenta que ya existe no se toca, ni su
/// contraseña ni ninguna otra propiedad, porque reiniciar el proceso no puede ser una forma de
/// cambiar credenciales. Ningún rechazo aborta el arranque (AC-03, AC-04).
/// </summary>
public sealed class AccountProvisioner(
    UserManager<AppUser> cuentas,
    IOptions<AccountSeedOptions> altasDeclaradas,
    ILogger<AccountProvisioner> registro)
{
    /// <summary>
    /// Máximo de cuentas activas admitidas (NFR-02). "Activa" es, en este alcance, una fila
    /// existente en el almacén de cuentas: no hay noción de baja ni de estado.
    /// </summary>
    public const int MaximoDeCuentasActivas = 5;

    /// <summary>
    /// Mínimo de caracteres de la contraseña (NFR-03). Lo aplica <c>PasswordOptions</c> a través de
    /// <see cref="UserManager{TUser}"/>; esta constante existe para configurarlo en un solo lugar,
    /// no para comprobarlo por separado acá.
    /// </summary>
    public const int LongitudMinimaDeLaContrasena = 12;

    /// <summary>Iteraciones de PBKDF2 del hash de contraseñas (NFR-01, ADR-002).</summary>
    public const int IteracionesDePbkdf2 = 100_000;

    /// <summary>Máximo de caracteres del nombre de usuario declarado.</summary>
    private const int LongitudMaximaDelNombreDeUsuario = 256;

    /// <summary>Máximo de caracteres de la contraseña declarada.</summary>
    private const int LongitudMaximaDeLaContrasena = 128;

    /// <summary>
    /// Recorre las altas declaradas y aplica las que correspondan. Se ejecuta después de las
    /// migraciones: escribe sobre el esquema de Identity, que tiene que existir.
    /// </summary>
    public async Task SembrarAsync(CancellationToken cancelacion = default)
    {
        var declaradas = altasDeclaradas.Value.Accounts;

        for (var indice = 0; indice < declaradas.Count; indice++)
        {
            await AplicarAsync(indice, declaradas[indice], cancelacion);
        }
    }

    private async Task AplicarAsync(int indice, AccountSeedEntry entrada, CancellationToken cancelacion)
    {
        var nombreDeUsuario = entrada.UserName?.Trim();

        // E3.3: sin nombre no hay cuenta que nombrar en el rechazo, así que se identifica la
        // entrada por su posición en la lista declarada.
        if (string.IsNullOrEmpty(nombreDeUsuario) || nombreDeUsuario.Length > LongitudMaximaDelNombreDeUsuario)
        {
            registro.LogWarning(
                "Alta rechazada en la entrada {Indice} de la configuración: el nombre de usuario es obligatorio y admite entre 1 y {Maximo} caracteres.",
                indice,
                LongitudMaximaDelNombreDeUsuario);

            return;
        }

        if (string.IsNullOrEmpty(entrada.Password) || entrada.Password.Length > LongitudMaximaDeLaContrasena)
        {
            // El mínimo no se comprueba acá: lo aplica `PasswordOptions.RequiredLength` al crear.
            registro.LogWarning(
                "Alta rechazada para la cuenta '{Cuenta}': la contraseña es obligatoria y no puede superar los {Maximo} caracteres.",
                nombreDeUsuario,
                LongitudMaximaDeLaContrasena);

            return;
        }

        // E3.5: la cuenta existente se omite sin compararle la contraseña ni tocarle una propiedad
        // (NFR-07). Comparar ya sería leer la contraseña declarada contra el hash almacenado, y de
        // ahí a "corregir" la diferencia hay un solo paso.
        if (await cuentas.FindByNameAsync(nombreDeUsuario) is not null)
        {
            registro.LogInformation(
                "La cuenta '{Cuenta}' ya existe: la siembra no la modifica.",
                nombreDeUsuario);

            return;
        }

        // E3.2: se cuenta lo persistido, no lo declarado. Contar la lista de la configuración
        // dejaría que dos arranques sucesivos, con listas distintas, superaran el límite.
        var cuentasPersistidas = await cuentas.Users.CountAsync(cancelacion);

        if (cuentasPersistidas >= MaximoDeCuentasActivas)
        {
            registro.LogWarning(
                "Alta rechazada para la cuenta '{Cuenta}': ya hay {Persistidas} cuentas activas y el máximo admitido es {Maximo}.",
                nombreDeUsuario,
                cuentasPersistidas,
                MaximoDeCuentasActivas);

            return;
        }

        var resultado = await cuentas.CreateAsync(
            new AppUser { UserName = nombreDeUsuario },
            entrada.Password);

        if (resultado.Succeeded)
        {
            registro.LogInformation(
                "Cuenta '{Cuenta}' creada por la siembra de arranque.",
                nombreDeUsuario);

            return;
        }

        // E3.1 y E3.4: se registran los códigos que devuelve Identity —`PasswordTooShort`,
        // `InvalidUserName`, `DuplicateUserName`…— y nunca la descripción completa, que en algunos
        // casos repite datos de la entrada, ni por supuesto la contraseña (AC-08, NFR-05).
        registro.LogWarning(
            "Alta rechazada para la cuenta '{Cuenta}': {Reglas}.",
            nombreDeUsuario,
            string.Join(", ", resultado.Errors.Select(error => error.Code)));
    }
}
