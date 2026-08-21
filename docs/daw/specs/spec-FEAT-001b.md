# Spec FEAT-001b: Inicio y cierre de sesión, y protección de las rutas privadas

| Campo | Valor |
|-------|-------|
| Ticket | FEAT-001b |
| PRD | `docs/daw/prd/prd-FEAT-001b.md` |
| Tier | FEATURE |
| Fecha | 2026-08-21 |
| Spec loops | 0 |
| Threat model | `docs/daw/security/threat-FEAT-001b.md` |
| ADR | ninguno nuevo por el momento; se evalúa durante el threat modeling. Hereda `docs/adr/adr-001-esquema-por-migraciones-ef-core.md`, `docs/adr/adr-002-hashing-de-contrasenas-fijado-en-codigo.md`, `docs/adr/adr-003-alta-de-cuentas-en-el-arranque.md`. |

## Summary

FEAT-001a dejó la aplicación arrancando, con su esquema creado y sus cuentas sembradas, pero sin forma
de usarlas: no hay inicio de sesión ni pantalla privada. FEAT-001b instala la puerta de entrada y la
cierra: una pantalla de inicio de sesión (GET/POST en `/Cuenta/IniciarSesion`), una acción de cierre de
sesión (POST en `/Cuenta/CerrarSesion`), una única página privada mínima en `/` como destino (sin datos
médicos, identificando a la cuenta autenticada y ofreciendo cerrar sesión), y el bloqueo de toda ruta
privada para quien no tenga sesión válida.

La protección se resuelve con **1 único mecanismo central**: `AddAuthorization` con `FallbackPolicy` que
exige usuario autenticado, aplicado por defecto a todas las rutas (NFR-07). La autenticación por cookie
se configura con `AddIdentityCore<AppUser>` + `AddSignInManager()` + `AddAuthentication(ApplicationScheme)` +
`AddCookie(...)`, con la cookie declarando `Secure`, `HttpOnly` y `SameSite=Strict` y **0 atributos de
persistencia** (sin `Expires`, sin `Max-Age`) — NFR-01 y NFR-02. El rechazo de credenciales devuelve
**1 único mensaje** indistinguible en todos los casos (NFR-03, AC-04). HTTPS al 100 % con redirección
HTTP→HTTPS y HSTS fuera de Development queda en alcance de este sub-ticket (AC-08), delegado por FEAT-001a.
Ningún log puede contener contraseñas, valores de cookie ni hashes (NFR-05, AC-09), y la cobertura del
código incorporado se verifica en el gate `daw-test` (NFR-06, AC-10).

Este sub-ticket **no** entrega expiración de sesión, sesión única ni bloqueo por intentos fallidos
(FEAT-001c): la sesión dura mientras el navegador siga abierto.

## Coverage: PRD → blocks

| Requerimiento | Cubierto por |
|---|---|
| FR-01 | Bloque 1 (FallbackPolicy) + Bloque 2 (única excepción `[AllowAnonymous]` en IniciarSesion). Verificado por AC-01 y AC-02. |
| FR-02 | Bloque 2 (POST IniciarSesion contra `SignInManager.PasswordSignInAsync`). Verificado por AC-03 y AC-04. |
| FR-03 | Bloque 2 (POST CerrarSesion → `HttpContext.SignOutAsync(ApplicationScheme)`). Verificado por AC-05. |
| FR-04 | Bloque 2 (Privada en `/`) + Bloque 3 (vista Privada). Verificado por AC-06. |
| NFR-01 | Bloque 1: `Cookie.SecurePolicy = SecurePolicy.Always`, `Cookie.HttpOnly = true`, `Cookie.SameSite = SameSiteMode.Strict`. Verificado por AC-07 y AC-09. |
| NFR-02 | Bloque 1: sin `ExpireTimeSpan` ni `Cookie.Expiration`; `isPersistent: false` en el login. Verificado por AC-07. |
| NFR-03 | Bloque 2: una sola rama else sobre `resultado.Succeeded` con un único mensaje; sin ramificar sobre `IsLockedOut`/`IsNotAllowed`/`RequiresTwoFactor`. Verificado por AC-04. |
| NFR-04 | Bloque 1: `UseHttpsRedirection` + `UseHsts` solo fuera de Development, `HttpsPort` inyectable por configuración. Verificado por AC-08. |
| NFR-05 | Bloque 2 + Bloque 3 + Bloque 4: ningún log, vista ni helper registra contraseñas, cookies o hashes. Verificado por AC-09. |
| NFR-06 | Bloque 4: medición con `coverlet.collector`; umbral de 80 % verificado por el gate `daw-test` en CODE (Camino A). Verificado por AC-10. |
| NFR-07 | Bloque 1: `FallbackPolicy = RequireAuthenticatedUser` en `AddAuthorization`, único mecanismo central. Verificado por AC-01 y AC-02. |

| Criterio de aceptación | Test que lo valida |
|---|---|
| AC-01 | `Pagina_Privada_Sin_Sesion_Redirige_Al_Inicio_De_Sesion` (Bloque 2) |
| AC-02 | `Ruta_Privada_Sin_Sesion_No_Entrega_Contenido` (Bloque 2) |
| AC-03 | `Inicio_De_Sesion_Correcto_Establece_Sesion_Y_Responde_La_Privada` (Bloque 4) |
| AC-04 | `Rechazo_Es_Identico_Si_El_Usuario_No_Existe_O_La_Contrasena_Es_Incorrecta` (Bloque 4) |
| AC-05 | `Cierre_De_Sesion_Invalida_La_Sesion` (Bloque 4) |
| AC-06 | `Pagina_Privada_Identifica_A_La_Cuenta_Y_Ofrece_Cerrar_Sesion` (Bloque 4) |
| AC-07 | `Cookie_De_Autenticacion_Declara_Secure_HttpOnly_SameSite_Strict_Y_Es_De_Sesion` (Bloque 4) |
| AC-08 | `Fuera_De_Desarrollo_Redirige_Http_A_Https_Y_Emite_Hsts` (Bloque 1) |
| AC-09 | `Rechazo_No_Registra_Contrasena_Cookie_Ni_Hash` (Bloque 4) |
| AC-10 | medición de cobertura del Bloque 4, verificada por el gate `daw-test` en CODE |

## Dependencies between blocks

- **Bloque 1** no depende de nada de este sub-ticket: modifica `Program.cs` y los tests que hereda de
  FEAT-001a. Es la base sobre la que se apoyan los otros tres.
- **Bloque 2** depende del Bloque 1: `CuentaController` inyecta `SignInManager<AppUser>` y `UserManager`,
  que solo existen si la Composition Root los registra.
- **Bloque 3** depende del Bloque 2: las vistas y el ViewModel referencian `CuentaController` y sus
  tipos (`IniciarSesionViewModel`, `AppUser`), y la vista IniciarSesion depende del mecanismo de
  antiforgery del `FormTagHelper`, que el Bloque 1 habilita con `AddControllersWithViews`.
- **Bloque 4** depende de los bloques 2 y 3: el helper `AutenticacionDePruebas` ejercita login/logout
  sobre el controlador y las vistas reales.

Orden de ejecución: 1 → 2 → 3 → 4. No hay paralelismo posible.

## Alcance del middleware (frontera con FEAT-001c)

Este sub-ticket configura el pipeline completo de la frontera de autenticación:

1. `UseExceptionHandler` al inicio del pipeline (PRIMERO), con manejador genérico fuera de Development
   (heredado de FEAT-001a y conservado).
2. `UseHsts()` + `UseHttpsRedirection()` **solo fuera de Development** (AC-08).
3. `app.UseRouting()` explícito, antes de la autenticación.
4. `UseAuthentication()` → **obligatoriamente antes** de `UseAuthorization()`.
5. `UseAuthorization()` (aplica la FallbackPolicy).
6. `MapControllerRoute("default", "{controller=Cuenta}/{action=Privada}/{id?}")`. **Nunca** `MapFallback`.

Queda explícitamente **fuera**, por pertenecer a FEAT-001c: `LockoutOptions`, `ExpireTimeSpan`,
`SlidingExpiration`, el bloqueo por intentos fallidos y la sesión única por cuenta. Este sub-ticket
no configura nada de eso; una sesión dura mientras el navegador siga abierto.

## Block 1 — Composition Root de autenticación y pipeline

**Files**
- `src/MiArchivoMedico.Web/Program.cs` (modificado) — registra Identity + cookie + autorización, arma el
  pipeline de middleware y mapea las rutas.
- `tests/MiArchivoMedico.Tests/AltaDeCuentasTests.cs` (modificado) — reexpresa las 7 aserciones `NotFound`
  del `/` como `Redirect` (302); ver Detalle de impacto.
- `tests/MiArchivoMedico.Tests/ArranqueTests.cs` (modificado) — sustituye el `FiltroQueProvocaUnaExcepcion`
  (`IStartupFilter` con `Run(throw)`) por un controlador de solo-tests con `[AllowAnonymous]` que lanza
  una excepción, expuesto vía `AddApplicationPart(Assembly del proyecto de tests)`; ver Detalle de impacto.
- `tests/MiArchivoMedico.Tests/ControladorDePruebasDeExcepcion.cs` (nuevo) — el controlador de solo-tests
  con una action `[AllowAnonymous]` que lanza `InvalidOperationException("Se produjo un error inesperado.")`,
  ejecutado únicamente en el entorno `Production` del test.

**Logic**

`Program.cs` gana, en este orden:

```csharp
builder.Services.AddIdentityCore<AppUser>(opciones => { /* PasswordOptions heredado de FEAT-001a */ })
    .AddSignInManager()
    .AddEntityFrameworkStores<AppDbContext>();
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddCookie(IdentityConstants.ApplicationScheme, opciones =>
    {
        opciones.LoginPath = "/Cuenta/IniciarSesion";
        opciones.AccessDeniedPath = "/Cuenta/IniciarSesion";
        opciones.Cookie.Name = "MiArchivoMedico.Auth";
        opciones.Cookie.HttpOnly = true;
        opciones.Cookie.SameSite = SameSiteMode.Strict;
        opciones.Cookie.SecurePolicy = CookieSecurePolicy.Always; // sin ExpireTimeSpan ni Cookie.Expiration → NFR-02
    });
builder.Services.AddAuthorization(opciones =>
    opciones.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser().Build()); // NFR-07
builder.Services.AddControllersWithViews(); // (en el build de tests, + AddApplicationPart del ensamblado de tests)
```

Y en el pipeline:

```csharp
app.UseExceptionHandler("/...");            // 1. primero, heredado de FEAT-001a
if (!app.Environment.IsDevelopment()) {     // 2. AC-08
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseRouting();                            // 3. explícito
app.UseAuthentication();                     // 4. SIEMPRE antes de UseAuthorization
app.UseAuthorization();                      // 5. aplica la FallbackPolicy
app.MapControllerRoute("default", "{controller=Cuenta}/{action=Privada}/{id?}"); // 6. nunca MapFallback
```

- `AddSignInManager()` es obligatorio: `CuentaController` va a inyectar `SignInManager<AppUser>`, y sin
  este registro el contenedor no lo resuelve (Composition Root). El `SignInManager` firma la cookie con
  `IdentityConstants.ApplicationScheme` de forma hardcodeada.
- **Prohibido** `AddIdentity<TUser,TRole>()` o `AddDefaultIdentity<>()`: instalarían el esquema de cookies
  por defecto (14 días, deslizante) que contradice NFR-02.
- `UseAuthentication` debe preceder a `UseAuthorization`; invertir el orden deja la FallbackPolicy sin sesión
  que evaluar y redirigiría al login en cada ruta.
- Para AC-08, `UseHttpsRedirection` resuelve el puerto HTTPS desde `HttpsRedirectionOptions.HttpsPort`,
  que se inyecta en el test con `UseSetting("https_port", ...)` bajo `UseEnvironment("Production")`. HSTS se
  verifica en dos partes: (a) el redirect 307 con `Location` HTTPS, y (b) un shim de HSTS que se activa
  cuando `Request.Scheme == "https"`, inyectado como `IStartupFilter` prefijado solo en el test; sobre el
  TestServer HTTP la cabecera HSTS jamás se emite (no hay HTTPS real en el pipeline de tests).

**Detalle de impacto (verificación de regresión FEAT-001a)**

1. **`AltaDeCuentasTests` (7 puntos).** Los 7 tests que solicitan `/` sin sesión esperaban `NotFound` (404)
   porque FEAT-001a no mapeaba rutas. Con la FallbackPolicy y la ruta `/` mapeada, la petición ahora recibe
   una `Redirect` (302) al login. Reexpresión: en cada uno de los 7 puntos se cambia la aserción a
   `Assert.Equal(HttpStatusCode.Redirect, cliente.GetAsync("/").Result.StatusCode)`. Además, el
   `WebApplicationFactory` por defecto sigue las redirecciones (`AllowAutoRedirect = true`), lo que
   enmascararía la 302: los 7 `CreateClient()` del archivo pasan a `CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false })`. Los comentarios in-file se actualizan para explicar que la 302 es la respuesta esperada ahora. Este test en concreto **cubre AC-01**.
2. **`ArranqueTests.Fuera_De_Desarrollo_No_Expone_Pagina_De_Error_Detallada`.** El `IStartupFilter` con
   `app.Run(throw)` quedaba detrás del challenge 302 de la FallbackPolicy y su excepción ya no llegaba al
   `UseExceptionHandler`. Fix E1.4: el shim pasa a ser un **controlador de solo-tests** — se registra
   `AddControllersWithViews().AddApplicationPart(typeof(...En el ensamblado de tests).Assembly)` —
   expuesto únicamente en el build de tests, con una action `[AllowAnonymous]` que lanza. El test conserva
   su proposición original: en entorno `Production`, la excepción queda atrapada por `UseExceptionHandler`
   y la respuesta es 500 con "Se produjo un error inesperado." Sin este controlador el pipeline de error
   fuera de dev no queda ejercitado por ningún test.
3. **`SuperficieHttpTests.Rutas_De_Registro_Y_De_Contrasena_Responden_404`.** Este test **no cambia**:
   aunque la FallbackPolicy exige usuario autenticado, las rutas `/Identity/Account/Register`,
   `/ForgotPassword`, `/ResetPassword` y `/Manage` no tienen endpoint mapeado, así que `AuthorizationMiddleware`
   no les aplica la política (no hay `GetEndpoint()`) y responden 404. Con `AllowAutoRedirect=false` se
   confirma que es un 404 real y no un redirect.

**Input validation**

No aplica: este bloque no recibe entrada de usuario. Las únicas entradas son de configuración (`https_port`
en el test de AC-08).

**Error handling**

- **E1.1 — `UseAuthentication` ordenado después de `UseAuthorization`.** Compila pero rompe toda la
  autorización (la política no ve la identidad). Prevenido por la revisión del orden en la spec; verificado
  por AC-01 y AC-02.
- **E1.2 — El bloque de tests (ApplicationPart) se filtra al build de producción.** Prohibido: el ensamblado
  de tests no debe referenciarse desde el ensamblado de la aplicación. Solo el **proyecto de tests** hace
  `AddApplicationPart` sobre el ensamblado de tests dentro de su propia `WebApplicationFactory`.
- **E1.3 — HSTS o redirect HTTPS se activan en Development.** Rompe el desarrollo local (NFR-01 rebaja a
  HTTPS local, no a HTTP). Prevenido por la guarda `if (!app.Environment.IsDevelopment())`; verificado por
  `En_Desarrollo_No_Redirige_Http_A_Https` en el Bloque 1.
- **E1.4 — Una excepción fuera de desarrollo filtra una página de error detallada.** Conservado el manejador
  genérico; verificado por `Fuera_De_Desarrollo_No_Expone_Pagina_De_Error_Detallada` (con el nuevo controlador
  de solo-tests).

**Required tests**
- [ ] `Pagina_Privada_Sin_Sesion_Redirige_Al_Inicio_De_Sesion` — `/` sin sesión devuelve 302 con `Location` al login sin incluir dato de ninguna cuenta. Valida AC-01 y FR-01.
- [ ] `Ruta_Privada_Sin_Sesion_No_Entrega_Contenido` — rutas privadas distintas del login devuelven 401/403 o redirección al login, nunca el contenido. Valida AC-02, FR-01 y NFR-07.
- [ ] `En_Desarrollo_No_Redirige_Http_A_Https` — con `UseEnvironment("Development")`, HTTP no redirige a HTTPS ni emite HSTS. Cubre E1.3.
- [ ] `Fuera_De_Desarrollo_Redirige_Http_A_Https_Y_Emite_Hsts` — con `UseEnvironment("Production")` e `HttpsPort`, HTTP responde 307 con `Location` HTTPS y la cabecera HSTS aparece cuando el esquema es https. Valida AC-08 y NFR-04.
- [ ] `Fuera_De_Desarrollo_No_Expone_Pagina_De_Error_Detallada` — con `Production`, el controlador de solo-tests dispara la excepción y la respuesta es 500 con "Se produjo un error inesperado." sin traza. Cubre E1.4 (reescritura de FEAT-001a).
- [ ] Reexpresión de los 7 tests de `AltaDeCuentasTests.cs` a `Redirect` — regresión FEAT-001a en verde con la autorización activa.

**Completion criterion**

El host compila y arranca con la Composition Root completa; los tests del Bloque 1 (incluida la regresión
de FEAT-001a reexpresada) pasan en verde; `/` sin sesión redirige al login y ninguna ruta privada entrega
contenido sin sesión.

## Block 2 — CuentaController: login, logout y página privada

**Files**
- `src/MiArchivoMedico.Web/Controllers/CuentaController.cs` (nuevo).

**Logic**

`CuentaController` (base de `Controller`) expone tres acciones:

1. **`GET /Cuenta/IniciarSesion`** — `[AllowAnonymous]`. Devuelve la vista con un `IniciarSesionViewModel`
   vacío. Es la **única** acción con `[AllowAnonymous]` del sistema.
2. **`POST /Cuenta/IniciarSesion`** — `[AllowAnonymous]`. Modelo enlazado desde el form con antiforgery
   automática (FormTagHelper). Flujo:
   - Si `ModelState` inválido → devuelve la misma vista, **200, sin `Location`**.
   - `var resultado = await _signInManager.PasswordSignInAsync(nombreUsuario, contrasena, isPersistent: false, lockoutOnFailure: false)`.
   - **Un solo branch sobre `resultado.Succeeded`**:
     - `true` → `RedirectToAction(nameof(Privada))` → 302 → GET `/` ya autenticado (PRG). **`returnUrl` se
       ignora siempre**: no se lee del querystring, no se valida y no se usa para redirigir. No hay open redirect.
     - `false` → misma vista, mismo mensaje único
       `ModelState.AddModelError(string.Empty, "Nombre de usuario o contraseña no válidos")`, **200 sin
       `Location`**. `IsLockedOut`, `IsNotAllowed` y `RequiresTwoFactor` caen todos en esta rama; está
       **prohibido** ramificar sobre el enum.
   - `lockoutOnFailure: false`: FEAT-001c entrega el bloqueo; acá registrar un fallo incremental no solo
     adelanta alcance sino que cambia el estado de la cuenta, que este sub-ticket no debe tocar.
3. **`POST /Cuenta/CerrarSesion`** — exige sesión (cae bajo la FallbackPolicy). `HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme)` y redirige al login (`RedirectToAction(nameof(IniciarSesion))`).
4. **`GET /`** — `Privada()`, `[Route("/")]` sobre el método. Exige sesión (FallbackPolicy). Lee
   `User.Identity!.Name` del `ClaimsPrincipal` — **sin consultas a la base de datos** — y devuelve la vista
   Privada con el nombre. No contiene datos médicos ni datos de ninguna otra cuenta.

El controlador inyecta `SignInManager<AppUser>` y `UserManager<AppUser>` (este último se usa en el helper
de tests, no en el login). La página privada no ejecuta ninguna consulta de dominio: la autenticación de
`[Authorize]`/FallbackPolicy ya garantiza que `User.Identity` está autenticado.

**Input validation**

- `NombreDeUsuario`: formado en el view model, `[Required]`, recortado de espacios al enlazarse, máximo 256.
- `Contrasena`: formado en el view model, `[Required]`, `[DataType(DataType.Password)]`. La contraseña
  **no se re-renderiza**: al re-devolver la vista tras un rechazo, `Contrasena` es `null` en el ViewModel.
- La validación de no-existencia vs contraseña incorrecta la resuelve `SignInManager.CheckPasswordAsync`:
  cualquier fallo → rama else → mismo mensaje.

**Error handling**

- **E2.1 — Usuario inexistente.** `PasswordSignInAsync` devuelve fallido; se re-renderiza con el mensaje
  único y 200 sin `Location`. El evento `PasswordMismatch`/`UserNotFound` no se registra con credenciales.
- **E2.2 — Contraseña incorrecta.** Idéntico a E2.1 en mensaje, estado y headers (AC-04).
- **E2.3 — Cuenta bloqueada o no permitida (IsLockedOut/IsNotAllowed/RequiresTwoFactor).** Caen en la misma
  rama else con el mismo mensaje; no hay rama separada (aunque FEAT-001c deje de ser alcanzable en un futuro, hoy el mensaje permanece único).
- **E2.4 — Token antiforgery ausente o inválido.** ModelState inválido antes del login → misma vista, 200, sin
  Location; el `FormTagHelper` incluye el `__RequestVerificationToken` automáticamente.
- **E2.5 — Alguien intenta logout por GET.** GET `/Cuenta/CerrarSesion` no existe (solo POST); el enrutado
  devuelve 405 o la FallbackPolicy lo redirige al login, y el navegador nunca ejecuta un logout por GET
  (la vista usa un form POST).

**Required tests**
- [ ] `Inicio_De_Sesion_Correcto_Establece_Sesion_Y_Responde_La_Privada` — login correcto → Set-Cookie de sesión + 302 a `/`; el GET autenticado a `/` devuelve la privada (Bloco 4). Valida AC-03 y FR-02/FR-04.
- [ ] `Rechazo_Es_Identico_Si_El_Usuario_No_Existe_O_La_Contrasena_Es_Incorrecta` — dos peticiones (usuario inexistente, contraseña incorrecta) producen respuestas idénticas: mismo status, mismos headers, mismo body (con el token antiforgery normalizado), ≥2 llamadas independientes. Valida AC-04 y NFR-03.
- [ ] `Cierre_De_Sesion_Invalida_La_Sesion` — logout → siguiente GET `/` exige autenticarse nuevamente. Valida AC-05 y FR-03.
- [ ] `Pagina_Privada_Identifica_A_La_Cuenta_Y_Ofrece_Cerrar_Sesion` — la privada autenticada muestra el nombre de la cuenta y contiene el form de logout. Valida AC-06 y FR-04.
- [ ] `Rechazo_De_Login_No_Establece_Sesion` — tras un rechazo, ninguna cookie de sesión queda emitida. Complementa AC-04.
- [ ] `Login_Ignora_El_ReturnUrl` — una petición a `/Cuenta/IniciarSesion?ReturnUrl=/...` con credenciales correctas redirige a `/`, no al `ReturnUrl`. Cubre el anti-open-redirect.

**Completion criterion**

Login, logout y privada funcionan de extremo a extremo sobre las cuentas reales sembradas por FEAT-001a; un
rechazo de login es indistinguible en los casos AC-04; ningún rechazo establece sesión; la privada no hace
consultas de dominio y no contiene datos médicos.

## Block 3 — Vistas, ViewModel e infraestructura MVC de la capa de presentación

**Files**
- `src/MiArchivoMedico.Web/Views/_ViewImports.cshtml` (nuevo) — `@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers` (habilita el antiforgery automático del FormTagHelper y las asistencias `asp-for`).
- `src/MiArchivoMedico.Web/Views/_ViewStart.cshtml` (nuevo) — `Layout = "_Layout"`.
- `src/MiArchivoMedico.Web/Views/Shared/_Layout.cshtml` (nuevo) — layout mínimo (HTML5, sin wwwroot ni referencia a archivos): renderiza `@RenderBody()`. No introduce JS/CSS externos.
- `src/MiArchivoMedico.Web/Views/Cuenta/IniciarSesion.cshtml` (nuevo) — form de login.
- `src/MiArchivoMedico.Web/Views/Cuenta/Privada.cshtml` (nuevo) — la página privada mínima.
- `src/MiArchivoMedico.Web/ViewModels/IniciarSesionViewModel.cs` (nuevo).

**Logic**

- **`IniciarSesionViewModel`** — **clase** (no `record`), con propiedades:
  - `string? NombreDeUsuario` — `[Required]`, `[MaxLength(256)]`. Prohibido `[EmailAddress]`, `[MinLength]`, `[StringLength]`, `[RegularExpression]`, `[Compare]`.
  - `string? Contrasena` — `[Required]`, `[DataType(DataType.Password)]`.
  - `[Required]` produce los mensajes del framework en español desde las resources de Identity; no hay mensajes propios.
- **`IniciarSesion.cshtml`** — modelo `@model IniciarSesionViewModel`:
  - `asp-validation-summary="ModelOnly"` (muestra solo el error de nivel de modelo, el mensaje único).
  - Campos form con `asp-for` y **sin el `value` de la contraseña**; el input de contraseña usa `asp-for="Contrasena"` que con `[DataType(Password)]` emite `type="password"`. La contraseña escrita NUNCA vuelve al HTML de la respuesta del rechazo (O6).
  - Cada campo con `asp-validation-for`. El borde del campo no distingue entre "usuario no existe" y "contraseña incorrecta": la única diferencia son los errores de validación de entrada, que son idénticos en ambos casos (o ninguno) .
  - Form POST con acción `/Cuenta/IniciarSesion`. Un solo formulario, un solo mecanismo de antiforgery (el FormTagHelper). Prohibido `@Html.AntiForgeryToken()` explícito.
- **`Privada.cshtml`** — saluda con `@Model.NombreDeUsuario` (pasa el nombre desde el controlador) y un form POST a `/Cuenta/CerrarSesion` (botón "Cerrar sesión"). Directiva `[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]` para que ninguna caché del navegador persista la página autenticada (O5). No contiene datos médicos ni datos de otras cuentas.

**Input validation**

El único input es el par `NombreDeUsuario`/`Contrasena` del ViewModel, validado por DataAnnotations: requeridos, longitud máxima 256, contraseña manejada como `DataType.Password`. La contraseña no se persiste en el ViewModel tras un rechazo (propiedad set a `null` al re-renderizar).

**Error handling**

- **E3.1 — La capa de vistas intenta mostrar la contraseña en el re-render.** Prohibido por O6: `Contrasena` se resetea a `null` antes de devolver la vista tras un rechazo. Si `ModelState` conservara el valor, se limpia explícitamente.
- **E3.2 — Error de antiforgery en POST.** El `FormTagHelper` lo resuelve incluyendo el token; la ausencia de token produce ModelState inválido → re-render sin logout ni login.
- **E3.3 — La página autenticada quedaría cacheada.** Prevenido por `[ResponseCache(NoStore)]` en Privada.

**Required tests**
- [ ] `Vista_De_Inicio_De_Sesion_Incluye_El_Token_De_Antiforgery` — el HTML del GET login parsea el input `__RequestVerificationToken`. Valida O3.
- [ ] `La_Contrasena_No_Se_Renderiza_En_El_Rechazo` — tras un rechazo de login, el HTML de la respuesta no contiene el valor de la contraseña enviada. Valida O6 y complementa AC-09.
- [ ] `La_Privada_Ofrece_El_Formulario_De_Cierre_De_Sesion` — el HTML autenticado contiene un form POST a `/Cuenta/CerrarSesion` y la cabecera `Cache-Control: no-store`. Valida AC-06, FR-04 y O5.

**Completion criterion**

La capa Razor arranca (TagHelpers habilitados), el login se ve y rechaza con el mensaje único, la contraseña
jamás se re-renderiza, y la privada identifica a la cuenta y ofrece cerrar sesión sin ningún dato médico.

## Block 4 — Helper de autenticación de pruebas y suite AC-01…AC-10

**Files**
- `tests/MiArchivoMedico.Tests/AutenticacionDePruebas.cs` (nuevo) — helper compartido.
- `tests/MiArchivoMedico.Tests/InicioDeSesionTests.cs` (nuevo) — tests de login/logout (AC-03, AC-04, AC-05, AC-06, AC-09).
- `tests/MiArchivoMedico.Tests/CookieDeAutenticacionTests.cs` (nuevo) — AC-07 y AC-09 (cookie).
- `tests/MiArchivoMedico.Tests/CoberturaTests.cs` (nuevo, opcional si hay un test de humo que mida) — AC-10 (medición en el gate; el archivo de cobertura lo produce el propio `dotnet test`).

**Logic**

`AutenticacionDePruebas` (patrón `ConfiguracionDeAltas` de FEAT-001a, clase estática `internal`):

```csharp
internal static class AutenticacionDePruebas
{
    internal static HttpClient ClienteSobreHttpsDespues(WebApplicationFactory<Program> arranque,
        bool seguirRedirecciones);
    // BaseAddress = new Uri("https://localhost") — la cookie Secure exige HTTPS para viajar (NFR-01).

    internal static Task<ResultadoDeInicioDeSesion> IniciarSesionAsync(
        HttpClient cliente, string usuario, string contrasena);
    // GET /Cuenta/IniciarSesion → parsear __RequestVerificationToken del HTML
    // → POST FormUrlEncodedContent con el token → afirmar Set-Cookie (AC-07) + 302 y Location (AC-03)
    // → devolver ResultadoDeInicioDeSesion(Estado, Ubicacion, CookiesEmitidas).

    internal static Task<HttpClient> CrearClienteAutenticadoAsync(
        AppFactory fabrica, params AltaDeclarada[] altas);
}

internal sealed record ResultadoDeInicioDeSesion(
    HttpStatusCode Estado, string? Ubicacion, IReadOnlyList<string> CookiesEmitidas);
```

- El helper confirma **en cada login correcto** (a) el `Set-Cookie` existe, (b) `Location` es `/` (AC-03), y
  (c) la cookie declara `Secure`, `HttpOnly`, `SameSite=Strict` y **no** `Expires`/`Max-Age` (AC-07).
- AC-04 se prueba con **dos llamadas independientes** (usuario inexistente y contraseña incorrecta de una cuenta
  real) comparando la **respuesta completa**: status, headers relevantes y body **con el token antiforgery
  normalizado** (cada GET login emite un token distinto; se reemplaza el valor del input por un marcador fijo
  antes de comparar).
- AC-09 usa `RegistroEnMemoria` de `ConfiguracionDeAltas` (ampliado en el propio test con
  `ConfigureLogging(registros => registros.AddProvider(registro))`, sin tocar `AppFactory`). El barrido cubre
  la categoría **`Microsoft.Extensions.Identity.Core`** además de `Microsoft.AspNetCore.Identity.*`, con filtro
  de prefijo `categoria == nombre || categoria.StartsWith(nombre + ".")`. Los EventIds 2011/2012 del
  `SignInManager` (login exitoso/fallido) se emiten por **DiagnosticSource**, no por `ILogger`: quedan fuera
  del alcance de un `ILoggerProvider` y se verifican desde la capa de servicio en FEAT-001c; esta prueba cubre
  lo que sí registra `ILogger` (p. ej. `PasswordMismatch`, `UserLockedOut` bajo `Microsoft.Extensions.Identity.Core`).
  El set plano de antivalores incluye: la contraseña correcta, contraseñas candidatas incorrectas, fragmentos
  reconocibles, el corte `Name + "=" + Value` del Set-Cookie, el `PasswordHash!` sembrado y el header Set-Cookie
  completo. `Assert.DoesNotContain` sobre cada mensaje del log.
- Ningún test escribe credenciales, cookies ni hashes en fixtures versionados; los valores de prueba son ficticios.

**Detalle de los tests por AC**

- **AC-01** `Pagina_Privada_Sin_Sesion_Redirige_Al_Inicio_De_Sesion` (Bloque 1): `/` sin sesión → 302 con `Location` al login, sin dato de cuenta en el body.
- **AC-02** `Ruta_Privada_Sin_Sesion_No_Entrega_Contenido` (Bloque 1): una ruta privada arbitraria (p. ej. `/Cuenta/CerrarSesion` por GET o una action hipotética) → 401/403 o redirección al login, jamás el contenido. Con `AllowAutoRedirect=false`.
- **AC-03** `Inicio_De_Sesion_Correcto_Establece_Sesion_Y_Responde_La_Privada` (Bloque 4): login correcto → Set-Cookie + 302 a `/` → GET autenticado a `/` → 200 con la privada identificando a la cuenta.
- **AC-04** `Rechazo_Es_Identico_Si_El_Usuario_No_Existe_O_La_Contrasena_Es_Incorrecta` (Bloque 4): comparación completa de status+headers+body entre los dos rechazos, ≥2 llamadas independientes, sin Set-Cookie emitida.
- **AC-05** `Cierre_De_Sesion_Invalida_La_Sesion` (Bloque 4): logout → siguiente GET `/` vuelve a exigir autenticación.
- **AC-06** `Pagina_Privada_Identifica_A_La_Cuenta_Y_Ofrece_Cerrar_Sesion` (Bloque 4): saludo con el nombre + form de logout + no contiene datos médicos.
- **AC-07** `Cookie_De_Autenticacion_Declara_Secure_HttpOnly_SameSite_Strict_Y_Es_De_Sesion` (Bloque 4): el `Set-Cookie` crudo declara las 3 propiedades y ni `Expires` ni `Max-Age`.
- **AC-08** `Fuera_De_Desarrollo_Redirige_Http_A_Https_Y_Emite_Hsts` (Bloque 1): 307 HTTP→HTTPS + `Location`; shim HSTS cuando `Request.Scheme == https`; HSTS nunca sobre TestServer HTTP.
- **AC-09** `Rechazo_No_Registra_Contrasena_Cookie_Ni_Hash` (Bloque 4): barrido de `RegistroEnMemoria` sobre las categorías de Identity con el set plano de antivalores.
- **AC-10** Cobertura: `coverlet.collector` ya presente, `dotnet test --collect:"XPlat Code Coverage"` produce el archivo; el umbral de 80 % lo verifica el gate `daw-test` en CODE (Camino A). Sin `coverlet.msbuild`, sin threshold en MSBuild.

**Input validation**

No aplica a producción: esta carpeta es solo de tests. Las entradas son los pares usuario/contraseña ficticios
declarados por `AltaDeclarada`, sobre cuentas creadas por la siembra de FEAT-001a.

**Error handling**

- **E4.1 — El helper asume HTTPS y el cliente es HTTP.** `ClienteSobreHttpsDespues` fija `BaseAddress = new Uri("https://localhost")`; cualquier test que envíe la cookie Secure por HTTP fallará al no recibirla (correcto, verifica NFR-amp;01 en la línea real).
- **E4.2 — Un rechazo de login emite sesión por error.** El helper y los tests de AC-04 afirman `Set-Cookie` ausente; si la rama else emitiera cookie, el test lo detecta.
- **E4.3 — El token antiforgery difiere entre los dos rechazos de AC-04.** Se normaliza (reemplazo por marcador fijo) antes de comparar los bodies; el test falla con un diff accionable si algo más difiere.

**Required tests**
- [ ] `Inicio_De_Sesion_Correcto_Establece_Sesion_Y_Responde_La_Privada` (AC-03)
- [ ] `Rechazo_Es_Identico_Si_El_Usuario_No_Existe_O_La_Contrasena_Es_Incorrecta` (AC-04)
- [ ] `Cierre_De_Sesion_Invalida_La_Sesion` (AC-05)
- [ ] `Pagina_Privada_Identifica_A_La_Cuenta_Y_Ofrece_Cerrar_Sesion` (AC-06)
- [ ] `Cookie_De_Autenticacion_Declara_Secure_HttpOnly_SameSite_Strict_Y_Es_De_Sesion` (AC-07)
- [ ] `Rechazo_No_Registra_Contrasena_Cookie_Ni_Hash` (AC-09)
- [ ] Tests de la vista: `Vista_De_Inicio_De_Sesion_Incluye_El_Token_De_Antiforgery`, `La_Contrasena_No_Se_Renderiza_En_El_Rechazo`, `La_Privada_Ofrece_El_Formulario_De_Cierre_De_Sesion` (Bloque 3).
- [ ] Regresión FEAT-001a: los 7 tests de `AltaDeCuentasTests` y `SuperficieHttpTests` pasan sin cambios de proposición.

**Completion criterion**

La suite completa pasa en verde con cobertura medida no inferior al 80 % por el gate `daw-test`. Toda la
superficie de FEAT-001b (pipeline, controlador, vistas, ViewModel, y las regresiones de FEAT-001a reexpresadas)
queda cubierta por los 10 AC del PRD.

## Reversión

FEAT-001b no introduce migraciones de esquema ni cambia el esquema de datos: la cookie de sesión vive en el
navegador y el `AspNetUsers` fue creado enteramente por FEAT-001a. La reversión es un `git revert` del commit
de este sub-ticket sin efectos colaterales sobre la base.

- **Efecto de revertir:** se quita el pipeline de autenticación, la FallbackPolicy, el controlador, las vistas
  y el helper de tests. Las cuentas sembradas permanecen intactas; la aplicación vuelve a una superficie HTTP
  vacía como en FEAT-001a (con la salvedad de que `Program.cs` conserva los registros de Identity que FEAT-001a
  ya incluía).
- **Detalle de regresión:** los 7 tests de `AltaDeCuentasTests` reexpresados a `Redirect` vuelven a `NotFound`
  si se revierte solo FEAT-001b y no FEAT-001a; esto es esperado y no debe "corregirse" en el revert.
- **Indicador para revertir:** una ruta privada entrega contenido sin sesión, la cookie de autenticación no
  declara `Secure`/`HttpOnly`/`SameSite=Strict`, o un login correcto no establece sesión, de forma reproducible
  y no explicable por configuración.

## Final verification

- Los 4 FR del PRD están cubiertos y los 10 AC tienen test en verde.
- Un usuario sin sesión que solicita `/` o cualquier ruta privada es redirigido al login sin recibir ningún
  dato de ninguna cuenta (AC-01, AC-02).
- Un login correcto emite una cookie de sesión y responde la página privada de esa cuenta; un login incorrecto
  —usuario inexistente o contraseña errónea— responde un único mensaje idéntico y no establece sesión (AC-03, AC-04).
- La cookie declara `Secure`, `HttpOnly`, `SameSite=Strict`, sin `Expires` ni `Max-Age` (AC-07).
- El cierre de sesión invalida la sesión y la siguiente petición privada exige volver a autenticarse (AC-05).
- Fuera de Development, HTTP redirige a HTTPS y se emite HSTS (AC-08).
- Ningún log contiene una contraseña, un valor de cookie ni un hash (AC-09).
- La cobertura del código incorporado es ≥ 80 %, verificada por el gate `daw-test` (AC-10).
- Ningún middleware de FEAT-001c (expiración, sesión única, bloqueo) quedó configurado por adelantado.
- No existe ninguna URL que reproduzca un estado autenticado por querystring, ni `returnUrl` honrado, ni
  `MapFallback`, ni rutas públicas distintas de `Cuenta/IniciarSesion` (y los recursos estáticos, si existieran).