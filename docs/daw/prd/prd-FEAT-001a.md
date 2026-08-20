# PRD FEAT-001a: Arranque de la solución y alta administrativa de cuentas

| Campo | Valor |
|-------|-------|
| Ticket | FEAT-001a |
| Tracker | ninguno |
| Fecha | 2026-08-19 |
| PRD loops | 0 |
| Tier | FEATURE |
| PRD padre | `docs/daw/prd/prd-FEAT-001.md` |
| PRD maestro | `docs/daw/prd/PRD2.md` |

> Sub-ticket `a` de FEAT-001. La fuente única de verdad del alcance sigue siendo
> `docs/daw/prd/PRD2.md`; este PRD reexpresa como FR/NFR/AC propios un subconjunto de sus RF y RNF.
> Ante cualquier discrepancia, prevalece el maestro.

## Contexto y Problema

El repositorio no tiene todavía ninguna línea de código de la aplicación: solo documentación y la
configuración del pipeline. Antes de que exista cualquier pantalla hace falta que la aplicación
arranque, que la persistencia esté creada y que existan cuentas contra las cuales autenticarse.

El grupo familiar no tiene registro abierto de cuentas: el alta y el restablecimiento de contraseña
son procedimientos administrativos fuera de la interfaz (RNF-54 del maestro). Eso traslada al
arranque de la aplicación una responsabilidad que en otros productos vive en una pantalla de
registro: crear las cuentas a partir de configuración externa, hashearlas conforme a la política, y
rechazar las que violen el límite de cuentas o el mínimo de contraseña.

Este sub-ticket no expone ninguna pantalla privada ni ningún inicio de sesión: eso es FEAT-001b. Su
entregable es una aplicación que levanta, crea su esquema, siembra sus cuentas y no filtra
credenciales por ningún lado.

## Objetivos

- Dejar la solución ejecutable: aplicación web, persistencia y proyecto de pruebas.
- Mantener la base de datos y los secretos fuera del repositorio y de toda carpeta pública.
- Crear las cuentas del grupo familiar por procedimiento administrativo, sin registro en la interfaz.
- Almacenar las contraseñas con una función de derivación aceptada por la política del producto.
- Hacer que un arranque repetido sea seguro: sin cuentas duplicadas ni credenciales alteradas.

## Requerimientos Funcionales

- **FR-01**: El sistema debe crear durante el arranque las cuentas declaradas en la configuración externa que todavía no existan, y no debe exponer en la interfaz ninguna ruta de registro, cambio ni recuperación de contraseña.
- **FR-02**: El sistema debe rechazar durante el arranque el alta de una cuenta que supere el máximo definido en NFR-02, informar el límite alcanzado y conservar intactas las cuentas ya existentes.
- **FR-03**: El sistema debe rechazar durante el arranque el alta de una cuenta cuya contraseña no alcance el mínimo definido en NFR-03, e informar el incumplimiento sin incluir la contraseña en el mensaje.
- **FR-04**: El sistema debe crear o actualizar durante el arranque el esquema de la base de datos descrito en NFR-04, antes de intentar cualquier alta de cuenta.

## Requerimientos No Funcionales

- **NFR-01**: Las contraseñas deben almacenarse mediante Argon2id con memoria mínima de 19 MiB, 2 iteraciones y paralelismo 1; o bcrypt con factor de costo mínimo 12; o PBKDF2-HMAC-SHA256 con un mínimo de 100.000 iteraciones. El valor almacenado nunca debe coincidir con la contraseña en claro.
- **NFR-02**: El sistema debe admitir un máximo de 5 cuentas activas.
- **NFR-03**: La contraseña asignada por el procedimiento administrativo debe tener 12 caracteres como mínimo, sin reglas de composición adicionales.
- **NFR-04**: La base de datos SQLite debe operar en modo WAL y residir fuera del repositorio y fuera de toda carpeta pública del servidor, junto con sus archivos `-wal` y `-shm`; el repositorio debe contener 0 secretos, 0 cadenas de conexión y 0 archivos de base de datos versionados.
- **NFR-05**: Los registros técnicos del arranque deben contener 0 contraseñas en claro y 0 hashes de credenciales; el diagnóstico debe apoyarse únicamente en identificadores técnicos internos.
- **NFR-06**: La cobertura de pruebas automatizadas del código incorporado por este sub-ticket debe ser de al menos 80 %.
- **NFR-07**: El arranque debe ser idempotente: 2 arranques consecutivos con la misma configuración deben dejar el mismo conjunto de cuentas, sin duplicados y sin reescribir las credenciales existentes.

## Criterios de Aceptación

- **AC-01 (FR-01)**: WHEN la aplicación arranca con una configuración externa que declara cuentas válidas inexistentes en la base de datos, THE sistema SHALL crearlas y dejarlas disponibles para autenticación.
- **AC-02 (FR-01)**: IF se solicita cualquier ruta de registro, cambio de contraseña o recuperación de contraseña, THEN THE sistema SHALL responder 404 y no crear ni modificar ninguna cuenta.
- **AC-03 (FR-02, NFR-02)**: IF la configuración externa declara una cuenta que elevaría el total por encima de 5 cuentas activas, THEN THE sistema SHALL rechazar esa alta, informar el límite alcanzado y conservar intactas las cuentas ya existentes.
- **AC-04 (FR-03, NFR-03)**: IF la configuración externa declara una cuenta con una contraseña de 11 caracteres o menos, THEN THE sistema SHALL no crear esa cuenta e informar durante el arranque el incumplimiento del mínimo.
- **AC-05 (NFR-01)**: WHEN se inspecciona el valor almacenado de la contraseña de una cuenta creada, THE sistema SHALL exhibir un valor que no coincide con la contraseña en claro y cuyo algoritmo y parámetros corresponden a alguna de las 3 combinaciones enumeradas en NFR-01.
- **AC-06 (FR-04, NFR-04)**: WHEN la aplicación abre la base de datos, THE sistema SHALL operar en modo WAL sobre un archivo ubicado fuera del árbol del repositorio y fuera de toda carpeta servida públicamente.
- **AC-07 (FR-04, NFR-07)**: WHEN la aplicación arranca 2 veces consecutivas con la misma configuración externa, THE sistema SHALL dejar exactamente el mismo conjunto de cuentas, sin duplicados y sin modificar el valor almacenado de las contraseñas existentes.
- **AC-08 (NFR-05)**: IF un alta de cuenta es rechazada durante el arranque, THEN THE sistema SHALL registrar el evento sin incluir la contraseña declarada ni ningún hash de credenciales.
- **AC-09 (NFR-06)**: WHEN se ejecuta la suite de pruebas de la solución, THE sistema SHALL alcanzar una cobertura de al menos 80 % sobre el código incorporado por este sub-ticket.

## Fuera de Alcance

- El inicio de sesión, el cierre de sesión, la página privada y la protección de rutas, que son FEAT-001b.
- La expiración de sesión, la sesión única por cuenta y el bloqueo por intentos fallidos, que son FEAT-001c.
- La entidad Estudio y todo lo relativo a archivos médicos, cifrado, búsqueda y PWA, según el índice del PRD padre.
- El cambio y la recuperación de contraseña desde la interfaz, excluidos del MVP por RNF-54 del maestro.
- La rotación de la clave de cifrado y el fallo de arranque por clave ausente, que llegan con la feature de archivos.

## Riesgos y Mitigaciones

- **Un arranque locuaz filtra credenciales.** El código de siembra es el lugar más probable donde una contraseña termina en un log. Mitigación: NFR-05 y AC-08 lo prohíben, y el mensaje de rechazo solo puede nombrar la cuenta y la regla incumplida.
- **La base de datos termina versionada por descuido.** Un archivo `.db` dentro del árbol del repositorio es el error clásico de un primer commit. Mitigación: NFR-04 y AC-06 lo verifican, y el `.gitignore` debe cubrir `*.db`, `*.db-wal` y `*.db-shm`.
- **Un arranque no idempotente rompe el despliegue.** Reiniciar el proceso es una operación normal; si la siembra duplica cuentas o rehashea contraseñas, cada reinicio degrada el sistema. Mitigación: NFR-07 y AC-07.
- **El límite de 5 cuentas se verifica solo contra la configuración.** Si se cuenta lo declarado en vez de lo persistido, dos arranques sucesivos pueden superar el máximo. Mitigación: AC-03 exige que el rechazo se evalúe contra el total de cuentas activas, no contra la lista declarada.

## Dependencias

- .NET 8 (LTS) y el SDK correspondiente, declarados en `AGENTS.md` § Stack.
- ASP.NET Core Identity para el modelo de cuentas y el hashing exigido por NFR-01.
- Entity Framework Core con proveedor SQLite, para el esquema de FR-04 y la persistencia de NFR-04.
- Configuración externa no versionada (variables de entorno o gestor de secretos) como origen de las altas de FR-01, FR-02 y FR-03 y de la cadena de conexión de NFR-04.
- Proyecto de pruebas de integración ejecutado con `dotnet test`, requerido por NFR-06.
- No depende de ningún otro sub-ticket: es el primero de la cadena a → b → c.
