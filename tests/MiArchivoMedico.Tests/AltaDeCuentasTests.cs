using System.Buffers.Binary;
using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MiArchivoMedico.Web.Accounts;
using MiArchivoMedico.Web.Data;
using Xunit;

namespace MiArchivoMedico.Tests;

/// <summary>
/// Tests del alta administrativa de cuentas (Bloque 3 de FEAT-001a): siembra aditiva desde
/// configuración externa, límite de cuentas activas, mínimo de contraseña, parámetros del hash y
/// ausencia de credenciales en los registros técnicos.
/// </summary>
public class AltaDeCuentasTests
{
    [Fact]
    public async Task Siembra_Crea_Las_Cuentas_Declaradas_Que_No_Existen()
    {
        var ana = ConfiguracionDeAltas.Alta("ana");
        var bruno = ConfiguracionDeAltas.Alta("bruno");

        using var fabrica = new AppFactory();
        using var arranque = ConfiguracionDeAltas.Arranque(fabrica, ana, bruno);

        var cuentas = ConfiguracionDeAltas.Cuentas(arranque.Services);

        Assert.Equal(new[] { ana.UserName, bruno.UserName }, cuentas.Select(c => c.UserName).ToArray());

        foreach (var alta in new[] { ana, bruno })
        {
            // AC-01 pide que queden "disponibles para autenticación": comprobar que la fila existe
            // no lo prueba: una cuenta creada con la contraseña equivocada también existiría.
            Assert.True(
                await ConfiguracionDeAltas.VerificaLaContrasenaAsync(arranque.Services, alta),
                $"La cuenta '{alta.UserName}' no valida la contraseña declarada para ella.");
        }

        foreach (var cuenta in cuentas)
        {
            Assert.NotNull(cuenta.PasswordHash);
            Assert.DoesNotContain(
                ConfiguracionDeAltas.ContrasenaDe(cuenta.UserName!),
                cuenta.PasswordHash!,
                StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task Rechaza_El_Alta_Que_Supera_El_Maximo_De_Cuentas()
    {
        var admitidas = Enumerable
            .Range(1, AccountProvisioner.MaximoDeCuentasActivas)
            .Select(numero => ConfiguracionDeAltas.Alta(
                string.Create(CultureInfo.InvariantCulture, $"cuenta{numero}")))
            .ToArray();

        var excedente = ConfiguracionDeAltas.Alta("excedente");

        using var fabrica = new AppFactory();
        var registro = new RegistroEnMemoria();
        using var arranque = ConfiguracionDeAltas.ArranqueConRegistro(
            fabrica, registro, admitidas.Append(excedente).ToArray());

        var cuentas = ConfiguracionDeAltas.Cuentas(arranque.Services);

        Assert.Equal(
            admitidas.Select(alta => alta.UserName).ToArray(),
            cuentas.Select(cuenta => cuenta.UserName).ToArray());

        // FR-02: el rechazo informa el límite alcanzado, nombrando la cuenta y el máximo.
        var rechazo = Assert.Single(
            registro.Mensajes,
            mensaje => mensaje.Contains(excedente.UserName, StringComparison.Ordinal));

        Assert.Contains(
            AccountProvisioner.MaximoDeCuentasActivas.ToString(CultureInfo.InvariantCulture),
            rechazo,
            StringComparison.Ordinal);

        // AC-03: el arranque completa igual. Que la aplicación conteste lo prueba; que el
        // proveedor de servicios se resuelva, no: eso ya ocurrió al leer las cuentas.
        // FEAT-001b instala una FallbackPolicy que exige usuario autenticado: la ruta `/` está
        // mapeada y sin sesión responde 302 hacia el login ANTES de autorizar. Se pide no-redirect
        // para observar el 302 (el `CreateClient` por defecto lo seguiría y enmascararía).
        using var cliente = arranque.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        Assert.Equal(HttpStatusCode.Redirect, (await cliente.GetAsync("/")).StatusCode);
    }

    [Fact]
    public async Task Rechaza_El_Alta_Con_Contrasena_Menor_A_12_Caracteres()
    {
        // Once caracteres: uno menos que el mínimo de NFR-03.
        var corta = new AltaDeclarada("carla", "0123456789A");
        var valida = ConfiguracionDeAltas.Alta("diego");

        Assert.Equal(AccountProvisioner.LongitudMinimaDeLaContrasena - 1, corta.Password.Length);

        using var fabrica = new AppFactory();
        var registro = new RegistroEnMemoria();
        using var arranque = ConfiguracionDeAltas.ArranqueConRegistro(fabrica, registro, corta, valida);

        var cuenta = Assert.Single(ConfiguracionDeAltas.Cuentas(arranque.Services));
        Assert.Equal(valida.UserName, cuenta.UserName);

        // AC-04: se informa el incumplimiento del mínimo, con la cuenta y la regla, sin la
        // contraseña. `PasswordTooShort` es el código que devuelve el validador de Identity, que
        // es quien aplica el mínimo: si alguien lo reemplazara por un `if` propio, este código
        // dejaría de aparecer.
        var rechazo = Assert.Single(
            registro.Mensajes,
            mensaje => mensaje.Contains(corta.UserName, StringComparison.Ordinal));

        Assert.Contains("PasswordTooShort", rechazo, StringComparison.Ordinal);
        Assert.DoesNotContain(corta.Password, rechazo, StringComparison.Ordinal);

        // FEAT-001b: la FallbackPolicy exige sesión; sin una, `/` responde 302 al login. Se pide
        // no-redirect para observar el 302.
        using var cliente = arranque.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        Assert.Equal(HttpStatusCode.Redirect, (await cliente.GetAsync("/")).StatusCode);
    }

    [Fact]
    public void Hash_Almacenado_Usa_Pbkdf2_Con_Al_Menos_100000_Iteraciones()
    {
        var elena = ConfiguracionDeAltas.Alta("elena");

        using var fabrica = new AppFactory();
        using var arranque = ConfiguracionDeAltas.Arranque(fabrica, elena);

        var cuenta = Assert.Single(ConfiguracionDeAltas.Cuentas(arranque.Services));

        Assert.NotNull(cuenta.PasswordHash);
        Assert.NotEqual(elena.Password, cuenta.PasswordHash);

        // Formato IdentityV3: 0x01 | prf (4) | iteraciones (4) | longitud de sal (4) | sal | clave.
        var almacenado = Convert.FromBase64String(cuenta.PasswordHash!);

        Assert.Equal(0x01, almacenado[0]);

        var prf = BinaryPrimitives.ReadUInt32BigEndian(almacenado.AsSpan(1, 4));
        var iteraciones = BinaryPrimitives.ReadUInt32BigEndian(almacenado.AsSpan(5, 4));
        var longitudDeLaSal = BinaryPrimitives.ReadUInt32BigEndian(almacenado.AsSpan(9, 4));

        // 0 es HMAC-SHA1, que NFR-01 no admite; 1 es SHA256 y 2 es SHA512.
        Assert.True(
            prf is 1 or 2,
            $"El hash declara la función pseudoaleatoria {prf}, fuera de la familia SHA-2 que exige NFR-01.");

        Assert.True(
            iteraciones >= 100_000,
            $"El hash declara {iteraciones} iteraciones, por debajo de las 100.000 que exige NFR-01.");

        Assert.Equal((uint)AccountProvisioner.IteracionesDePbkdf2, iteraciones);

        Assert.Equal(128u / 8u, longitudDeLaSal);
        Assert.Equal(13 + longitudDeLaSal + (256 / 8), (uint)almacenado.Length);
    }

    [Fact]
    public void Segundo_Arranque_No_Duplica_Cuentas_Ni_Cambia_Sus_Credenciales()
    {
        var altas = new[] { ConfiguracionDeAltas.Alta("ana"), ConfiguracionDeAltas.Alta("bruno") };

        using var fabrica = new AppFactory();

        IReadOnlyList<AppUser> primerArranque;
        using (var primero = ConfiguracionDeAltas.Arranque(fabrica, altas))
        {
            primerArranque = ConfiguracionDeAltas.Cuentas(primero.Services);
        }

        // El segundo host apunta al MISMO archivo de base: la fábrica delegada hereda la ruta.
        using var segundo = ConfiguracionDeAltas.Arranque(fabrica, altas);
        var segundoArranque = ConfiguracionDeAltas.Cuentas(segundo.Services);

        Assert.Equal(2, primerArranque.Count);

        Assert.Equal(
            primerArranque.Select(cuenta => cuenta.UserName).ToArray(),
            segundoArranque.Select(cuenta => cuenta.UserName).ToArray());

        // NFR-07: ni el hash, ni el identificador, ni el sello de seguridad se reescriben.
        Assert.Equal(
            primerArranque.Select(cuenta => cuenta.PasswordHash).ToArray(),
            segundoArranque.Select(cuenta => cuenta.PasswordHash).ToArray());

        Assert.Equal(
            primerArranque.Select(cuenta => cuenta.Id).ToArray(),
            segundoArranque.Select(cuenta => cuenta.Id).ToArray());

        Assert.Equal(
            primerArranque.Select(cuenta => cuenta.SecurityStamp).ToArray(),
            segundoArranque.Select(cuenta => cuenta.SecurityStamp).ToArray());
    }

    [Fact]
    public async Task Arranque_Con_Configuracion_De_Altas_Retirada_Conserva_Las_Cuentas()
    {
        var ana = ConfiguracionDeAltas.Alta("ana");

        using var fabrica = new AppFactory();

        IReadOnlyList<AppUser> conConfiguracion;
        using (var primero = ConfiguracionDeAltas.Arranque(fabrica, ana))
        {
            conConfiguracion = ConfiguracionDeAltas.Cuentas(primero.Services);
        }

        Assert.Single(conConfiguracion);

        // M-13: la fábrica base no declara ninguna alta, así que este arranque es el de un entorno
        // del que ya se retiraron las contraseñas en claro.
        var sinConfiguracion = ConfiguracionDeAltas.Cuentas(fabrica.Services);

        Assert.Equal(
            conConfiguracion.Select(cuenta => cuenta.UserName).ToArray(),
            sinConfiguracion.Select(cuenta => cuenta.UserName).ToArray());

        Assert.Equal(
            conConfiguracion.Select(cuenta => cuenta.PasswordHash).ToArray(),
            sinConfiguracion.Select(cuenta => cuenta.PasswordHash).ToArray());

        Assert.True(
            await ConfiguracionDeAltas.VerificaLaContrasenaAsync(fabrica.Services, ana),
            "La cuenta dejó de validar su contraseña tras arrancar sin la configuración de altas.");

        // FEAT-001b: la FallbackPolicy exige sesión; sin una, `/` responde 302 al login. Se pide
        // no-redirect para observar el 302.
        using var cliente = fabrica.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        Assert.Equal(HttpStatusCode.Redirect, (await cliente.GetAsync("/")).StatusCode);
    }

    [Fact]
    public void Rechazo_De_Alta_No_Registra_Credenciales()
    {
        var valida = ConfiguracionDeAltas.Alta("ana");

        // E3.4: `IdentityResult` fallido por una causa distinta del mínimo de contraseña. El
        // espacio no pertenece al juego de caracteres admitidos para el nombre de usuario.
        var nombreInvalido = new AltaDeclarada("ana bruno", ConfiguracionDeAltas.ContrasenaDe("ana bruno"));
        var contrasenaCorta = new AltaDeclarada("bruno", "0123456789A");

        using var fabrica = new AppFactory();
        var registro = new RegistroEnMemoria();
        using var arranque = ConfiguracionDeAltas.ArranqueConRegistro(
            fabrica, registro, valida, nombreInvalido, contrasenaCorta);

        var cuenta = Assert.Single(ConfiguracionDeAltas.Cuentas(arranque.Services));
        Assert.Equal(valida.UserName, cuenta.UserName);

        Assert.NotNull(cuenta.PasswordHash);

        var mensajes = registro.Mensajes;

        // Aserciones positivas: sin ellas, un arranque que no registrara absolutamente nada
        // satisfaría todos los `DoesNotContain` de abajo. M-8 exige que el rechazo quede anotado.
        Assert.Contains(
            mensajes,
            mensaje => mensaje.Contains(nombreInvalido.UserName, StringComparison.Ordinal)
                && mensaje.Contains("InvalidUserName", StringComparison.Ordinal));

        Assert.Contains(
            mensajes,
            mensaje => mensaje.Contains(contrasenaCorta.UserName, StringComparison.Ordinal)
                && mensaje.Contains("PasswordTooShort", StringComparison.Ordinal));

        // AC-08 y NFR-05: ni la contraseña declarada ni el hash almacenado, en ningún mensaje de
        // ninguna categoría.
        foreach (var mensaje in mensajes)
        {
            foreach (var declarada in new[] { valida, nombreInvalido, contrasenaCorta })
            {
                Assert.DoesNotContain(declarada.Password, mensaje, StringComparison.Ordinal);
            }

            Assert.DoesNotContain(cuenta.PasswordHash!, mensaje, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Rechaza_Entrada_Con_Nombre_De_Usuario_Vacio()
    {
        var vacia = new AltaDeclarada("   ", ConfiguracionDeAltas.ContrasenaDe("sin-nombre"));
        var valida = ConfiguracionDeAltas.Alta("felipe");

        using var fabrica = new AppFactory();
        var registro = new RegistroEnMemoria();
        using var arranque = ConfiguracionDeAltas.ArranqueConRegistro(fabrica, registro, vacia, valida);

        // E3.3: la entrada inválida se rechaza y las demás se procesan igual.
        var cuenta = Assert.Single(ConfiguracionDeAltas.Cuentas(arranque.Services));
        Assert.Equal(valida.UserName, cuenta.UserName);

        // El rechazo se identifica por la posición en la lista declarada: no hay nombre que nombrar.
        var rechazo = Assert.Single(
            registro.Mensajes,
            mensaje => mensaje.Contains("entrada 0", StringComparison.Ordinal));

        Assert.Contains("nombre de usuario", rechazo, StringComparison.Ordinal);
        Assert.DoesNotContain(vacia.Password, rechazo, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Rechaza_Entrada_Sin_La_Clave_De_Nombre_De_Usuario()
    {
        var valida = ConfiguracionDeAltas.Alta("mariana");
        var huerfana = ConfiguracionDeAltas.ContrasenaDe("sin-clave-de-nombre");

        using var fabrica = new AppFactory();
        var registro = new RegistroEnMemoria();

        // Entrada 0: la clave `UserName` no se declara en absoluto. Es una forma distinta de la que
        // cubre `Rechaza_Entrada_Con_Nombre_De_Usuario_Vacio`, que sí la declara —con espacios—: lo
        // que se fija acá es el contrato con el enlazador de configuración, que una entrada
        // declarada solo con `Password` materialice igual un `AccountSeedEntry` con el nombre nulo.
        using var arranque = ConfiguracionDeAltas.ArranqueConClaves(
            fabrica,
            registro,
            ConfiguracionDeAltas.Clave(0, nameof(AccountSeedEntry.Password), huerfana),
            ConfiguracionDeAltas.Clave(1, nameof(AccountSeedEntry.UserName), valida.UserName),
            ConfiguracionDeAltas.Clave(1, nameof(AccountSeedEntry.Password), valida.Password));

        // E3.3: la entrada sin nombre no crea cuenta y la siguiente se procesa igual.
        var cuenta = Assert.Single(ConfiguracionDeAltas.Cuentas(arranque.Services));
        Assert.Equal(valida.UserName, cuenta.UserName);

        // Sin nombre que nombrar, el rechazo identifica la entrada por su posición.
        var rechazo = Assert.Single(
            registro.Mensajes,
            mensaje => mensaje.Contains("entrada 0", StringComparison.Ordinal));

        Assert.Contains("nombre de usuario", rechazo, StringComparison.Ordinal);

        // NFR-05: la contraseña de la entrada rechazada no se arrastra a ningún mensaje, de
        // ninguna categoría.
        foreach (var mensaje in registro.Mensajes)
        {
            Assert.DoesNotContain(huerfana, mensaje, StringComparison.Ordinal);
        }

        // AC-03: el rechazo no aborta el arranque. Que la aplicación conteste lo prueba.
        // FEAT-001b: la FallbackPolicy exige sesión; sin una, `/` responde 302 al login. Se pide
        // no-redirect para observar el 302.
        using var cliente = arranque.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        Assert.Equal(HttpStatusCode.Redirect, (await cliente.GetAsync("/")).StatusCode);
    }

    [Fact]
    public void Appsettings_Versionado_No_Contiene_Contrasenas()
    {
        var ruta = Path.Combine(
            Repositorio.RaizDelRepositorio(), "src", "MiArchivoMedico.Web", "appsettings.json");

        Assert.True(File.Exists(ruta), $"No se encontró el appsettings.json versionado en '{ruta}'.");

        using var documento = JsonDocument.Parse(File.ReadAllText(ruta));
        var declaradas = Aplanar(documento.RootElement, prefijo: string.Empty).ToArray();
        var claves = declaradas.Select(par => par.Clave).ToArray();

        // Positiva: confirma que se leyó el archivo esperado. Sin ella, un archivo vacío —o una
        // ruta equivocada que devolviera `{}`— pasaría todas las comprobaciones de abajo.
        Assert.Contains("Logging:LogLevel:Default", claves);
        Assert.Contains("AllowedHosts", claves);

        foreach (var prohibida in new[]
                 {
                     "password", "contrasena", "contraseña", "pwd", "secret", "connectionstring",
                 })
        {
            Assert.DoesNotContain(
                claves,
                clave => clave.Contains(prohibida, StringComparison.OrdinalIgnoreCase));
        }

        // M-2: las altas llegan por variables de entorno o user-secrets, nunca por un archivo
        // versionado.
        Assert.DoesNotContain(
            claves,
            clave => clave.StartsWith(AccountSeedOptions.ClaveDeConfiguracion, StringComparison.OrdinalIgnoreCase));

        // Y tampoco la ruta de la base: los tests la declaran con `UseSetting`, que escribe en la
        // configuración del host, y una clave homónima en appsettings.json la sobrescribiría. La
        // suite dejaría de escribir en el temporal del sistema operativo sin que nada avisara.
        Assert.DoesNotContain(
            claves,
            clave => clave.Equals(SqliteLocation.ClaveDeConfiguracion, StringComparison.OrdinalIgnoreCase));

        foreach (var (clave, valor) in declaradas)
        {
            foreach (var pinta in new[] { "Data Source=", "Password=", "Pwd=" })
            {
                Assert.False(
                    valor.Contains(pinta, StringComparison.OrdinalIgnoreCase),
                    $"La clave '{clave}' del appsettings.json versionado declara un valor con pinta de credencial.");
            }
        }
    }

    [Fact]
    public void ToString_De_La_Entrada_No_Expone_La_Contrasena()
    {
        const string usuario = "gabriela";
        const string contrasena = "contrasena-reconocible-de-gabriela-2026";

        var entrada = new AccountSeedEntry { UserName = usuario, Password = contrasena };
        var textual = entrada.ToString();

        // NFR-05 y M-1: esto es lo que imprimirían un registro estructurado o un volcado de
        // diagnóstico que reciban la entrada completa. El valor entero del `ToString()` escrito a
        // mano está en el escenario futuro: si la clase pasara a ser un `record`, el generado
        // imprimiría TODAS las propiedades —contraseña incluida— y este test se pondría rojo.
        Assert.DoesNotContain(contrasena, textual, StringComparison.Ordinal);

        // Positiva: sin ella, devolver la cadena vacía —o borrar el `ToString()` sin más—
        // satisfaría la comprobación de arriba y el guardián podría desaparecer en verde.
        Assert.Contains(usuario, textual, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Rechaza_Entrada_Sin_Contrasena()
    {
        var valida = ConfiguracionDeAltas.Alta("ivan");

        using var fabrica = new AppFactory();
        var registro = new RegistroEnMemoria();

        // Entrada 0: la clave `Password` no se declara (contraseña ausente). Entrada 1: la clave
        // existe pero llega vacía. Ambas formas las rechaza la misma rama de validación.
        using var arranque = ConfiguracionDeAltas.ArranqueConClaves(
            fabrica,
            registro,
            ConfiguracionDeAltas.Clave(0, nameof(AccountSeedEntry.UserName), "gabriela"),
            ConfiguracionDeAltas.Clave(1, nameof(AccountSeedEntry.UserName), "hugo"),
            ConfiguracionDeAltas.Clave(1, nameof(AccountSeedEntry.Password), string.Empty),
            ConfiguracionDeAltas.Clave(2, nameof(AccountSeedEntry.UserName), valida.UserName),
            ConfiguracionDeAltas.Clave(2, nameof(AccountSeedEntry.Password), valida.Password));

        // La contraseña es requerida: ninguna de las dos entradas llega a crear cuenta, y la
        // siguiente entrada válida se procesa igual.
        var cuenta = Assert.Single(ConfiguracionDeAltas.Cuentas(arranque.Services));
        Assert.Equal(valida.UserName, cuenta.UserName);

        foreach (var rechazada in new[] { "gabriela", "hugo" })
        {
            var rechazo = Assert.Single(
                registro.Mensajes,
                mensaje => mensaje.Contains(rechazada, StringComparison.Ordinal));

            // El rechazo nombra la entrada y la regla evaluada; el máximo declarado en la spec
            // forma parte del mensaje, que es la superficie donde NFR-05 puede romperse.
            Assert.Contains("contraseña", rechazo, StringComparison.Ordinal);
            Assert.Contains("128", rechazo, StringComparison.Ordinal);
        }

        // AC-03: los rechazos no abortan el arranque. Que la aplicación conteste lo prueba.
        // FEAT-001b: la FallbackPolicy exige sesión; sin una, `/` responde 302 al login. Se pide
        // no-redirect para observar el 302.
        using var cliente = arranque.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        Assert.Equal(HttpStatusCode.Redirect, (await cliente.GetAsync("/")).StatusCode);
    }

    [Fact]
    public async Task Rechaza_Entrada_Con_Contrasena_Mayor_A_128_Caracteres()
    {
        // 129 caracteres: uno más que el máximo que la spec admite para la contraseña declarada.
        var excesiva = new AltaDeclarada("julieta", "contrasena-excesiva-de-julieta-".PadRight(129, 'z'));
        var valida = ConfiguracionDeAltas.Alta("karina");

        Assert.Equal(129, excesiva.Password.Length);

        using var fabrica = new AppFactory();
        var registro = new RegistroEnMemoria();
        using var arranque = ConfiguracionDeAltas.ArranqueConRegistro(fabrica, registro, excesiva, valida);

        var cuenta = Assert.Single(ConfiguracionDeAltas.Cuentas(arranque.Services));
        Assert.Equal(valida.UserName, cuenta.UserName);

        var rechazo = Assert.Single(
            registro.Mensajes,
            mensaje => mensaje.Contains(excesiva.UserName, StringComparison.Ordinal));

        Assert.Contains("128", rechazo, StringComparison.Ordinal);

        // El rechazo lo decide el provisionador, no Identity: 129 caracteres superan de sobra el
        // mínimo de NFR-03 y `CreateAsync` habría aceptado la cuenta.
        Assert.DoesNotContain("PasswordTooShort", rechazo, StringComparison.Ordinal);

        // NFR-05: ni la contraseña entera ni un fragmento reconocible de ella.
        Assert.DoesNotContain(excesiva.Password, rechazo, StringComparison.Ordinal);
        Assert.DoesNotContain("contrasena-excesiva", rechazo, StringComparison.Ordinal);

        // FEAT-001b: la FallbackPolicy exige sesión; sin una, `/` responde 302 al login. Se pide
        // no-redirect para observar el 302.
        using var cliente = arranque.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        Assert.Equal(HttpStatusCode.Redirect, (await cliente.GetAsync("/")).StatusCode);
    }

    [Fact]
    public void Iteraciones_De_Pbkdf2_No_Son_Degradables_Desde_Configuracion_Externa()
    {
        var laura = ConfiguracionDeAltas.Alta("laura");

        using var fabrica = new AppFactory();

        // Un entorno que intenta bajar el coste del hash desde afuera del repositorio, por las
        // claves que usaría un `GetSection` plausible. ADR-002 y M-5 existen para que ninguna de
        // ellas tenga efecto: un despliegue con `IterationCount: 1000` arrancaría sin que nada
        // avisara.
        using var arranque = ConfiguracionDeAltas.ArranqueConClaves(
            fabrica,
            registro: null,
            new KeyValuePair<string, string?>(
                "MiArchivoMedico:PasswordHasher:IterationCount", "1000"),
            new KeyValuePair<string, string?>(
                "MiArchivoMedico:PasswordHasher:CompatibilityMode",
                nameof(PasswordHasherCompatibilityMode.IdentityV2)),
            new KeyValuePair<string, string?>("PasswordHasher:IterationCount", "1000"),
            new KeyValuePair<string, string?>("Identity:PasswordHasher:IterationCount", "1000"),
            ConfiguracionDeAltas.Clave(0, nameof(AccountSeedEntry.UserName), laura.UserName),
            ConfiguracionDeAltas.Clave(0, nameof(AccountSeedEntry.Password), laura.Password));

        var opciones = arranque.Services.GetRequiredService<IOptions<PasswordHasherOptions>>().Value;

        Assert.Equal(AccountProvisioner.IteracionesDePbkdf2, opciones.IterationCount);
        Assert.Equal(PasswordHasherCompatibilityMode.IdentityV3, opciones.CompatibilityMode);

        // ADR-002 no pide sólo el valor: pide que esté fijado por un delegado en código. Enlazar
        // las opciones a configuración registraría un `NamedConfigureFromConfigurationOptions`
        // —derivado, no de este tipo exacto— junto a su fuente de recarga; borrar el bloque entero
        // no dejaría ninguna configuración registrada y el valor pasaría a depender del default
        // del framework, que ya cambió entre versiones.
        var configuraciones = arranque.Services
            .GetServices<IConfigureOptions<PasswordHasherOptions>>()
            .ToArray();

        Assert.Contains(
            configuraciones,
            configuracion => configuracion.GetType() == typeof(ConfigureNamedOptions<PasswordHasherOptions>));

        Assert.Empty(arranque.Services.GetServices<IOptionsChangeTokenSource<PasswordHasherOptions>>());

        // Y el efecto observable: el hash realmente almacenado sigue declarando 100.000 iteraciones.
        var cuenta = Assert.Single(ConfiguracionDeAltas.Cuentas(arranque.Services));
        var almacenado = Convert.FromBase64String(cuenta.PasswordHash!);

        Assert.Equal(0x01, almacenado[0]);
        Assert.Equal(
            (uint)AccountProvisioner.IteracionesDePbkdf2,
            BinaryPrimitives.ReadUInt32BigEndian(almacenado.AsSpan(5, 4)));
    }

    [Fact]
    public async Task El_Maximo_De_Cuentas_Se_Cuenta_Contra_Las_Persistidas()
    {
        var primeras = new[] { "ana", "bruno", "carla" }.Select(ConfiguracionDeAltas.Alta).ToArray();
        var segundas = new[] { "diego", "elena", "felipe" }.Select(ConfiguracionDeAltas.Alta).ToArray();

        using var fabrica = new AppFactory();

        using (var primero = ConfiguracionDeAltas.Arranque(fabrica, primeras))
        {
            Assert.Equal(3, ConfiguracionDeAltas.Cuentas(primero.Services).Count);
        }

        // El segundo host apunta al MISMO archivo de base y declara otras tres cuentas distintas.
        var registro = new RegistroEnMemoria();
        using var segundo = ConfiguracionDeAltas.ArranqueConRegistro(fabrica, registro, segundas);

        var cuentas = ConfiguracionDeAltas.Cuentas(segundo.Services);

        // NFR-02 se cuenta contra las cuentas PERSISTIDAS, no contra las declaradas: seis altas
        // repartidas en dos arranques dejan cinco cuentas. Una implementación que contara la
        // posición dentro de la lista declarada crearía las seis, porque ninguna de las dos listas
        // llega al límite por sí sola.
        Assert.Equal(AccountProvisioner.MaximoDeCuentasActivas, cuentas.Count);

        Assert.Equal(
            new[] { "ana", "bruno", "carla", "diego", "elena" },
            cuentas.Select(cuenta => cuenta.UserName).ToArray());

        var excedente = segundas[^1].UserName;

        Assert.DoesNotContain(cuentas, cuenta => cuenta.UserName == excedente);

        var rechazo = Assert.Single(
            registro.Mensajes,
            mensaje => mensaje.Contains(excedente, StringComparison.Ordinal));

        Assert.Contains(
            AccountProvisioner.MaximoDeCuentasActivas.ToString(CultureInfo.InvariantCulture),
            rechazo,
            StringComparison.Ordinal);

        // FEAT-001b: la FallbackPolicy exige sesión; sin una, `/` responde 302 al login. Se pide
        // no-redirect para observar el 302.
        using var cliente = segundo.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        Assert.Equal(HttpStatusCode.Redirect, (await cliente.GetAsync("/")).StatusCode);
    }

    /// <summary>Aplana un documento JSON a las claves de configuración que produciría.</summary>
    private static IEnumerable<(string Clave, string Valor)> Aplanar(JsonElement elemento, string prefijo)
    {
        switch (elemento.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var propiedad in elemento.EnumerateObject())
                {
                    var clave = prefijo.Length == 0 ? propiedad.Name : $"{prefijo}:{propiedad.Name}";
                    foreach (var hija in Aplanar(propiedad.Value, clave))
                    {
                        yield return hija;
                    }
                }

                break;

            case JsonValueKind.Array:
                var indice = 0;
                foreach (var elementoDelArreglo in elemento.EnumerateArray())
                {
                    foreach (var hija in Aplanar(elementoDelArreglo, $"{prefijo}:{indice}"))
                    {
                        yield return hija;
                    }

                    indice++;
                }

                break;

            default:
                yield return (prefijo, elemento.ToString());
                break;
        }
    }
}
