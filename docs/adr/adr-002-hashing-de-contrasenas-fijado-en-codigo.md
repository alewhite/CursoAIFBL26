# ADR-002: Política de hashing de contraseñas fijada en el código de composición

| Campo | Valor |
|-------|-------|
| Fecha | 2026-08-20 |
| Ticket | FEAT-001a |
| Estado | Aceptado |

## Contexto

NFR-01 de `docs/daw/prd/prd-FEAT-001a.md` admite Argon2id, bcrypt o PBKDF2-HMAC-SHA256 con al menos
100.000 iteraciones, y AC-05 verifica el valor almacenado contra esos parámetros. ASP.NET Core
Identity trae su propio `PasswordHasher<T>` con un valor de iteraciones por defecto que ha cambiado
entre versiones del framework. La pregunta es de dónde sale el número que termina aplicándose.

## Opciones consideradas

### Opción 1: Heredar el default de Identity

- **Pros:** cero código. Los defaults del framework tienden a ser razonables y suben con el tiempo.
- **Contras:** AC-05 quedaría verificando un valor que el producto no controla. Un upgrade de .NET
  que cambie el default puede romper el criterio en silencio, o —peor— un downgrade lo debilita sin
  que nada avise. La política de seguridad del producto no puede depender de la versión del SDK.

### Opción 2: Argon2id mediante un paquete de terceros

- **Pros:** es la primera opción que enumera NFR-01 y la más recomendada hoy en general.
- **Contras:** exige una dependencia fuera del framework. `AGENTS.md` § Dependencias pide justificar
  toda dependencia nueva y evitar complejidad desproporcionada para una aplicación familiar. PBKDF2
  con 100.000 iteraciones ya satisface NFR-01, así que la dependencia no compraría cumplimiento.

### Opción 3: PBKDF2 del framework con parámetros fijados en el código de composición

- **Pros:** sin dependencias nuevas. El parámetro queda en el repositorio, versionado y revisable, y
  AC-05 verifica algo que el producto efectivamente decide.
- **Contras:** cambiar el parámetro exige un despliegue. Para este producto eso es una ventaja.

## Decisión

Opción 3: `PasswordHasherOptions.IterationCount` fijado explícitamente en `Program.cs` en al menos
100.000 iteraciones, con `PasswordHasherCompatibilityMode.IdentityV3`.

**No va en `appsettings.json` ni en una variable de entorno.** Un parámetro de seguridad configurable
desde afuera del repositorio es degradable sin revisión: un despliegue con `IterationCount: 1000`
arrancaría y pasaría todos los tests que no lo inspeccionen. La configuración externa es para
secretos y para lo que cambia entre entornos; esto no es ninguna de las dos cosas.

## Consecuencias

- `Program.cs` configura `PasswordHasherOptions` explícitamente; el default del framework no se usa.
- El mínimo de 12 caracteres de NFR-03 se aplica por `PasswordOptions.RequiredLength`, el mecanismo
  central del stack, y no con una comprobación propia en el provisionador de cuentas.
- Subir las iteraciones en el futuro es un cambio de código con su propio commit y su propio ADR.
- Las contraseñas ya almacenadas no se rehashean al cambiar el parámetro: Identity solo lo hace al
  verificar una contraseña, y en este ticket todavía no hay inicio de sesión.
- No se agrega ninguna dependencia de terceros para criptografía.
