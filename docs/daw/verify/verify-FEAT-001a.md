# Verificación — FEAT-001a: Arranque de la solución y alta administrativa de cuentas

| Campo | Valor |
|-------|-------|
| Ticket | FEAT-001a |
| Fecha | 2026-08-21 |
| Resultado | **PASS** |
| Verificador | `daw-module-verifier` |

## Trazabilidad PRD → Código → Test

Los 4 FR, 7 NFR y 9 AC de `docs/daw/prd/prd-FEAT-001a.md` tienen implementación y al menos un test
real. Ninguno quedó sin cobertura. Detalle completo en el reporte del agente (abajo, íntegro).

- **FR-01 a FR-04**: `AccountProvisioner`, `Program.cs`, `SqliteLocation` — verificados por
  `Siembra_Crea_Las_Cuentas_Declaradas_Que_No_Existen`, `Rutas_De_Registro_Y_De_Contrasena_Responden_404`,
  `Rechaza_El_Alta_Que_Supera_El_Maximo_De_Cuentas`, `El_Maximo_De_Cuentas_Se_Cuenta_Contra_Las_Persistidas`,
  `Rechaza_El_Alta_Con_Contrasena_Menor_A_12_Caracteres`, `Aplica_Las_Migraciones_Y_Crea_El_Esquema`.
- **NFR-01 a NFR-07**: hashing (`Hash_Almacenado_Usa_Pbkdf2...`), límite de cuentas (test que
  discrimina persistidas de declaradas), WAL y ubicación de la base, no filtración de credenciales,
  cobertura ≥80 % medida en la corrida real, siembra aditiva verificada en dos arranques.
- **AC-01 a AC-09**: los nueve con test dedicado, varios con aserciones positivas deliberadas que
  descartan implementaciones vacías (AC-01, AC-05, AC-08).

## Riesgo aceptado A-1 (base SQLite sin cifrar en reposo)

Sigue vigente con el código final. Ninguna condición de revisión se disparó por este ticket.

## Threat model y ADR

Las 13 mitigaciones (M-1 a M-13) están implementadas en el código que quedó, no en el planeado. Los
3 ADR se respetaron al pie de la letra: migraciones EF Core (no `EnsureCreated`), hashing fijado en
código y verificado por un test que discrimina de verdad (rompe si se reemplaza por configuración
externa), siembra aditiva sin ninguna ruta HTTP de alta.

## Frontera con FEAT-001b y FEAT-001c

Verificada por grep sobre el código, no por los comentarios: 0 ocurrencias de
`UseAuthentication`, `UseAuthorization`, `ConfigureApplicationCookie`, `UseHttpsRedirection`,
`UseHsts`, `LockoutOptions`, `ExpireTimeSpan`, `SlidingExpiration`, `AddIdentity<,>` fuera de un
comentario que explica por qué no se usa, y 0 rutas mapeadas.

## Interacción entre bloques

Orden de arranque sin condición de carrera: WAL se fija antes de `Migrate()`, y `Migrate()` antes de
la siembra, todo secuencial. El supuesto del Bloque 2 (`DbContext` scoped sin pooling, para no
cerrarle la puerta al futuro filtro por propietario de RNF-53) no fue invalidado por el Bloque 3.

## Suite, cobertura y dependencias

28/28 tests verdes. Cobertura de esta corrida: 98,56 % líneas / 91,66 % ramas (el reporte de cierre
de CODE citó 98,21 % líneas; la diferencia de 0,35 puntos se atribuye a orden de ejecución entre
corridas, no a una regresión — ambas muy por encima del 80 % de NFR-06). `dotnet list package
--vulnerable` no reporta ninguna dependencia vulnerable, confirmando la remediación del commit
`3ab3574`.

## Datos sensibles

Barrido completo de producción y tests: 0 contraseñas, hashes o secretos reales hardcodeados. Los
datos de prueba se generan por función y están explícitamente declarados como ficticios.

## Veredicto

```
FAILs: 0 | WARNs: 1 (discrepancia menor de cobertura, no bloqueante) | PASSes: 33
Resultado: PASS
```
