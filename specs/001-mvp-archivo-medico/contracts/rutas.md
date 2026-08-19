# Fase 1 — Contrato de Rutas HTTP

**Feature**: MVP Mi Archivo Médico · **Fecha**: 2026-08-19 · **Plan**: [../plan.md](../plan.md)

La interfaz que este proyecto expone es un conjunto de rutas HTTP que devuelven HTML renderizado en el
servidor, más una que devuelve el contenido de un archivo. No hay API para terceros ni formato de
intercambio: el contrato es este.

## Reglas que valen para todas las rutas

1. **Autenticación por omisión.** Una política global exige sesión en todo el sitio. Aparecer en esta
   tabla sin la marca *anónima* significa que exige sesión. Toda ruta anónima está listada
   explícitamente y es una excepción justificada. (RNF-01, AC-01)
2. **404 uniforme.** Un recurso de otra cuenta, uno inexistente y un identificador mal formado
   devuelven **404** con la misma respuesta, sin cuerpo que los distinga. (RNF-53, AC-47, AC-48)
3. **Nada médico en la dirección.** Ninguna ruta acepta términos de búsqueda, filtros ni metadatos como
   parte de la dirección. Todo eso viaja en el cuerpo. (RNF-63, AC-96)
4. **Antifalsificación.** Toda ruta que mute datos o que reciba el criterio de búsqueda exige el token
   antifalsificación del formulario.
5. **Cookie de sesión** con `Secure`, `HttpOnly` y `SameSite=Strict`; expiración deslizante de 30
   minutos y tope absoluto de 24 horas. (RNF-11, RNF-04, RNF-05, AC-58)
6. **Sin datos médicos en la respuesta de error.** Ningún mensaje de error revela títulos,
   instituciones, profesionales ni la existencia de recursos ajenos. (RNF-09, AC-64)

## Rutas anónimas

Son las únicas tres. Cualquier agregado a esta lista es una decisión de seguridad, no una de
comodidad.

| Método | Ruta | Qué devuelve | Requisitos |
|---|---|---|---|
| GET | `/Cuenta/InicioDeSesion` | Formulario de ingreso. | RF-02 |
| POST | `/Cuenta/InicioDeSesion` | Redirección al listado si las credenciales son válidas; el mismo formulario con un error idéntico para usuario inexistente y contraseña incorrecta, con demora comparable, si no. | RF-02, RNF-13, RNF-60, RNF-65, AC-03, AC-04, AC-69, AC-86, AC-87, AC-98 |
| GET | `/sin-conexion` | Pantalla sin conexión, sin layout y sin ningún dato médico. Es la única página de la aplicación que el service worker guarda. | RF-26, RNF-51, AC-40, AC-41 |

Los archivos estáticos (`/css/…`, `/js/…`, `/iconos/…`, `/manifest.webmanifest`, `/sw.js`) también se
sirven sin sesión, y un test comprueba que cada entrada de la lista del service worker responde sin
autenticar.

## Sesión

| Método | Ruta | Qué hace | Requisitos |
|---|---|---|---|
| POST | `/Cuenta/CerrarSesion` | Invalida la sesión en curso y redirige al ingreso. | RF-03, RNF-12, AC-05 |

## Estudios

| Método | Ruta | Cuerpo | Qué devuelve | Requisitos |
|---|---|---|---|---|
| GET | `/Estudios` | — | Listado del propietario, del más reciente al más antiguo, con el criterio de búsqueda vigente en la sesión aplicado, hasta 25 por página, con contador de resultados y controles de avance y retroceso. Sin ningún estudio propio muestra el estado de bienvenida; sin resultados de búsqueda, el estado correspondiente con la acción de limpiar filtros. | RF-15, RF-23, RF-39, RF-40, RNF-27, AC-28, AC-37, AC-49, AC-54, AC-93, AC-94, AC-101 |
| POST | `/Estudios/Buscar` | Término, rango de fechas, institución elegida de la lista | Guarda el criterio en la sesión y devuelve el listado en la página 1. | RF-16, RF-17, RF-20, RF-21, RF-38, RNF-55, RNF-63, AC-29 a AC-31, AC-34, AC-35, AC-45, AC-46, AC-71 a AC-73, AC-92, AC-96 |
| POST | `/Estudios/Pagina` | Número de página | Devuelve el listado en esa página, con el criterio de la sesión intacto. | RF-40, RNF-27, AC-95, AC-101 |
| POST | `/Estudios/LimpiarFiltros` | — | Borra el criterio de la sesión y devuelve el listado completo. | RF-22, AC-36 |
| GET | `/Estudios/Detalle/{id}` | — | Detalle del estudio: metadatos, etiquetas y la lista de archivos **sin transferir su contenido**. Ajeno o inexistente: 404. | RF-11, RNF-28, RNF-53, AC-47, AC-78 |
| GET | `/Estudios/Crear` | — | Formulario de alta. | RF-33, RNF-31 |
| POST | `/Estudios/Crear` | Metadatos + archivos | Crea el estudio y adjunta los archivos válidos; informa cada archivo rechazado junto a ese archivo. Con datos inválidos devuelve el formulario con los metadatos intactos y el aviso de readjuntar. | RF-33 a RF-37, RNF-32, RNF-66, RNF-67, AC-09 a AC-12, AC-79, AC-80, AC-90, AC-91, AC-99, AC-100 |
| GET | `/Estudios/Editar/{id}` | — | Formulario de edición. Ajeno: 404. | RF-10, RNF-53 |
| POST | `/Estudios/Editar/{id}` | Metadatos | Actualiza los metadatos sin tocar los archivos ni su huella. Ajeno: 404. | RF-10, RNF-18, AC-14 |
| POST | `/Estudios/AgregarArchivos/{id}` | Archivos | Adjunta archivos a un estudio existente, con las mismas validaciones y límites. | RF-07, RNF-61, AC-12, AC-70 |
| GET | `/Estudios/Eliminar/{id}` | — | Confirmación explícita, que declara que la operación es irreversible. Ajeno: 404. | RF-13, RNF-33, AC-17 |
| POST | `/Estudios/Eliminar/{id}` | — | Borra el estudio, sus etiquetas, sus filas de archivo y el contenido físico; libera el cupo. Cancelar no elimina nada. | RF-14, AC-18, AC-19, AC-102 |

## Archivos

| Método | Ruta | Qué devuelve | Requisitos |
|---|---|---|---|
| GET | `/Archivos/Ver/{id}` | Vista que incrusta el archivo en un marco restringido, con un token de acceso recién emitido. Ajeno: 404. | RF-11, RNF-20, AC-15, AC-26 |
| GET | `/Archivos/Contenido/{id}?t={token}` | El contenido descifrado, para mostrar en línea, con las cabeceras que impiden interpretar el tipo y ejecutar contenido activo. | RNF-06, RNF-07, RNF-08, RNF-20, AC-02, AC-08, AC-84 |
| GET | `/Archivos/Descargar/{id}?t={token}` | El contenido descifrado como descarga, con el nombre original sanitizado y una huella idéntica a la registrada. | RF-12, RNF-18, AC-16, AC-77 |

El parámetro `t` es un token firmado con vencimiento de 5 minutos. **No es una credencial**: las dos
rutas de contenido exigen además sesión válida del propietario. Un token vigente sin sesión no entrega
nada, y una sesión válida con token vencido tampoco. No es un dato médico, así que puede viajar en la
dirección sin violar la regla 3.

## Respuestas de error

| Situación | Código | Cuerpo |
|---|---|---|
| Sin sesión en una ruta protegida | 302 al ingreso (navegación) o 401 (solicitud de contenido) | Sin datos médicos. AC-01, AC-02 |
| Recurso ajeno, inexistente o identificador mal formado | 404 | Idéntico en los tres casos. AC-47, AC-48 |
| Intento de cambiar el propietario de un estudio | 404 si la ruta no existe, 405 si existe y no admite el método | AC-63 |
| Ruta de registro de cuentas | 404 | No existe ninguna. AC-50 |
| Token de archivo vencido | 404 | Indistinguible de un archivo inexistente. AC-08 |
| Archivo que supera 50 MB, vacío, corrupto o con firma incoherente | El formulario, con el motivo junto a ese archivo | Nada llega al almacenamiento definitivo. AC-21 a AC-24, AC-27, AC-44 |
| Cupo de 20 GB insuficiente para la carga | El formulario, con el aviso del límite | Sin revelar qué cuenta consumió el espacio. AC-55, AC-64, AC-97 |
| Más de 20 archivos en un estudio | El formulario, con el aviso del límite | AC-70 |

## Lo que este contrato deliberadamente no incluye

No hay rutas de registro, de cambio ni de recuperación de contraseña; ninguna que asigne, copie o
autorice un estudio a otro propietario; ninguna de exportación, importación o restauración; y ninguna
que entregue un archivo sin autenticación. Su ausencia es verificable enumerando las rutas registradas
por la aplicación. (RNF-39, RNF-54, RNF-57, AC-50, AC-63)
