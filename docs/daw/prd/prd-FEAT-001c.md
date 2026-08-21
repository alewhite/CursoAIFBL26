# PRD FEAT-001c: Endurecimiento de sesión y bloqueo por intentos fallidos

| Campo | Valor |
|-------|-------|
| Ticket | FEAT-001c |
| Tracker | ninguno |
| Fecha | 2026-08-19 |
| PRD loops | 0 |
| Tier | FEATURE |
| PRD padre | `docs/daw/prd/prd-FEAT-001.md` |
| PRD maestro | `docs/daw/prd/PRD2.md` |

> Sub-ticket `c` de FEAT-001. La fuente única de verdad del alcance sigue siendo
> `docs/daw/prd/PRD2.md`; este PRD reexpresa como FR/NFR/AC propios un subconjunto de sus RF y RNF.
> Ante cualquier discrepancia, prevalece el maestro.

## Contexto y Problema

FEAT-001b entrega una sesión que funciona pero que no caduca, que puede estar abierta en cuantos
dispositivos se quiera y que no opone resistencia a un adversario que pruebe contraseñas una tras
otra. Los tres huecos son los que el maestro cierra con RNF-04, RNF-05, RNF-60, RNF-65 y RNF-68.

Son requerimientos temporales, y esa es su dificultad: una sesión que expira a los 30 minutos, un
bloqueo que dura 15 minutos y una duración absoluta de 24 horas no se pueden probar esperando. Por
eso el tiempo tiene que ser una dependencia inyectable y no una lectura directa del reloj del
sistema.

La otra dificultad es la indistinguibilidad. Un bloqueo que responde distinto —en texto o en
demora— delata qué nombres de usuario existen y cuáles están bloqueados, que es exactamente lo que
RNF-13 y RNF-65 del maestro prohíben.

## Objetivos

- Acotar la vida de una sesión, tanto por inactividad como en términos absolutos.
- Limitar cada cuenta a una única sesión activa, invalidando la anterior del lado del servidor.
- Frenar la fuerza bruta sin revelar qué cuentas existen ni cuáles están bloqueadas.
- Dejar las tres reglas temporales verificables de forma determinista, sin esperas reales.

## Requerimientos Funcionales

- **FR-01**: El sistema debe invalidar la sesión tras el período de inactividad definido en NFR-01 y exigir una nueva autenticación.
- **FR-02**: El sistema debe invalidar la sesión al alcanzar la duración absoluta definida en NFR-02 y exigir una nueva autenticación, con independencia de la actividad registrada.
- **FR-03**: El sistema debe invalidar la sesión previa de una cuenta cuando esa misma cuenta inicia sesión nuevamente, en el mismo dispositivo o en otro.
- **FR-04**: El sistema debe rechazar todo intento de inicio de sesión de un nombre de usuario bloqueado según NFR-03, incluso cuando la contraseña presentada sea correcta.
- **FR-05**: El sistema debe reiniciar a cero el contador de intentos fallidos de un nombre de usuario cuando se produce un inicio de sesión exitoso.

## Requerimientos No Funcionales

- **NFR-01**: La sesión debe expirar tras 30 minutos sin actividad del usuario.
- **NFR-02**: La sesión debe expirar 24 horas después de su inicio, con independencia de la actividad registrada.
- **NFR-03**: El sistema debe bloquear un nombre de usuario que acumule 5 intentos fallidos dentro de una ventana de 15 minutos, y debe mantener el bloqueo durante 15 minutos contados desde el quinto intento fallido.
- **NFR-04**: El contador de intentos fallidos debe llevarse contra el nombre de usuario ingresado, exista o no la cuenta, y la mediana del tiempo de respuesta de un rechazo medida sobre 20 intentos no debe diferir en más de 100 ms entre una cuenta existente y una inexistente.
- **NFR-05**: El sistema debe devolver 1 único mensaje de rechazo para usuario inexistente, contraseña incorrecta y cuenta bloqueada, sin distinguir entre los 3 casos.
- **NFR-06**: Cada cuenta debe tener como máximo 1 sesión activa, y la validez de una sesión debe verificarse del lado del servidor en cada solicitud, de modo que una cookie emitida antes de la invalidación no sea aceptada.
- **NFR-07**: Las 3 reglas temporales definidas en NFR-01, NFR-02 y NFR-03 deben resolverse contra 1 fuente de tiempo inyectable, de modo que puedan probarse sin esperas reales.
- **NFR-08**: La cobertura de pruebas automatizadas del código incorporado por este sub-ticket debe ser de al menos 80 %.

## Criterios de Aceptación

- **AC-01 (FR-01, NFR-01, NFR-07)**: WHEN transcurren 30 minutos sin actividad de una sesión y el usuario emite una nueva solicitud, THE sistema SHALL exigir una nueva autenticación.
- **AC-02 (FR-02, NFR-02, NFR-07)**: WHEN una sesión alcanza 24 horas desde su inicio y el usuario emite una nueva solicitud, THE sistema SHALL exigir una nueva autenticación, aunque haya habido actividad continua.
- **AC-03 (FR-03, NFR-06)**: WHEN una cuenta con sesión activa en un dispositivo inicia sesión en un segundo dispositivo, THE sistema SHALL invalidar la primera sesión, de modo que su siguiente solicitud exija autenticarse nuevamente.
- **AC-04 (FR-03, NFR-06)**: IF se presenta la cookie de una sesión invalidada por un inicio de sesión posterior de la misma cuenta, THEN THE sistema SHALL rechazarla y exigir una nueva autenticación, aunque la cookie no haya vencido.
- **AC-05 (FR-04, NFR-03, NFR-05)**: IF un nombre de usuario acumula 5 intentos fallidos dentro de una ventana de 15 minutos y presenta la contraseña correcta antes de que transcurran 15 minutos desde el quinto fallo, THEN THE sistema SHALL rechazar el acceso con el mismo mensaje que devuelve ante credenciales inválidas.
- **AC-06 (FR-04, NFR-03, NFR-07)**: WHEN transcurren 15 minutos desde el quinto intento fallido y la cuenta presenta la contraseña correcta, THE sistema SHALL conceder el acceso.
- **AC-07 (FR-05, NFR-03)**: WHEN una cuenta acumula 4 intentos fallidos, inicia sesión con éxito y a continuación acumula otros 4 intentos fallidos, THE sistema SHALL mantener la cuenta desbloqueada.
- **AC-08 (NFR-04, NFR-05)**: IF se realizan 5 intentos fallidos y luego un sexto sobre un nombre de usuario inexistente, THEN THE sistema SHALL devolver un rechazo cuyo mensaje es idéntico y cuya mediana de tiempo de respuesta, medida sobre 20 intentos, no difiere en más de 100 ms respecto de una cuenta existente en la misma situación.
- **AC-09 (NFR-08)**: WHEN se ejecuta la suite de pruebas de la solución, THE sistema SHALL alcanzar una cobertura de al menos 80 % sobre el código incorporado por este sub-ticket.

## Fuera de Alcance

- El alta de cuentas y la política de contraseñas, entregadas por FEAT-001a.
- El inicio de sesión, el cierre de sesión, la página privada y la protección de rutas, entregados por FEAT-001b.
- El bloqueo por dirección IP o por dispositivo: el maestro define el bloqueo contra el nombre de usuario ingresado (RNF-60, RNF-65) y no admite otro criterio.
- La notificación al usuario de que su sesión fue cerrada desde otro dispositivo.
- La entidad Estudio y todo lo relativo a archivos médicos, cifrado, búsqueda y PWA, según el índice del PRD padre.

## Riesgos y Mitigaciones

- **La sesión única deja sesiones huérfanas válidas.** Si la invalidación se resuelve borrando la cookie del cliente nuevo, la cookie vieja sigue siendo aceptada por el servidor. Mitigación: NFR-06 exige verificación del lado del servidor contra un marcador de sesión por cuenta, y AC-04 lo prueba presentando explícitamente la cookie vieja.
- **El umbral de 100 ms de NFR-04 es un supuesto de este PRD.** El maestro exige que la demora sea "comparable" (RNF-65) sin fijar un número, y la validación exige una métrica. Se adopta 100 ms sobre medianas de 20 intentos; si se prefiere otro umbral, cambia el número y no el requerimiento.
- **La medición temporal es inestable en CI compartido.** Mitigación: comparar medianas en vez de valores individuales, y ejecutar el mismo trabajo costoso —verificación de hash o equivalente— también cuando la cuenta no existe.
- **El bloqueo por nombre de usuario habilita una denegación de servicio dirigida.** Quien conozca un nombre de usuario puede mantenerlo bloqueado. Riesgo aceptado: es lo que exige RNF-60 del maestro para una instalación familiar de hasta 5 cuentas, y bloquear por IP rompería la indistinguibilidad de RNF-65.
- **Un reloj tomado directamente del sistema vuelve las pruebas lentas o frágiles.** Mitigación: NFR-07 exige una fuente de tiempo inyectable, usada por las 3 reglas temporales sin excepción.

## Dependencias

- FEAT-001b: inicio de sesión, cierre de sesión y rutas privadas protegidas. Sin ellos no hay sesión que endurecer.
- FEAT-001a: cuentas creadas, sobre las que se cuentan los intentos fallidos.
- ASP.NET Core Identity para el conteo de intentos fallidos y el bloqueo de NFR-03.
- La autenticación por cookie de ASP.NET Core y su validación por solicitud, para NFR-01, NFR-02 y NFR-06.
- Una abstracción de tiempo inyectable disponible para la aplicación y las pruebas, requerida por NFR-07.
- Proyecto de pruebas de integración ejecutado con `dotnet test`, requerido por NFR-08.
