# PRD FEAT-001b: Inicio y cierre de sesión, y protección de las rutas privadas

| Campo | Valor |
|-------|-------|
| Ticket | FEAT-001b |
| Tracker | ninguno |
| Fecha | 2026-08-19 |
| PRD loops | 0 |
| Tier | FEATURE |
| PRD padre | `docs/daw/prd/prd-FEAT-001.md` |
| PRD maestro | `docs/daw/prd/PRD2.md` |

> Sub-ticket `b` de FEAT-001. La fuente única de verdad del alcance sigue siendo
> `docs/daw/prd/PRD2.md`; este PRD reexpresa como FR/NFR/AC propios un subconjunto de sus RF y RNF.
> Ante cualquier discrepancia, prevalece el maestro.

## Contexto y Problema

FEAT-001a deja la aplicación arrancando, con su esquema creado y sus cuentas sembradas, pero sin
ninguna forma de usarlas: no hay inicio de sesión ni pantalla privada. Al mismo tiempo, el
requerimiento más temprano del producto es RF-01 del maestro, que exige autenticación antes de
mostrar cualquier dato médico.

Este sub-ticket instala la puerta de entrada y la cierra: una pantalla de inicio de sesión, una
pantalla de cierre, una única página privada mínima como destino, y el bloqueo de toda ruta privada
para quien no tenga sesión válida. La página privada de destino no contiene datos médicos, porque el
producto todavía no tiene ninguno: es el lugar donde el listado de estudios se enchufará cuando esa
feature llegue.

La otra mitad del problema es el descubrimiento de cuentas: un mensaje que distinga "el usuario no
existe" de "la contraseña es incorrecta" convierte la pantalla de inicio de sesión en un enumerador
de cuentas del grupo familiar.

## Objetivos

- Permitir a cada integrante iniciar y cerrar sesión desde cualquier dispositivo.
- Poner en vigencia la privacidad por defecto: ninguna ruta privada responde sin sesión válida.
- Impedir que la pantalla de inicio de sesión revele qué nombres de usuario existen.
- Emitir la sesión en una cookie que el navegador no persista y que el sitio no exponga a scripts.
- Dejar un destino privado mínimo donde las features siguientes puedan enchufarse.

## Requerimientos Funcionales

- **FR-01**: El sistema debe exigir autenticación antes de responder cualquier página privada o recurso privado.
- **FR-02**: El sistema debe permitir iniciar sesión mediante el nombre de usuario y la contraseña de una cuenta existente.
- **FR-03**: El sistema debe permitir cerrar la sesión manualmente desde la interfaz e invalidar la sesión cerrada.
- **FR-04**: El sistema debe responder, tras un inicio de sesión exitoso, una página privada propia de la cuenta autenticada que no contiene datos médicos y que ofrece la acción de cerrar sesión.

## Requerimientos No Funcionales

- **NFR-01**: La cookie de autenticación debe declarar las 3 propiedades Secure, HttpOnly y SameSite=Strict.
- **NFR-02**: La cookie de autenticación debe ser de sesión del navegador: 0 atributos de persistencia declarados, sin `Expires` y sin `Max-Age`.
- **NFR-03**: El sistema debe devolver 1 único mensaje de rechazo tanto si el nombre de usuario no existe como si la contraseña es incorrecta, sin distinguir entre los 2 casos.
- **NFR-04**: El 100 % de las comunicaciones debe utilizar HTTPS con TLS 1.2 o superior, con redirección de HTTP a HTTPS y HSTS habilitado fuera del entorno de desarrollo.
- **NFR-05**: Los registros técnicos deben contener 0 contraseñas, 0 valores de cookie y 0 hashes de credenciales; el diagnóstico debe apoyarse únicamente en identificadores técnicos internos.
- **NFR-06**: La cobertura de pruebas automatizadas del código incorporado por este sub-ticket debe ser de al menos 80 %.
- **NFR-07**: La autorización debe resolverse mediante 1 único mecanismo central aplicado por defecto a todas las rutas, de modo que una ruta nueva quede protegida sin que su autor deba recordar protegerla.

## Criterios de Aceptación

- **AC-01 (FR-01, NFR-07)**: WHEN un usuario sin sesión válida solicita la página privada de la cuenta, THE sistema SHALL responder con una redirección al inicio de sesión y no incluir ningún dato de ninguna cuenta en la respuesta.
- **AC-02 (FR-01, NFR-07)**: IF un usuario sin sesión válida solicita cualquier ruta privada distinta del inicio de sesión, THEN THE sistema SHALL responder 401, 403 o una redirección al inicio de sesión, y no entregar el contenido solicitado.
- **AC-03 (FR-02, FR-04)**: WHEN un usuario presenta el nombre de usuario y la contraseña correctos de una cuenta existente, THE sistema SHALL establecer la sesión y responder la página privada de esa cuenta.
- **AC-04 (FR-02, NFR-03)**: IF el nombre de usuario no existe o la contraseña es incorrecta, THEN THE sistema SHALL rechazar el acceso con un mensaje idéntico en ambos casos y no establecer ninguna sesión.
- **AC-05 (FR-03)**: WHEN un usuario con sesión válida ejecuta la acción de cerrar sesión, THE sistema SHALL invalidar esa sesión, de modo que la siguiente solicitud a una ruta privada exija autenticarse nuevamente.
- **AC-06 (FR-04)**: WHEN un usuario autenticado solicita su página privada, THE sistema SHALL responder una página que identifica a la cuenta autenticada, ofrece la acción de cerrar sesión y no contiene datos médicos ni datos de ninguna otra cuenta.
- **AC-07 (NFR-01, NFR-02)**: WHEN se inspecciona la cookie de autenticación emitida en un inicio de sesión exitoso, THE sistema SHALL exhibir los atributos Secure, HttpOnly y SameSite=Strict, y ningún atributo `Expires` ni `Max-Age`.
- **AC-08 (NFR-04)**: WHEN la aplicación se ejecuta fuera del entorno de desarrollo y recibe una solicitud por HTTP, THE sistema SHALL redirigirla a HTTPS y emitir la cabecera HSTS.
- **AC-09 (NFR-05)**: IF un intento de inicio de sesión es rechazado, THEN THE sistema SHALL registrar el evento sin incluir la contraseña presentada, el valor de ninguna cookie ni ningún hash de credenciales.
- **AC-10 (NFR-06)**: WHEN se ejecuta la suite de pruebas de la solución, THE sistema SHALL alcanzar una cobertura de al menos 80 % sobre el código incorporado por este sub-ticket.

## Fuera de Alcance

- La expiración de sesión por inactividad y por duración absoluta, la sesión única por cuenta y el bloqueo por intentos fallidos, que son FEAT-001c. Este sub-ticket entrega una sesión que dura mientras el navegador siga abierto.
- El alta de cuentas y la política de contraseñas, entregadas por FEAT-001a.
- La entidad Estudio y el listado real de estudios: la página privada de FR-04 es un destino mínimo, no el listado.
- Todo lo relativo a archivos médicos, cifrado, búsqueda y PWA, según el índice del PRD padre.
- El cambio y la recuperación de contraseña desde la interfaz, excluidos del MVP por RNF-54 del maestro.

## Riesgos y Mitigaciones

- **Una ruta nueva nace desprotegida.** Proteger ruta por ruta funciona hasta que alguien agrega una y se olvida. Mitigación: NFR-07 exige autorización por defecto sobre todas las rutas, con excepción explícita únicamente para el inicio de sesión y los recursos estáticos públicos.
- **La indistinguibilidad se rompe por un camino lateral.** Aunque el mensaje sea idéntico, un redirect distinto, un código de estado distinto o un campo resaltado delatan si la cuenta existe. Mitigación: AC-04 exige que el rechazo sea idéntico, y la prueba compara la respuesta completa, no solo el texto.
- **La demora del rechazo delata la existencia de la cuenta.** Verificar el hash solo cuando la cuenta existe produce una diferencia medible. Riesgo conocido y acotado: la indistinguibilidad temporal se especifica y se prueba en FEAT-001c (NFR-04 de ese sub-ticket); acá se evita introducir atajos que la hagan imposible, ejecutando el mismo trabajo en ambas ramas.
- **El atributo Secure rompe el desarrollo local.** Una cookie Secure no viaja por HTTP, y forzarla mal lleva a desactivarla "temporalmente". Mitigación: el entorno de desarrollo usa HTTPS local, y NFR-01 no admite excepciones por entorno.

## Dependencias

- FEAT-001a: cuentas creadas y contraseñas hasheadas. Sin ellas no hay credenciales válidas contra las cuales autenticar.
- ASP.NET Core Identity para la verificación de credenciales de FR-02.
- La autenticación por cookie de ASP.NET Core para NFR-01 y NFR-02, y su política de autorización por defecto para NFR-07.
- Un certificado TLS válido en el entorno de despliegue, requerido por NFR-04 y por el atributo Secure de NFR-01.
- Proyecto de pruebas de integración ejecutado con `dotnet test`, requerido por NFR-06.
