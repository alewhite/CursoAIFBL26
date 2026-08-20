# PRD padre FEAT-001: Autenticación y arranque de la aplicación

| Campo | Valor |
|-------|-------|
| Ticket | FEAT-001 |
| Tracker | ninguno |
| Fecha | 2026-08-19 |
| Estado | Dividido |
| Tier | FEATURE |
| PRD maestro | `docs/daw/prd/PRD2.md` |

> Este documento dejó de ser un PRD ejecutable: pasó a ser el índice de la división. El PRD completo
> que lo ocupaba —12 FR, 15 NFR, 22 AC, validado PASSED— se repartió sin pérdida entre los tres
> sub-PRD de abajo. Cada uno se ejecuta como un ticket propio, con su rama y su pipeline completo.

## Sub-tickets

| Sub-ticket | Título | PRD | Dependencias | Estado |
|---|---|---|---|---|
| FEAT-001a | Arranque de la solución y alta administrativa de cuentas | `prd-FEAT-001a.md` | ninguna | activo |
| FEAT-001b | Inicio y cierre de sesión, y protección de las rutas privadas | `prd-FEAT-001b.md` | depende de a | pendiente |
| FEAT-001c | Endurecimiento de sesión y bloqueo por intentos fallidos | `prd-FEAT-001c.md` | depende de b | pendiente |

## Orden de implementación sugerido

a → b → c

No hay orden alternativo posible: no existe inicio de sesión sin cuentas creadas, ni expiración,
sesión única o bloqueo sin un inicio de sesión que endurecer.

## Contexto original

El repositorio no tenía ninguna línea de código de la aplicación. La primera rebanada vertical
elegida fue la autenticación, porque RF-01 del PRD maestro exige autenticación antes de mostrar
cualquier dato médico, y toda funcionalidad posterior se apoya en una identidad autenticada y
aislada por cuenta (RNF-53). Construir cualquier otra rebanada primero habría levantado igualmente
la solución, la persistencia y las pruebas, pero habría instalado la invariante de privacidad
después de que ya existiera código capaz de saltearla.

El control de alcance de DEFINE detectó 22 criterios de aceptación sobre cuatro áreas distintas
—arranque y persistencia, alta administrativa de cuentas, puerta de entrada, endurecimiento de
sesión— muy por encima del umbral de 5 a 7. Los tres sub-tickets son entregables por separado: **a**
deja la aplicación arrancando con las cuentas creadas y las contraseñas hasheadas; **b** agrega la
puerta de entrada y cierra el acceso anónimo; **c** endurece la sesión que **b** dejó funcionando.

## Alcance excluido de FEAT-001 y sus sub-tickets

Estos puntos del PRD maestro no pertenecen a ninguno de los tres sub-tickets y esperan sus propias
features:

- La entidad Estudio, su alta, edición, eliminación y el listado real (RF-07 a RF-15, RF-29 a RF-37).
- La carga, validación, cifrado AES-256, visualización y descarga de archivos, y el fallo de arranque
  por clave de cifrado ausente (RNF-02, RNF-58, RNF-62).
- La búsqueda, los filtros, la paginación y los estados de listado vacío (RF-16 a RF-23, RF-38 a RF-40).
- Todo lo relativo a PWA (RF-24, RF-26, RF-28, RNF-51).
- El aislamiento por propietario sobre estudios y archivos (RNF-53), que no tiene datos sobre los que
  aplicar hasta que exista la entidad Estudio.
- Las URLs temporales de acceso a archivos y su expiración de 5 minutos (RNF-07).
- Los respaldos de infraestructura y la restauración.
