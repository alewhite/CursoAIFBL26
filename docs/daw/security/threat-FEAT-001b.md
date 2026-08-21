# Threat Model FEAT-001b: Inicio y cierre de sesión, y protección de las rutas privadas

| Campo | Valor |
|-------|-------|
| Ticket | FEAT-001b |
| PRD | `docs/daw/prd/prd-FEAT-001b.md` |
| Spec | `docs/daw/specs/spec-FEAT-001b.md` |
| Tier | FEATURE |
| Fecha | 2026-08-21 |
| Resultado | PASSED — 13 amenazas STRIDE analizadas, 0 sin mitigación o riesgo aceptado |
| Ruta del reporte | este archivo |

## Alcance

FEAT-001b instala la frontera de autenticación y autorización: pantalla de inicio de sesión
(GET/POST `/Cuenta/IniciarSesion`), cierre de sesión (POST `/Cuenta/CerrarSesion`), página privada mínima
en `/`, y `FallbackPolicy` que exige usuario autenticado en todas las rutas (NFR-07). La cookie de
autenticación declara `Secure`, `HttpOnly`, `SameSite=Strict`, sin `Expires` ni `Max-Age` (NFR-01, NFR-02).
HTTPS al 100 % con redirect HTTP→HTTPS y HSTS fuera de Development (NFR-04). Fuera de alcance de este
sub-ticket (FEAT-001c): expiración de sesión, sesión única y bloqueo por intentos fallidos.

Componentes del diseño analizados:

1. **Composition Root (`Program.cs`):** registro de Identity (`AddIdentityCore<AppUser>` +
   `AddSignInManager` + `AddEntityFrameworkStores`), cookie (`AddAuthentication(ApplicationScheme).AddCookie`),
   autorización con `FallbackPolicy`, pipeline de middleware de 6 pasos.
2. **`CuentaController`:** GET/POST `IniciarSesion` (`[AllowAnonymous]`, única excepción), POST `CerrarSesion`,
   GET `/` (`Privada`, `[Route("/")]`).
3. **Capa de vistas:** `IniciarSesion.cshtml`, `Privada.cshtml`, `IniciarSesionViewModel`.
4. **Helper de pruebas (`AutenticacionDePruebas`):** login/logout sobre el controlador y las vistas reales.

## Superficies de ataque identificadas

| # | Componente | ¿Acepta input? | ¿Expone datos sensibles? | Nueva autenticación/autenticación? | Integración externa? |
|---|-----------|----------------|--------------------------|------------------------------------|-----------------------|
| S1 | `POST /Cuenta/IniciarSesion` | Sí (usuario + contraseña + token antiforgery) | No en respuesta; sí en tránsito (HTTPS) | Sí (sesión) | No |
| S2 | `GET /Cuenta/IniciarSesion` | Sí (querystring; `returnUrl` se ignora) | No | No | No |
| S3 | `POST /Cuenta/CerrarSesion` | Sí (token antiforgery) | No | Sí (invalida sesión) | No |
| S4 | `GET /` (Privada) | No | Sí (nombre de la cuenta autenticada) | Sí | No |
| S5 | Pipeline de middleware | No (excepto ruta solicitada) | No | Sí (FallbackPolicy) | No |
| S6 | Cookie de autenticación | Indirecto (cada request) | Sí (ticket de sesión) | Sí | No |
| S7 | Vistas Razor | Indirecto (ModelState, ViewModel) | Potencial (re-render de contraseña — prohibido) | No | No |

### Fronteras de confianza declaradas

1. **Navegador ↔ Aplicación** — frontera en la que viaja la cookie `MiArchivoMedico.Auth` y el form de login.
   Protegida por HTTPS (redirect forzado fuera de Development + HSTS) y por la cookie marcada `Secure`
   (la cookie nunca viaja por HTTP sin cifrar, NFR-01).
2. **Usuario anónimo ↔ Usuario autenticado** — frontera en la que la `FallbackPolicy` decide. La única puerta
   de entrada es `POST /Cuenta/IniciarSesion` con credenciales válidas; no existe registro abierto ni
   recuperación de contraseña desde la interfaz.
3. **Cuenta A ↔ Cuenta B (aislamiento de propietario)** — fuera del alcance de FEAT-001b (no hay datos de
   dominio todavía), pero la privada de `/` lee únicamente `User.Identity!.Name` del `ClaimsPrincipal`: una
   cuenta autenticada jamás recibe el nombre de otra cuenta.
4. **Aplicación ↔ Almacenamiento (SQLite, `AspNetUsers`)** — FEAT-001a sembró las cuentas y el hash
   (PBKDF2, adr-002). FEAT-001b no ejecuta consultas de dominio sobre estudios (no existen); el `SignInManager`
   consulta el `UserManager` para verificar credenciales, sin exponer el hash en logs ni en respuestas.

## Análisis STRIDE por componente

### C1 — Composition Root y pipeline (Program.cs)

| STRIDE | Análisis |
|--------|----------|
| **S** | No hay identidad propia del host; el servicio no se hace pasar por un usuario. No aplica directamente. |
| **T** | La configuración de la cookie/cadena es estática en código; la manipulación exigiría comprometer el deployment (fuera del modelo). El pipeline en orden correcto (`UseAuthentication` antes de `UseAuthorization`) previene que la autorización decida sobre una identidad ausente. |
| **R** | Los eventos de `SignInManager` (2011/2012) se emiten por DiagnosticSource; el modelo de logs se cierra con el barrido de AC-09 (sin contraseñas, cookies ni hashes en logs, NFR-05). |
| **I** | La cookie declara `Secure` (nunca viaja por HTTP), `HttpOnly` (no legible por script), `SameSite=Strict`. Sin estos atributos el ticket de sesión quedaría expuesto a scripts (XSS) o CSRF lateral. |
| **D** | La FallbackPolicy redirige todo request no autenticado al login; un no-autenticado no consume recursos de dominio (no hay dominio todavía). El login no tiene ninguna medida de bloqueo en este sub-ticket (FEAT-001c), pero es una página única sin datos sensibles. |
| **E** | Si `UseAuthorization` corriera sin `UseAuthentication`, la política no vería identidad y **todo** intento de acceso privado redirigiría al login (sin escalación, pero funcionalmente roto). El orden correcto es E1.1. No hay ruta de escalación de privilegios: la autorización es un único mecanismo central (NFR-07) y la única excepción es `[AllowAnonymous]` en el par GET/POST login. |

**Riesgos derivados:** T-01 (ver detalle abajo), T-02, T-03, T-04, T-05.

### C2 — CuentaController

| STRIDE | Análisis |
|--------|----------|
| **S** | `PasswordSignInAsync` verifica contra el hash PBKDF2 sembrado por FEAT-001a (adr-002). No existe registro abierto: un atacante no puede crear una cuenta para suplantarla. La rama de usuario inexistente ejecuta el mismo trabajo PBKDF2 dummy (T-06) para no distinguir demora. |
| **T** | El form de login está protegido por antiforgery automático del FormTagHelper (un solo mecanismo, prohibido `@Html.AntiForgeryToken()` explícito). Un POST sin token → ModelState inválido → re-render 200 sin `Location` (E2.4). |
| **R** | La única rama sobre `resultado.Succeeded` no distingue entre usuario inexistente, contraseña incorrecta, cuenta bloqueada o no permitida (NFR-03): no se puede repudiar un rechazo atribuyéndolo a un estado particular de la cuenta. El mensaje único también evita revelar existencia de cuenta. |
| **I** | El rechazo no emite cookie de sesión (E4.2 verificada por AC-04), no incluye `Location`, y el body repite el mismo mensaje único. La contraseña del re-render se resetea a `null` (O6, E3.1): nunca vuelve al HTML. `returnUrl` se ignora siempre: no hay open redirect (test `Login_Ignora_El_ReturnUrl`). |
| **D** | No hay recurso de dominio que un atacante pueda agotar: `/Cuenta/IniciarSesion` es barato. Los intentos exhaustivos sobre credenciales se acotan por el bloqueo de FEAT-001c; este sub-ticket no lo configura (un authenticated request no abusa de nada). |
| **E** | `Privada` solo lee `User.Identity!.Name` del `ClaimsPrincipal`, sin consultas de dominio ni datos de otras cuentas; la fallback policy garantiza que `User.Identity.IsAuthenticated == true` antes de llegar (aislamiento de la frontera 3). |

**Riesgos derivados:** T-06, T-07, T-08, T-09, T-10.

### C3 — Vistas y ViewModel

| STRIDE | Análisis |
|--------|----------|
| **S** | Las vistas no autentican nada; no aplica. |
| **T** | La vista `IniciarSesion.cshtml` usa `asp-for` con TagHelpers; el `FormTagHelper` inyecta el token antiforgery y el `asp-validation-summary="ModelOnly"` muestra el mensaje único. La vista `Privada.cshtml` no renderiza datos médicos ni de otras cuentas. |
| **R** | No aplica (sin acciones auditables en vistas). |
| **I** | Riesgo principal **T-10**: el re-render de la contraseña en el HTML del rechazo. Prevenido por O6 (`Contrasena` reset a `null` y limpieza de `ModelState`) y verificado por `La_Contrasena_No_Se_Renderiza_En_El_Rechazo`. La `Privada` está marcada `[ResponseCache(NoStore)]` (O5) para que ningún caché persistente del navegador guarde la página autenticada. |
| **D** | No aplica. |
| **E** | No aplica. |

**Riesgos derivados:** T-10, T-11.

### C4 — Helper de pruebas (AutenticacionDePruebas)

| STRIDE | Análisis |
|--------|----------|
| **S** | Usa cuentas ficticias declaradas por `AltaDeclarada`; no toca sistemas reales. |
| **T** | Parsa el token antiforgery del HTML real del GET login (mismo flujo que el navegador); cualquier cambio de mecanismo antiforgery rompe el helper (detector, no riesgo). |
| **R** | Los valores de prueba son ficticios; no se escriben credenciales, cookies ni hashes en fixtures versionados. |
| **I** | El helper afirma de forma explícita la ausencia de `Set-Cookie` en los rechazos (AC-04) y la presencia/ausencia de atributos de cookie (AC-07). |
| **D** | No aplica. |
| **E** | No aplica. |

**Riesgos derivados:** ninguno nuevo; el helper es verificador.

## Clasificación de datos sensibles (F-TM-05)

| Dato | Clasificación | Respaldo |
|------|---------------|----------|
| Contraseña del login | Credencial | Nunca en logs, nunca en respuestas (O6), solo en tránsito vía HTTPS (NFR-01/04). Hash PBKDF2 en `AspNetUsers` (adr-002) — fuera de alcance de FEAT-001b (FEAT-001a). |
| Cookie `MiArchivoMedico.Auth` | Credencial de sesión | `Secure` + `HttpOnly` + `SameSite=Strict`, de sesión, sin `Expires`/`Max-Age` (NFR-01, NFR-02, AC-07). |
| Hash de contraseña | Credencial at-rest | Ningún log ni vista puede contenerlo (NFR-05, AC-09). |
| Nombre de la cuenta autenticada | PII mínima | Único dato mostrado en la privada; extraído del `ClaimsPrincipal`, no de la base. Aislamiento por propietario: la privada jamás muestra otro nombre de cuenta. |
| Título, fecha, profesional, institución, descripción, etiquetas, archivos originales | Datos médicos | **No existen todavía** en FEAT-001b; no aparecen en ninguna vista, controlador, log o respuesta. |

## Cifrado (F-TM-07)

- **En tránsito (PII y credenciales):** 100 % HTTPS. Redirect HTTP→HTTPS y HSTS fuera de Development
  (AC-08), cookie `Secure` que jamás viaja por HTTP. No se degrada `SecurePolicy` en ningún entorno,
  incluidos los tests (NFR-01 no admite excepciones).
- **En reposo (credenciales):** el hash PBKDF2 de contraseñas esté en `AspNetUsers` (FEAT-001a, adr-002).
  La cookie de autenticación es un ticket firmado por el Data Protection de ASP.NET Core (registrado por
  `AddIdentityCore`); no se persiste en reposo fuera de lo que el navegador mantiene en memoria para la
  sesión. Los archivos médicos (AES-256) son FEAT posterior, fuera de este alcance.

## Matriz de riesgos (STRIDE)

| # | Riesgo | Categoría | Data flow | Likelihood | Impact | Mitigación | Estado |
|---|--------|-----------|-----------|------------|--------|------------|--------|
| T-01 | Cookie de autenticación sin `Secure` viaja por HTTP y puede ser interceptada | I | S6 | Medium | High | `Cookie.SecurePolicy = CookieSecurePolicy.Always` (sin excepciones por entorno); redirect HTTP→HTTPS + HSTS fuera de Development. Bloque 1. | ✅ Mitigado |
| T-02 | Cookie legible por script mediante XSS → robo de sesión (session hijacking) | I | S6, S5 | Medium | High | `Cookie.HttpOnly = true`. Bloque 1. | ✅ Mitigado |
| T-03 | CSRF sobre POST de logout: un atacante cierra la sesión de un usuario autenticado enviando el form desde otro origen | T | S3 | Medium | Medium | `SameSite=Strict` en la cookie (bloquea el envío cross-site) + antiforgery automático del `FormTagHelper` en el form de logout. Bloque 1 + Bloque 3. | ✅ Mitigado |
| T-04 | Detrás de un proxy, HTTPS se degrada a HTTP o se pierde la cabecera HSTS | M | S5 | Low | Medium | `UseForwardedHeaders` para proxies confiables fuera de Development (configura por deployment, no en código); HSTS `max-age` largo cuando `Request.Scheme == https`. Bloque 1. **Observación M1 cerrada** en spec (Alcance del middleware). | ✅ Mitigado |
| T-05 | FallbackPolicy mal configurada (o `UseAuthorization` sin `UseAuthentication`) deja rutas privadas accesibles o redirige todo al login | E | S5 | Low | High | Orden fijo del pipeline de 6 pasos (E1.1), verificado por AC-01 y AC-02. Un único mecanismo central (NFR-07). Bloque 1. | ✅ Mitigado |
| T-06 | El login anti usuario inexistente ejecuta antes que la verificación PBKDF2 → demora observable que revela existencia de cuenta | I | S1, C2 | Medium | Medium | Rama dummy que ejecuta PBKDF2 con la misma carga en la rama de usuario inexistente. Bloque 1 + Bloque 2. **Observación M2 cerrada** en spec (Bloque 2, rama dummy). | ✅ Mitigado |
| T-07 | Enumeración de cuentas por mensaje de error distinto en el login | I | S1 | Medium | High | Un único mensaje `ModelState.AddModelError(string.Empty, "...")` en la única rama else; prohibido ramificar sobre `IsLockedOut`/`IsNotAllowed`/`RequiresTwoFactor`. NFR-03, AC-04. Bloque 2. | ✅ Mitigado |
| T-08 | Enumeración de cuentas por demora en el login (timing) | I | S1 | Low | Medium | Diferido a FEAT-001c junto al bloqueo; el dummy PBKDF2 (T-06) cubre la mayor parte de la superficie ya en este sub-ticket. **Observación M3 cerrada** como riesgo aceptado en spec (Límites de FEAT-001b). | ✅ Aceptado (documentado) |
| T-09 | Open redirect vía `returnUrl` tras un login exitoso | R | S2, C2 | Medium | Medium | `returnUrl` **se ignora siempre** (no se lee, no se valida, no se usa) → solo se redirige a `/`. Test `Login_Ignora_El_ReturnUrl`. Bloque 2. | ✅ Mitigado |
| T-10 | La contraseña se re-renderiza en el HTML del rechazo de login | I | S7, C2 | Medium | High | `Contrasena` reset a `null` antes de re-renderizar + limpieza de `ModelState`; input sin `value`. Verificado por `La_Contrasena_No_Se_Renderiza_En_El_Rechazo`. NFR-05, AC-09. Bloque 3. | ✅ Mitigado |
| T-11 | La página autenticada (privada) queda cacheada por el navegador o un intermediario | I | S4 | Low | Medium | `[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]` en la vista privada (O5). Bloque 3. | ✅ Mitigado |
| T-12 | El error detallado de producción filtra traza o datos → información sensible (excepción con credenciales) | I | S5, S7 | Low | High | `UseExceptionHandler` + manejador genérico fuera de Development (heredado de FEAT-001a, conservado); respuesta 500 con texto fijo sin traza. Verificado por `Fuera_De_Desarrollo_No_Expone_Pagina_De_Error_Detallada`. Bloque 1. | ✅ Mitigado |
| T-13 | Bloqueo por intentos fallidos ausente permite fuerza bruta sobre credenciales | D | C2 | Low | Medium | Fuera de alcance de FEAT-001b (FEAT-001c configura `LockoutOptions`). Documentado como riesgo aceptado con su alcance. **Observación M3 cerrada** en spec (Alcance del middleware). | ✅ Aceptado (documentado) |

### Detalle de los riesgos aceptados (F-TM-04)

Ninguno requiere aprobación del usuario con los tres campos formales: las dos anotaciones "Aceptado" (T-08, T-13)
son **límites de alcance explícitos del PRD** (L74-78 del PRD FEAT-001b: expiración/sesión única/bloqueo →
FEAT-001c), ya aprobados en DEFINE. El control de la demora observable y del bloqueo se cierra en FEAT-001c;
mientras tanto, T-06 mitiga la superficie de timing más grande y AC-04 verifica la indistinguibilidad de
mensaje/status/headers.

## Mitigaciones plegadas en la spec

1. **Bloque 1:** cookie `Secure`+`HttpOnly`+`SameSite=Strict` sin `Expires`/`Max-Age` (T-01, T-02, T-03);
   orden fijo del pipeline (T-05); `UseForwardedHeaders` por configuración (T-04); dummy PBKDF2 en rama de
   usuario inexistente (T-06, Bloque 1+2); manejador genérico de excepciones (T-12).
2. **Bloque 2:** una única rama `else` con mensaje único (T-07); `returnUrl` ignorado siempre (T-09);
   `lockoutOnFailure: false` (T-13, no adelantar FEAT-001c).
3. **Bloque 3:** reset de `Contrasena` a `null` + sin `value` (T-10); `[ResponseCache(NoStore)]` en privada
   (T-11); un solo mecanismo de antiforgery via FormTagHelper (T-03).
4. **Bloque 4:** verificación de ausencia de `Set-Cookie` en rechazo (T-07), barrido de logs sin
   contraseña/cookie/hash (T-10, NFR-05), y normalización del token antiforgery en AC-04.

## Resultado

```
┌─────────────────────────────────────────────────────────┐
│  /daw-threat-modeling — PASSED                           │
├─────────────────────────────────────────────────────────┤
│  Superficies de ataque identificadas: 7                  │
│  Fronteras de confianza declaradas: 4                    │
│  Componentes STRIDE: 4                                   │
│  Riesgos: C:0 H:4 M:7 L:2   (13 total; 11 mitigados,     │
│           2 aceptados por límite de alcance del PRD)     │
│  Mitigaciones plegadas en la spec: 4 bloques (ver arriba)│
└─────────────────────────────────────────────────────────┘
```