# Spec FEAT-001a: Arranque de la solución y alta administrativa de cuentas

| Campo | Valor |
|-------|-------|
| Ticket | FEAT-001a |
| PRD | `docs/daw/prd/prd-FEAT-001a.md` |
| Tier | FEATURE |
| Fecha | 2026-08-20 |
| Spec loops | 0 |
| Threat model | `docs/daw/security/threat-FEAT-001a.md` |
| ADR | `docs/adr/adr-001-esquema-por-migraciones-ef-core.md`, `docs/adr/adr-002-hashing-de-contrasenas-fijado-en-codigo.md`, `docs/adr/adr-003-alta-de-cuentas-en-el-arranque.md` |

## Summary

Se crea la solución .NET 8 desde cero: una aplicación web ASP.NET Core y un proyecto de tests de
integración, con el SDK fijado por `global.json` porque la máquina resuelve 10.0.300 por defecto. La
persistencia es EF Core sobre SQLite en modo WAL, con el archivo en una ruta que llega por
configuración externa y que el arranque **rechaza** si falta o si cae dentro del árbol de la
aplicación. El esquema se aplica con migraciones de EF Core (ADR-001). Sobre ese esquema, un
provisionador siembra en el arranque las cuentas declaradas en configuración externa (ADR-003),
hasheadas con PBKDF2 de parámetros fijados en código (ADR-002), de forma estrictamente aditiva y sin
que ningún rechazo escriba una credencial en un log. No se expone ninguna ruta: la superficie HTTP de
este sub-ticket es deliberadamente vacía, y AC-02 lo verifica.

## Coverage: PRD → blocks

| Requerimiento | Cubierto por |
|---|---|
| FR-01 | Bloque 3 |
| FR-02 | Bloque 3 |
| FR-03 | Bloque 3 |
| FR-04 | Bloque 2 |
| NFR-01 | Estrategia: `PasswordHasherOptions` fijado en `Program.cs` con `IdentityV3` e `IterationCount = 100_000`, nunca desde configuración externa (ADR-002). Verificado decodificando el hash almacenado en el test de AC-05. |
| NFR-02 | Estrategia: el provisionador cuenta las cuentas **persistidas** (`Users.CountAsync()`) antes de cada alta, no las declaradas en la configuración, de modo que dos arranques sucesivos no puedan superar el límite. Bloque 3. |
| NFR-03 | Estrategia: `PasswordOptions.RequiredLength = 12` con el resto de las reglas de composición desactivadas; la validación la aplica `UserManager`, el mecanismo central del stack, y no una comprobación propia duplicada en el provisionador. Bloque 3. |
| NFR-04 | Estrategia: `SqliteLocation` resuelve la ruta desde configuración externa y falla el arranque si falta o si resuelve dentro del `ContentRoot`; se abre con `journal_mode=WAL`; el directorio se crea con permisos `0700` en POSIX; `.gitignore` cubre `*.db`, `*.db-wal`, `*.db-shm` como defensa secundaria contra el versionado. Bloques 1 y 2. |
| NFR-05 | Estrategia: el provisionador registra por cada alta el nombre de usuario y la regla evaluada; jamás la contraseña declarada, el `PasswordHash` ni ningún fragmento de ellos. `AccountSeedEntry` sobrescribe `ToString()` para no exponer la contraseña. Bloque 3. |
| NFR-06 | Estrategia: `coverlet.collector` en el proyecto de tests, ejecutado por `dotnet test --collect:"XPlat Code Coverage"`; el umbral de 80 % lo verifica el gate `daw-test` en CODE. Bloque 1. |
| NFR-07 | Estrategia: la siembra es aditiva — si `FindByNameAsync` devuelve una cuenta, el provisionador no la toca ni compara su contraseña. Bloque 3. |

| Criterio de aceptación | Test que lo valida |
|---|---|
| AC-01 | `Siembra_Crea_Las_Cuentas_Declaradas_Que_No_Existen` (Bloque 3) |
| AC-02 | `Rutas_De_Registro_Y_De_Contrasena_Responden_404` (Bloque 3) |
| AC-03 | `Rechaza_El_Alta_Que_Supera_El_Maximo_De_Cuentas` (Bloque 3) |
| AC-04 | `Rechaza_El_Alta_Con_Contrasena_Menor_A_12_Caracteres` (Bloque 3) |
| AC-05 | `Hash_Almacenado_Usa_Pbkdf2_Con_Al_Menos_100000_Iteraciones` (Bloque 3) |
| AC-06 | `Base_Opera_En_Modo_Wal` y `Base_Reside_Fuera_Del_Arbol_De_La_Aplicacion` (Bloque 2) |
| AC-07 | `Segundo_Arranque_No_Duplica_Cuentas_Ni_Cambia_Sus_Credenciales` (Bloque 3) |
| AC-08 | `Rechazo_De_Alta_No_Registra_Credenciales` (Bloque 3) |
| AC-09 | `Ejecuta_Sobre_Net8` y la medición de cobertura del Bloque 1, verificada por el gate `daw-test` |

## Dependencies between blocks

- **Bloque 1** no depende de nada. Crea la solución sobre la que se apoyan los otros dos.
- **Bloque 2** depende del Bloque 1: necesita los proyectos y el `Program.cs` donde registrar el `DbContext`.
- **Bloque 3** depende del Bloque 2: siembra cuentas sobre un esquema que tiene que existir y estar migrado.

Orden de ejecución: 1 → 2 → 3. No hay paralelismo posible.

## Alcance del middleware (frontera con FEAT-001b y FEAT-001c)

Este sub-ticket configura **únicamente**: `UseDeveloperExceptionPage` en el entorno de desarrollo y
un manejador genérico fuera de él. Nada más.

Queda explícitamente **fuera**, por pertenecer a otros sub-tickets: `UseAuthentication`,
`UseAuthorization`, `FallbackPolicy`, `ConfigureApplicationCookie`, `UseHttpsRedirection` y `UseHsts`
(FEAT-001b); `LockoutOptions`, `ExpireTimeSpan` y `SlidingExpiration` (FEAT-001c). Por esa razón
Identity se registra con `AddIdentityCore<AppUser>()` y no con `AddIdentity<,>()`: la segunda instala
el esquema de autenticación por cookies con sus valores por defecto (14 días, deslizante), que es
justamente la política que FEAT-001c tiene que endurecer, y la dejaría configurada por omisión antes
de que nadie lo decida.

## Block 1 — Esqueleto de la solución

**Files**
- `global.json` (nuevo) — fija el SDK en `8.0.0` con `rollForward: latestFeature`, para no depender de qué SDK resuelva la máquina ni romper en otra.
- `MiArchivoMedico.sln` (nuevo) — agrupa los dos proyectos.
- `src/MiArchivoMedico.Web/MiArchivoMedico.Web.csproj` (nuevo) — `net8.0`, `Nullable=enable`, `ImplicitUsings=enable`. Referencias: `Microsoft.EntityFrameworkCore.Sqlite`, `Microsoft.EntityFrameworkCore.Design`, `Microsoft.AspNetCore.Identity.EntityFrameworkCore`. **Prohibido** referenciar `Microsoft.AspNetCore.Identity.UI`: ese paquete publica `/Identity/Account/Register`, `/ForgotPassword` y `/Manage/*` sin escribir una línea, y violaría FR-01 y AC-02.
- `src/MiArchivoMedico.Web/Program.cs` (nuevo) — composición mínima: entorno, manejo de errores y arranque del host. Los bloques 2 y 3 le agregan sus registros.
- `src/MiArchivoMedico.Web/appsettings.json` (nuevo) — solo `Logging` y `AllowedHosts`. Cero contraseñas, cero cadenas de conexión, cero rutas de base.
- `tests/MiArchivoMedico.Tests/MiArchivoMedico.Tests.csproj` (nuevo) — `net8.0`. Referencias: `Microsoft.AspNetCore.Mvc.Testing`, `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`, `coverlet.collector`.
- `tests/MiArchivoMedico.Tests/ArranqueTests.cs` (nuevo) — tests de humo.
- `.gitignore` (modificado) — agrega `bin/`, `obj/`, `*.db`, `*.db-wal`, `*.db-shm`, `appsettings.*.local.json`, sin tocar el bloque gestionado por DAW.

**Logic**

Se crea la solución y se deja compilando y testeando en verde, sin funcionalidad de producto. El
`.csproj` de tests declara `coverlet.collector` porque AC-09 exige medir cobertura y sin instrumento
no hay número que medir; `AGENTS.md` § Dependencias pide justificar toda dependencia nueva, y ésta es
su justificación. Igual criterio para xUnit: `AGENTS.md` solo fija `dotnet test`, así que la elección
del framework queda nombrada acá.

El test de humo **no** verifica que exista ninguna ruta anónima que devuelva 200. Verifica que el host
arranca y que el contenedor se resuelve. Un smoke test apoyado en una ruta pública se rompería en
cuanto FEAT-001b instale la autorización por defecto, y el implementador de ese ticket tendría que
"arreglar" un test de éste — que es la forma habitual de erosionar una invariante de privacidad.

**Input validation**

No aplica: este bloque no recibe entrada de usuario.

**Error handling**

- **E1.1 — El build resuelve un SDK distinto de 8.0.x.** `global.json` hace fallar la restauración con un mensaje explícito del SDK. Verificado en tiempo de ejecución por `Ejecuta_Sobre_Net8`.
- **E1.2 — Artefactos de build o archivos de base quedan versionados.** Prevenido por `.gitignore`; verificado por `Repositorio_Ignora_Artefactos_De_Build_Y_Bases`.
- **E1.3 — El host no puede construirse por una composición inválida.** La excepción se propaga y aborta el arranque; no se atrapa en silencio. Verificado por `Host_Arranca_Y_Resuelve_El_Contenedor`.
- **E1.4 — Se filtra una página de error detallada fuera de desarrollo.** `UseDeveloperExceptionPage` solo en `Development`; fuera de él, manejador genérico. Verificado por `Fuera_De_Desarrollo_No_Expone_Pagina_De_Error_Detallada`.

**Required tests**
- [ ] `Host_Arranca_Y_Resuelve_El_Contenedor` — el host construye y el proveedor de servicios se resuelve. Cubre E1.3 y da la base de AC-09.
- [ ] `Ejecuta_Sobre_Net8` — el runtime en ejecución es .NET 8.x. Cubre E1.1 y AC-09.
- [ ] `Repositorio_Ignora_Artefactos_De_Build_Y_Bases` — `.gitignore` cubre `bin/`, `obj/`, `*.db`, `*.db-wal` y `*.db-shm`. Cubre E1.2 y la mitigación M-3 del threat model.
- [ ] `Fuera_De_Desarrollo_No_Expone_Pagina_De_Error_Detallada` — con entorno `Production`, una excepción no devuelve traza ni detalle. Cubre E1.4 y M-7.

**Completion criterion**

`dotnet build` compila contra `net8.0` y `dotnet test --collect:"XPlat Code Coverage"` ejecuta los 4
tests en verde y produce un archivo de cobertura.

## Block 2 — Persistencia SQLite en WAL fuera del árbol de la aplicación

**Files**
- `src/MiArchivoMedico.Web/Data/AppDbContext.cs` (nuevo) — `IdentityDbContext<AppUser>`.
- `src/MiArchivoMedico.Web/Data/SqliteLocation.cs` (nuevo) — resuelve, valida y prepara la ubicación del archivo de base.
- `src/MiArchivoMedico.Web/Accounts/AppUser.cs` (nuevo) — `IdentityUser`, sin propiedades propias.
- `src/MiArchivoMedico.Web/Migrations/` (nuevo) — migración inicial generada por EF Core.
- `src/MiArchivoMedico.Web/Program.cs` (modificado) — registra `AppDbContext` y aplica las migraciones al arrancar.
- `tests/MiArchivoMedico.Tests/PersistenciaTests.cs` (nuevo).
- `tests/MiArchivoMedico.Tests/AppFactory.cs` (nuevo) — `WebApplicationFactory` que inyecta configuración en memoria y apunta la base a un directorio temporal del sistema operativo.

**Logic**

`SqliteLocation` lee la clave `MiArchivoMedico:Database:Path` de la configuración y aplica tres
reglas antes de que la aplicación siga levantando:

1. Si la clave falta o está vacía → **falla el arranque**. No existe valor por defecto. Un default
   tolerante dejaría la base bajo el `ContentRoot`, y además instalaría el antipatrón contrario justo
   donde RNF-62 del maestro exigirá fallar cuando llegue la clave de cifrado.
2. Si la ruta resuelta cae dentro del `ContentRootPath` o del `WebRootPath` → **falla el arranque**.
   `AGENTS.md` exige que la base viva fuera del repositorio y de toda carpeta pública; el `.gitignore`
   es defensa secundaria contra el versionado, no cumplimiento de la ubicación.
3. El directorio se crea si no existe, con permisos `0700` en plataformas POSIX.

El `DbContext` se registra con `AddDbContext` **scoped, sin pooling y sin `IDbContextFactory`**: el
pooling y las factorías singleton dificultan inyectar después un accesor del usuario autenticado, que
es lo que hará falta para el filtro global por propietario que exige RNF-53 del maestro. No se
construye nada de ese mecanismo ahora — no hay datos que aislar — pero tampoco se le cierra la puerta.

La conexión se abre con `journal_mode=WAL`. Las migraciones pendientes se aplican antes de cualquier
otra cosa que toque la base.

**Data model**

Entidad `AppUser`, que hereda de `IdentityUser` y **no agrega ninguna propiedad propia**. Las columnas
para sesión única y bloqueo pertenecen a FEAT-001c; las que llegan heredadas del framework
(`AccessFailedCount`, `LockoutEnd`, `SecurityStamp`) vienen con el esquema de Identity y no se
configuran en este sub-ticket.

Esquema creado por la migración inicial, el estándar de ASP.NET Core Identity:

- `AspNetUsers` — PK `Id` (`TEXT`, GUID generado por Identity, no nulo). `UserName` y `Email`
  (`TEXT`, nulables por el modelo de Identity). `NormalizedUserName` (`TEXT`) con **índice único**
  `UserNameIndex`. `NormalizedEmail` con índice no único. `PasswordHash` (`TEXT`, nulable en el
  modelo; siempre poblado por este producto). `SecurityStamp`, `ConcurrencyStamp` (`TEXT`).
  `LockoutEnd` (`TEXT`, nulable), `LockoutEnabled` y `EmailConfirmed` (`INTEGER`, no nulos),
  `AccessFailedCount` (`INTEGER`, no nulo, default 0).
- `AspNetRoles`, `AspNetUserRoles`, `AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserTokens`,
  `AspNetRoleClaims` — tablas del esquema de Identity, con sus PK, FK a `AspNetUsers` con borrado en
  cascada y sus índices por defecto. El producto no usa roles (RNF-57 del maestro los excluye), pero
  las tablas llegan con el esquema y quitarlas sería divergir del mecanismo central sin necesidad.
- `__EFMigrationsHistory` — control de migraciones aplicadas.

**Input validation**

La única entrada de este bloque es la ruta de la base, que viene de configuración externa y no de un
usuario final: `MiArchivoMedico:Database:Path`, cadena no vacía, ruta de archivo del sistema
operativo, resuelta a ruta absoluta antes de validarse, y rechazada si queda dentro del `ContentRoot`
o del `WebRoot`.

**Error handling**

- **E2.1 — Falta la ruta de la base en la configuración.** El arranque falla con un mensaje que nombra la clave ausente y ningún valor.
- **E2.2 — La ruta resuelta cae dentro del árbol de la aplicación.** El arranque falla nombrando la regla incumplida.
- **E2.3 — No se puede crear el directorio de la base.** La excepción del sistema de archivos se propaga y aborta el arranque.
- **E2.4 — Una migración no puede aplicarse.** La excepción se propaga y aborta el arranque; el mensaje no incluye contenido de la base.

**Required tests**
- [ ] `Aplica_Las_Migraciones_Y_Crea_El_Esquema` — tras arrancar, existen `AspNetUsers` y `__EFMigrationsHistory`. Valida FR-04.
- [ ] `Base_Opera_En_Modo_Wal` — `PRAGMA journal_mode` devuelve `wal` sobre el archivo real. Valida AC-06.
- [ ] `Base_Reside_Fuera_Del_Arbol_De_La_Aplicacion` — la ruta efectiva no está bajo `ContentRootPath` ni bajo `WebRootPath`. Valida AC-06.
- [ ] `Crea_El_Directorio_Con_Permisos_Restrictivos` — en POSIX, el directorio tiene modo `0700`. Valida M-9. En Windows se omite explícitamente.
- [ ] `Arranque_Falla_Si_Falta_La_Ruta_De_La_Base` — sad path. Cubre E2.1.
- [ ] `Arranque_Falla_Si_La_Ruta_Cae_Dentro_Del_Arbol_De_La_Aplicacion` — sad path. Cubre E2.2.
- [ ] `Arranque_Falla_Si_No_Puede_Crear_El_Directorio` — sad path con una ruta no creable. Cubre E2.3.
- [ ] `Arranque_Falla_Si_La_Migracion_No_Puede_Aplicarse` — sad path con un archivo de base corrupto. Cubre E2.4.

**Completion criterion**

Los 8 tests pasan; la base de los tests es un **archivo real** en el directorio temporal del sistema
operativo —no SQLite en memoria, que no tiene modo WAL y dejaría AC-06 sin probar, ni un archivo bajo
`bin/`, que sigue dentro del repositorio— y se limpia al terminar.

## Block 3 — Alta administrativa de cuentas

**Files**
- `src/MiArchivoMedico.Web/Accounts/AccountSeedOptions.cs` (nuevo) — `AccountSeedOptions` con `List<AccountSeedEntry> Accounts`; `AccountSeedEntry` con `UserName` y `Password`, y `ToString()` sobrescrito para no exponer la contraseña.
- `src/MiArchivoMedico.Web/Accounts/AccountProvisioner.cs` (nuevo) — aplica las altas al arrancar.
- `src/MiArchivoMedico.Web/Program.cs` (modificado) — registra `AddIdentityCore<AppUser>()` con `PasswordOptions` y `PasswordHasherOptions`, enlaza `AccountSeedOptions` y ejecuta el provisionador tras las migraciones.
- `tests/MiArchivoMedico.Tests/AltaDeCuentasTests.cs` (nuevo).
- `tests/MiArchivoMedico.Tests/SuperficieHttpTests.cs` (nuevo).

**Logic**

`AccountProvisioner` recorre las entradas declaradas y, por cada una:

1. Si `FindByNameAsync` devuelve una cuenta → no la toca. Ni la contraseña, ni ninguna otra
   propiedad. La siembra es estrictamente aditiva (ADR-003): un arranque no puede cambiar
   credenciales, porque eso convertiría reiniciar el proceso en un vector de secuestro de cuenta.
2. Si no existe, cuenta las cuentas **persistidas** con `Users.CountAsync()`. Si el alta elevaría el
   total por encima de 5 → rechazo. Contar la lista declarada en vez de lo persistido permitiría que
   dos arranques sucesivos superaran el límite.
3. Crea la cuenta con `UserManager.CreateAsync(user, password)`. **El mínimo de 12 caracteres lo
   aplica `PasswordOptions.RequiredLength`**, no una comprobación propia: `AGENTS.md` pide reutilizar
   el mecanismo central en vez de duplicar reglas de seguridad, y una comprobación escrita a mano acá
   dejaría de aplicarse el día que el restablecimiento administrativo pase por otro camino.
4. Registra el resultado: nombre de usuario y regla o código de error de `IdentityResult`. Nunca la
   contraseña ni el hash.

Un rechazo **no aborta el arranque**: la aplicación sigue levantando con las cuentas válidas, como
exigen AC-03 y AC-04. Lo único que aborta el arranque es no poder resolver la ubicación de la base
(Bloque 2).

`PasswordHasherOptions` se fija en código: `CompatibilityMode = IdentityV3` e `IterationCount =
100_000` (ADR-002). `PasswordOptions`: `RequiredLength = 12`, `RequireDigit`, `RequireLowercase`,
`RequireUppercase`, `RequireNonAlphanumeric` y `RequiredUniqueChars` desactivados — NFR-03 no exige
reglas de composición y agregarlas sería alcance no pedido.

"Cuenta activa", a los efectos de NFR-02, es una fila existente en `AspNetUsers`. Este alcance no
tiene noción de baja, y un flag de estado sería funcionalidad fuera del PRD.

No se mapea ninguna ruta. Con la superficie HTTP vacía, `/Identity/Account/Register` y sus hermanas
responden 404 por no existir — que es exactamente lo que pide AC-02, y la razón por la que el
`.csproj` tiene prohibido referenciar `Microsoft.AspNetCore.Identity.UI`.

**Input validation**

Entrada: la configuración externa de altas, bajo `MiArchivoMedico:Accounts`. Origen admitido:
variables de entorno o user-secrets. **Nunca un archivo versionado**; los tests la inyectan en memoria
con `ConfigureAppConfiguration`, jamás con un `appsettings.Test.json` con contraseñas.

- `UserName`: requerido, no vacío tras recortar espacios, 1 a 256 caracteres.
- `Password`: requerido, no vacío, mínimo 12 caracteres (aplicado por `PasswordOptions`), máximo 128.
- La lista puede declarar cualquier cantidad de entradas; el límite de 5 se evalúa contra lo persistido, entrada por entrada.

**Error handling**

- **E3.1 — Contraseña de menos de 12 caracteres.** `UserManager` devuelve `IdentityResult` fallido con `PasswordTooShort`; se registra el rechazo con el nombre de usuario y la regla, la cuenta no se crea y el arranque continúa.
- **E3.2 — El alta superaría las 5 cuentas activas.** Se rechaza antes de llamar a `UserManager`, se registra el límite alcanzado, las cuentas existentes quedan intactas y el arranque continúa.
- **E3.3 — Entrada con nombre de usuario vacío o repetido dentro de la misma configuración.** Se rechaza esa entrada y se registra; las demás se procesan.
- **E3.4 — `IdentityResult` fallido por cualquier otra causa.** Se registran los códigos de error devueltos, sin credenciales, y el arranque continúa.
- **E3.5 — La cuenta ya existe.** No es un error: se omite sin modificarla y se registra la omisión.

**Required tests**
- [ ] `Siembra_Crea_Las_Cuentas_Declaradas_Que_No_Existen` — valida AC-01 y FR-01.
- [ ] `Rutas_De_Registro_Y_De_Contrasena_Responden_404` — `/Identity/Account/Register`, `/Identity/Account/ForgotPassword`, `/Identity/Account/ResetPassword` y `/Identity/Account/Manage` devuelven 404. Valida AC-02 y FR-01.
- [ ] `Rechaza_El_Alta_Que_Supera_El_Maximo_De_Cuentas` — con 5 cuentas persistidas, la sexta se rechaza, las 5 quedan intactas y el arranque completa. Valida AC-03, FR-02 y cubre E3.2.
- [ ] `Rechaza_El_Alta_Con_Contrasena_Menor_A_12_Caracteres` — una contraseña de 11 caracteres no crea cuenta y el arranque completa. Valida AC-04, FR-03 y cubre E3.1.
- [ ] `Hash_Almacenado_Usa_Pbkdf2_Con_Al_Menos_100000_Iteraciones` — decodifica el `PasswordHash`, verifica el marcador de formato `IdentityV3` y lee el contador de iteraciones. Valida AC-05 y NFR-01.
- [ ] `Segundo_Arranque_No_Duplica_Cuentas_Ni_Cambia_Sus_Credenciales` — dos arranques con la misma configuración: mismo conjunto de cuentas y mismo `PasswordHash`. Valida AC-07, NFR-07 y cubre E3.5.
- [ ] `Arranque_Con_Configuracion_De_Altas_Retirada_Conserva_Las_Cuentas` — tras crear la cuenta, un arranque sin la configuración levanta igual y la cuenta sigue existiendo. Valida M-13 del threat model.
- [ ] `Rechazo_De_Alta_No_Registra_Credenciales` — capturando el log, ningún mensaje contiene la contraseña declarada ni el hash. Valida AC-08, NFR-05 y cubre E3.4.
- [ ] `Rechaza_Entrada_Con_Nombre_De_Usuario_Vacio` — sad path; las demás entradas se procesan igual. Cubre E3.3.
- [ ] `Appsettings_Versionado_No_Contiene_Contrasenas` — lee el `appsettings.json` real del repositorio y verifica que no declara ninguna clave de contraseña ni cadena de conexión. Valida M-2 del threat model.

**Completion criterion**

Los 10 tests pasan, los 9 AC del PRD tienen al menos un test en verde, y `dotnet test` completo
—bloques 1, 2 y 3— queda en verde con cobertura medida y no inferior al 80 %.

## Reversión

Este sub-ticket introduce una migración de esquema, así que la reversión no es solo revertir el
commit.

- **Antes del primer despliegue en producción** (el caso de este ticket: la base no existe todavía en
  ningún lado): revertir el commit y borrar el archivo de base local alcanza. No hay dato que perder.
- **Después de un despliegue con cuentas creadas:** revertir el código deja el esquema de Identity en
  la base. Es inofensivo —tablas sin uso— y **no debe deshacerse con una migración destructiva**,
  prohibidas por `AGENTS.md`. Si hiciera falta volver atrás de verdad, se detiene el proceso, se
  respalda el archivo de base con `VACUUM INTO` o la API de backup en línea —nunca copiándolo en
  caliente— y recién ahí se decide.
- **Indicador para revertir:** el arranque falla de forma reproducible por una causa que no sea
  configuración ausente o inválida, que son fallos deliberados de E2.1 y E2.2.

## Final verification

- Los 4 FR del PRD están cubiertos y los 9 AC tienen test en verde.
- La aplicación arranca, migra el esquema y siembra las cuentas declaradas; un arranque repetido no
  cambia nada.
- No existe ninguna ruta que cree, cambie o recupere una contraseña; la superficie HTTP está vacía.
- Ningún log, mensaje ni excepción contiene una credencial.
- El archivo de base vive fuera del árbol de la aplicación, en modo WAL, con el directorio en `0700`.
- El repositorio no versiona bases, artefactos de build ni secretos.
- Ningún middleware de FEAT-001b o FEAT-001c quedó configurado por adelantado.
