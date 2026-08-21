# SAST — FEAT-001a: Arranque de la solución y alta administrativa de cuentas

| Campo | Valor |
|-------|-------|
| Ticket | FEAT-001a |
| Fecha | 2026-08-20 |
| Fase | CODE — cierre |
| Alcance | Los 6 archivos de producción del ticket, más el árbol de dependencias completo |
| Resultado | **PASSED** (tras remediar 1 hallazgo High) |

## Alcance analizado

`src/MiArchivoMedico.Web/Program.cs`, `Data/SqliteLocation.cs`, `Data/AppDbContext.cs`,
`Accounts/AppUser.cs`, `Accounts/AccountSeedOptions.cs`, `Accounts/AccountProvisioner.cs`.
`Migrations/` queda fuera por ser código generado sin entrada de usuario.

## Resultados por categoría

| Regla | Verificación | Resultado |
|---|---|---|
| **F-SAST-01** Secretos embebidos | `appsettings.json` versionado contiene solo `Logging` y `AllowedHosts`: 0 contraseñas, 0 cadenas de conexión, 0 rutas de base. `.gitignore` cubre `*.db`, `*.db-wal`, `*.db-shm` y `appsettings.*.local.json`. El test `Appsettings_Versionado_No_Contiene_Contrasenas` lo verifica sobre el archivo real y bloquea la regresión. | ✅ |
| **F-SAST-02** Inyección SQL | Una sola sentencia literal en todo el código: `PRAGMA journal_mode=WAL;` (`SqliteLocation.cs:75`), constante, sin ninguna entrada de usuario. Todo lo demás pasa por EF Core con parámetros. | ✅ |
| **F-SAST-03** Inyección de comandos | 0 `Process.Start`, 0 `exec`. | ✅ |
| **F-SAST-04** Funciones inseguras | 0 `BinaryFormatter`, 0 `Assembly.Load`, 0 `Activator.CreateInstance`. Deserialización solo por el enlazador de configuración de .NET sobre tipos declarados. | ✅ |
| **F-SAST-05** Path traversal | La única ruta que la aplicación construye es la de la base, y no viene de un usuario final sino de configuración del administrador. Además se resuelve a absoluta y se **rechaza** si cae bajo `ContentRoot`/`WebRoot` (`SqliteLocation.cs:44-53`). Limitación conocida y documentada: no se resuelven enlaces simbólicos. | ✅ |
| **F-SAST-06** XSS | Sin vistas, sin HTML generado, sin superficie HTTP. La única respuesta es texto plano constante. | ✅ |
| **F-SAST-07** SSRF | 0 llamadas salientes. | ✅ |
| **F-SAST-08** Criptografía débil | 0 MD5, 0 SHA1, 0 DES, 0 ECB. Contraseñas con PBKDF2-HMAC-SHA256, `IdentityV3`, 100.000 iteraciones fijadas en código (`Program.cs:42-43`), conforme a NFR-01 y ADR-002. | ✅ |
| **F-SAST-09** Debug en producción | `UseDeveloperExceptionPage` está dentro de `if (app.Environment.IsDevelopment())` (`Program.cs:72-75`); fuera de Development responde 500 con texto genérico. `EnableSensitiveDataLogging` y `EnableDetailedErrors` no aparecen en ningún lado. Verificado por `Fuera_De_Desarrollo_No_Expone_Pagina_De_Error_Detallada`, que comprueba ausencia de traza contra 5 marcadores. | ✅ |
| **F-SAST-10** Datos sensibles en logs | Los 6 `Log*` del provisionador reciben índice, nombre de usuario, constantes de límite y `IdentityResult.Code` — nunca `Description`, nunca la contraseña, nunca el hash. `AccountSeedEntry.ToString()` interpola solo `UserName`. Verificado por `Rechazo_De_Alta_No_Registra_Credenciales`, que afirma sobre todos los mensajes de todas las categorías capturadas, incluidas las de EF Core. | ✅ |
| **F-SAST-11** Carga sin restricción | No hay carga de archivos en este ticket. | ✅ |
| **F-SAST-12** CSRF | No hay formularios ni endpoints que muten estado: la superficie HTTP está vacía. Llega con FEAT-001b. | ✅ |
| **F-SAST-13 / F-SAST-16** Dependencias vulnerables | **1 hallazgo High — remediado.** Detalle abajo. | ✅ tras remediación |
| **F-SAST-14** Validación de entrada incompleta | La configuración de altas se valida en nombre (requerido, 1-256) y contraseña (requerida, ≤128), y el mínimo de 12 lo aplica `PasswordOptions.RequiredLength`. La ruta de la base se valida con tres reglas y sin valor por defecto. Ramas cubiertas al 100 % en `AccountProvisioner`. | ✅ |
| **F-SAST-15** Errores que filtran internos | Fuera de Development, respuesta genérica sin traza ni tipo de excepción. Los mensajes de aborto de arranque nombran la clave de configuración o la regla incumplida, nunca el valor. El de la migración fallida se verifica con un marcador plantado que no debe aparecer. | ✅ |

## Hallazgo remediado

### H-1 — `SQLitePCLRaw.lib.e_sqlite3` 2.1.6 con aviso de severidad High

- **Regla:** F-SAST-13 / F-SAST-16 · **Severidad:** High · **Disposición:** bloqueante, no suprimible.
- **Aviso:** GHSA-2m69-gcr7-jv3q.
- **Origen:** dependencia transitiva de `Microsoft.EntityFrameworkCore.Sqlite` 8.0.11, presente en los dos proyectos.
- **Detección:** `dotnet list package --vulnerable --include-transitive`.
- **Remediación:** actualización de `Microsoft.EntityFrameworkCore.Sqlite`, `Microsoft.EntityFrameworkCore.Design`, `Microsoft.AspNetCore.Identity.EntityFrameworkCore` y `Microsoft.AspNetCore.Mvc.Testing` de 8.0.11 a **8.0.30**, la última de la serie 8.0.x. Arrastra `SQLitePCLRaw` 2.1.6 → **2.1.12**, fuera del rango afectado. Se mantiene el criterio del proyecto de que todo el stack del framework comparta versión.
- **Verificación:** `dotnet list package --vulnerable --include-transitive` → *"no vulnerable packages"* en ambos proyectos. Los 28 tests siguen en verde y el build no emite warnings.
- **Nota:** el ticket permanece en .NET 8 LTS; 8.0.30 es una actualización de parche dentro de la misma banda que fija `global.json`.

## Supresiones

Ninguna. No hubo hallazgos Medium que suprimir, y el único High se remedió.

## Resultado

```
Total: 15 categorías limpias, 1 vulnerabilidad encontrada y remediada
  Critical: 0    High: 1 (remediada)    Medium: 0    Low: 0
Resultado: PASSED
```
