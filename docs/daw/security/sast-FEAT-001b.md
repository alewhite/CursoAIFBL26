# Reporte SAST — FEAT-001b: Inicio y cierre de sesión, y protección de rutas privadas

| Campo | Valor |
|---|---|
| Ticket | FEAT-001b |
| Fecha | 2026-08-22 |
| Revisor | Validación automática + análisis de código |
| Resultado | **PASSED** — 0 vulnerabilidades críticas/altas |

---

## Resumen ejecutivo

Bloque 1 de FEAT-001b (Composition Root de autenticación) completó el escaneo SAST sin hallazgos críticos ni altos.

- ✅ **F-SAST-01 (Secretos hardcodeados):** No se hallaron claves, tokens, contraseñas ni cadenas de conexión en el código fuente. Archivos `.env` no están versionados (`.gitignore` válido). ✓
- ✅ **F-SAST-02 (SQL Injection):** No se usa string concatenation en queries. Todas las consultas van por Entity Framework Core con parámetros. ✓
- ✅ **F-SAST-03 (Command Injection):** No hay `Runtime.exec()`, `Process.Start()` o equivalentes. ✓
- ✅ **F-SAST-04 (Eval/Exec):** No hay `eval()` ni desserialización insegura. ✓
- ✅ **F-SAST-05 (Path Traversal):** Rutas de base de datos y archivos resueltas desde configuración externa, no desde entrada del usuario. ✓
- ✅ **F-SAST-06 (XSS):** Sin vistas Razor aún (Bloque 3). No hay `Html.Raw()` ni `innerHTML`. ✓
- ✅ **F-SAST-07 (SSRF):** No hay llamadas a servicios externos no validados. ✓
- ✅ **F-SAST-08 (Criptografía débil):** Hashing con PBKDF2-SHA256, 100.000 iteraciones, modo IdentityV3 (conforme a NFR-01). Cobertura de datos sensibles (contraseñas, hashes, cookies) no registrada. ✓
- ✅ **F-SAST-09 (Debug mode en producción):** Pipeline condicional para Development vs. Production (HSTS, redirección HTTPS). ✓
- ✅ **F-SAST-10 (Logging de datos sensibles):** Ningún log contiene credenciales, hashes, valores de cookie ni resultados médicos. Verificado en el controlador de pruebas: sanitización de errores antes de exponerlos (AC-09). ✓
- ✅ **F-SAST-11 (Upload sin restricciones):** No implementado en Bloque 1 (pertenece a FEAT-002). ✓
- ✅ **F-SAST-12 (CSRF):** Antiforgery automático via FormTagHelper (entra en Bloque 3). Program.cs habilita `AddControllersWithViews()`. ✓
- ✅ **F-SAST-13/16 (Dependencias vulnerables):** `dotnet list package --vulnerable` → 0 CVEs detectadas. ✓
- ✅ **F-SAST-14 (Validación incompleta):** Entrada limitada en Bloque 1 (solo configuración externa). Bloque 2 y 3 incluirán validación de `NombreDeUsuario`, `Contrasena` y metadatos. ✓
- ✅ **F-SAST-15 (Error leaking):** Manejador genérico de excepciones fuera de Development (`UseExceptionHandler`). Respuesta 500: "Se produjo un error inesperado." sin traza, sin tipo de excepción, sin datos médicos (AC-08). ✓

---

## Detalles técnicos

### Configuración de seguridad (Program.cs)

- **Cookie:** `Secure`, `HttpOnly`, `SameSite=Strict`, sin `Expires` (sesión del navegador).
- **Autenticación:** `AddIdentityCore<AppUser>` + `AddSignInManager()` (no `AddIdentity<,>()`, que traería cookies de 14 días deslizantes).
- **Autorización:** `FallbackPolicy = RequireAuthenticatedUser()` (NFR-07, mecanismo central único).
- **Password Hasher:** `CompatibilityMode = IdentityV3`, `IterationCount = 100_000` (ADR-002).

### Gestión de configuración

- **Ubicación SQLite:** `SqliteLocation.Resolver()` desde configuración externa, no hardcodeada.
- **Altas de cuentas:** `AccountSeedOptions` desde variables de entorno o user-secrets, jamás versionadas.
- **HTTPS:** Redirect + HSTS solo fuera de Development (`if (!app.Environment.IsDevelopment())`).

### Datos sensibles en tests

- Sin credenciales reales en fixtures versionados.
- Contraseñas de prueba son ficticias (`ConfiguracionDeAltas.Alta()` genera valores aleatorios).
- `RegistroEnMemoria` verifica que logs NO contienen: contraseña, hash, valores de cookie, nombres de archivo médico.

---

## Hallazgos (Total: 0)

No hay hallazgos críticos, altos ni medios sin suprimir.

---

## Disposición

✅ **PASSED** — Bloque 1 avanza a VERIFY sin bloqueadores de seguridad.

---

## Notas para Bloque 2 y 3

- **Bloque 2:** Validar entrada en `CuentaController` (NombreDeUsuario, Contrasena). Verificar que rechazo de login es indistinguible (AC-04, NFR-03).
- **Bloque 3:** Vistas Razor con FormTagHelper (antiforgery automático, AC-03), input sin renderizar (AC-06).
- **Bloque 4:** Tests de seguridad (AC-07, AC-09): cookie attributes, ausencia de credenciales en logs.

---

**Gate SAST:** ✅ TRUE
