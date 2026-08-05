# PRD-001: Mi Archivo Médico — Aplicación web progresiva de uso familiar para almacenar, organizar, consultar y encontrar estudios médicos de forma segura

> **Estado**: vigente · **Alcance**: MVP · **Última actualización**: 2026-08-05
>
> Este documento es la fuente única de verdad del alcance. Convenciones de lectura:
> **RF-nn** requerimiento funcional, **RNF-nn** requerimiento no funcional, **AC-nn** criterio de aceptación
> (cada AC declara entre paréntesis los requerimientos que verifica). Los identificadores son permanentes:
> un identificador retirado se marca como tal y **nunca se reutiliza**. La selección tecnológica concreta se
> documenta en `AGENTS.md`; este PRD define el qué, no el cómo.

## Contexto y Problema

Los estudios médicos personales suelen quedar distribuidos entre correos electrónicos, aplicaciones de mensajería, carpetas locales, portales de clínicas y documentos impresos. Esta dispersión genera varios problemas:

- Resulta difícil encontrar rápidamente un estudio anterior.
- Se pierde el contexto histórico de análisis, informes y consultas.
- Los archivos dependen de plataformas externas que pueden dejar de estar disponibles.
- La información médica puede quedar almacenada sin controles adecuados de seguridad.
- Acceder a un documento durante una consulta médica puede requerir buscarlo manualmente entre diferentes fuentes.

La aplicación estará destinada a un grupo familiar reducido, de hasta 5 usuarios, y proporcionará a cada uno un repositorio centralizado, privado y accesible desde computadoras, tabletas y teléfonos. Cada usuario accede exclusivamente a sus propios estudios: las cuentas comparten la instalación, no los datos.

La información utilizada para buscar los estudios será ingresada manualmente mediante metadatos. El sistema no analizará ni extraerá automáticamente el contenido interno de los documentos.

## Objetivos

- Centralizar los estudios médicos de hasta 5 integrantes de una familia en una única aplicación, manteniendo los datos de cada uno aislados de los demás.
- Permitir encontrar un estudio conocido en menos de 10 segundos.
- Proteger los archivos y sus metadatos frente a accesos no autorizados.
- Permitir el acceso desde computadoras, tabletas y teléfonos.
- Evitar la necesidad de desarrollar aplicaciones móviles nativas.
- Mantener el MVP pequeño, construible y con bajos costos operativos.
- Evitar dependencias innecesarias de servicios externos pagos.
- Conservar los archivos originales sin alteraciones.
- Reducir el riesgo de pérdida de información mediante respaldos automáticos de infraestructura.

## Requerimientos Funcionales

### Autenticación

- **RF-01**: El sistema debe requerir autenticación antes de mostrar estudios, archivos o metadatos.
- **RF-02**: El sistema debe permitir iniciar sesión utilizando credenciales válidas.
- **RF-03**: El sistema debe permitir cerrar la sesión manualmente.
- **RF-04**: El sistema debe bloquear la sesión después de un período de inactividad.
- **RF-05**: El sistema debe requerir una nueva autenticación cuando la sesión alcance su duración máxima.

### Gestión de estudios

- **RF-06**: El sistema debe permitir crear un estudio médico indicando como mínimo un título y una fecha.
- **RF-07**: El sistema debe permitir asociar uno o más archivos a un mismo estudio.
- **RF-08**: El sistema debe aceptar archivos en formato PDF, JPG, JPEG y PNG.
- **RF-09**: El sistema debe permitir registrar para cada estudio el profesional, la institución, la descripción y las etiquetas, todos como texto libre.
- **RF-10**: El sistema debe permitir editar los metadatos de un estudio.
- **RF-11**: El sistema debe permitir visualizar individualmente cada archivo asociado a un estudio.
- **RF-12**: El sistema debe permitir descargar individualmente cada archivo asociado a un estudio.
- **RF-13**: El sistema debe solicitar una confirmación explícita antes de eliminar un estudio.
- **RF-14**: El sistema debe eliminar el estudio y sus archivos asociados cuando la eliminación es confirmada.

### Organización y búsqueda

- **RF-15**: El sistema debe listar los estudios desde el más reciente hasta el más antiguo.
- **RF-16**: El sistema debe permitir buscar estudios por título, descripción, profesional, institución y etiquetas.
- **RF-17**: El sistema debe permitir filtrar estudios por rango de fechas.
- **RF-18**: *(retirado)* El filtro por tipo se eliminó del alcance junto con el metadato "tipo". Identificador retirado; no reutilizar.
- **RF-19**: *(retirado)* El filtro por especialidad se eliminó del alcance junto con el metadato "especialidad". Identificador retirado; no reutilizar.
- **RF-20**: El sistema debe permitir filtrar estudios por institución.
- **RF-21**: El sistema debe permitir combinar la búsqueda textual con uno o más filtros.
- **RF-22**: El sistema debe permitir limpiar todos los filtros mediante una única acción.
- **RF-23**: El sistema debe mostrar la cantidad de estudios encontrados después de aplicar una búsqueda o filtro.

### PWA

- **RF-24**: El sistema debe permitir instalar la aplicación como PWA en navegadores compatibles.
- **RF-25**: *(reasignado)* La adaptabilidad de la interfaz a computadoras, tabletas y teléfonos se especifica en RNF-29 y RNF-30. Identificador retirado; no reutilizar.
- **RF-26**: El sistema debe mostrar una pantalla controlada cuando no exista conexión.
- **RF-27**: *(reasignado)* La prohibición de mostrar información médica sin autenticación se especifica en RNF-51. Identificador retirado; no reutilizar.
- **RF-28**: El sistema debe informar al usuario cuando una carga no pueda completarse por pérdida de conexión.

## Requerimientos No Funcionales

### Seguridad

- **RNF-01**: El 100 % de las comunicaciones debe utilizar HTTPS con TLS 1.2 o superior.
- **RNF-02**: El 100 % de los archivos debe almacenarse cifrado en reposo mediante AES-256. La clave de cifrado no debe residir en el código fuente ni en archivos de configuración versionados.
- **RNF-58**: La clave de cifrado de archivos debe custodiarse fuera del entorno principal y fuera de los respaldos, de modo que su pérdida no sea posible sin perder simultáneamente el entorno y la custodia, y que el acceso a un respaldo por sí solo no permita descifrar los archivos.
- **RNF-03**: Las contraseñas deben almacenarse mediante Argon2id (memoria mínima 19 MiB, 2 iteraciones, paralelismo 1), bcrypt (factor de costo mínimo 12) o PBKDF2-HMAC-SHA256 con un mínimo de 100.000 iteraciones.
- **RNF-04**: La sesión debe bloquearse después de 30 minutos de inactividad.
- **RNF-05**: La sesión debe tener una duración absoluta máxima de 24 horas.
- **RNF-06**: Los archivos privados no deben estar disponibles mediante URLs públicas permanentes.
- **RNF-07**: Las URLs temporales utilizadas para acceder a archivos deben expirar en un máximo de 5 minutos.
- **RNF-08**: El sistema debe validar la autenticación y autorización antes de entregar cualquier archivo.
- **RNF-09**: Los nombres de estudios, profesionales, instituciones, descripciones y resultados médicos no deben registrarse en logs técnicos.
- **RNF-10**: Los entornos de desarrollo y prueba deben utilizar documentos ficticios o anonimizados.
- **RNF-11**: Las cookies de autenticación deben utilizar las propiedades Secure, HttpOnly y SameSite=Strict.
- **RNF-12**: El sistema debe invalidar la sesión al cerrar sesión manualmente.
- **RNF-13**: Los mensajes de error de autenticación no deben indicar si el usuario o la contraseña fueron incorrectos por separado.
- **RNF-53**: Cada estudio y cada archivo deben estar asociados a un propietario. El sistema debe entregar exclusivamente los estudios, archivos y metadatos cuyo propietario sea el usuario autenticado, y responder HTTP 403 o HTTP 404 ante cualquier solicitud sobre un recurso de otro propietario, incluso si el usuario está autenticado y conoce el identificador del recurso.
- **RNF-54**: La interfaz no debe ofrecer registro abierto de cuentas. El alta de usuarios debe realizarse fuera de la aplicación, mediante un procedimiento administrativo.
- **RNF-56**: El sistema debe admitir un máximo de 5 cuentas activas y rechazar el alta de una cuenta adicional cuando ese límite esté alcanzado.
- **RNF-57**: El sistema no debe permitir que un usuario comparta, delegue o transfiera el acceso a sus estudios a otra cuenta. No existen roles, ni cuentas con visibilidad sobre los datos de terceros.

### Validación de archivos

- **RNF-14**: El sistema debe aceptar archivos de hasta 50 MB.
- **RNF-15**: La extensión declarada, el tipo MIME y la firma binaria del archivo deben ser compatibles.
- **RNF-16**: El sistema debe rechazar archivos ejecutables, scripts o formatos no permitidos aunque su extensión haya sido modificada.
- **RNF-17**: El sistema debe rechazar archivos de 0 bytes y archivos corruptos. Se considera corrupto todo archivo que no supere la validación estructural de su formato declarado: un PDF sin encabezado `%PDF-` o sin marca de fin `%%EOF`, y una imagen JPG o PNG que no pueda decodificarse por completo.
- **RNF-18**: Los archivos originales no deben modificarse durante la carga, visualización o descarga.
- **RNF-19**: El sistema debe calcular y almacenar un hash SHA-256 para cada archivo cargado.
- **RNF-20**: La visualización de archivos no debe ejecutar macros, scripts, JavaScript embebido ni contenido activo.
- **RNF-21**: Los archivos rechazados no deben permanecer en el almacenamiento definitivo.
- **RNF-22**: El nombre físico utilizado en el almacenamiento debe ser un identificador GUID generado por el sistema, sin ninguna porción derivada del nombre original proporcionado por el usuario.
- **RNF-23**: El sistema debe sanitizar el nombre original antes de mostrarlo o almacenarlo como metadato, aplicando estas reglas: eliminar separadores de ruta (`/`, `\`), caracteres de control y secuencias `..`; truncar a un máximo de 255 caracteres; conservar la extensión original; y escapar el resultado al mostrarlo en la interfaz.

### Rendimiento

- **RNF-24**: Una búsqueda sobre una colección de hasta 2.000 estudios debe responder en menos de 1 segundo, p95. Ese volumen es el techo de diseño del MVP: corresponde a las hasta 5 cuentas de un grupo familiar acumulando alrededor de 40 estudios por año durante 10 años, con margen. No se optimiza para volúmenes mayores.
- **RNF-25**: El listado inicial debe responder en menos de 2 segundos, p95, sin incluir la descarga de archivos, sobre el volumen definido en RNF-24.
- **RNF-26**: La carga de un estudio con archivos que sumen hasta 10 MB debe completarse en menos de 15 segundos con una conexión estable de 10 Mbps.
- **RNF-27**: La aplicación debe paginar el listado cuando existan más de 25 estudios.
- **RNF-28**: La aplicación no debe descargar archivos médicos hasta que el usuario solicite visualizarlos o descargarlos.

### Usabilidad

- **RNF-29**: La interfaz debe ser utilizable desde dispositivos con un ancho mínimo de 360 píxeles.
- **RNF-30**: Las acciones principales no deben requerir desplazamiento horizontal. Se consideran acciones principales: iniciar sesión, crear un estudio, cargar archivos, buscar, aplicar filtros, limpiar filtros, abrir el detalle de un estudio, visualizar un archivo, descargar un archivo y eliminar un estudio.
- **RNF-31**: La creación de un estudio debe completarse en un máximo de tres pantallas o pasos.
- **RNF-32**: Los errores de validación deben mostrarse junto al campo o archivo que los produjo.
- **RNF-33**: La aplicación debe solicitar confirmación antes de realizar una eliminación irreversible.
- **RNF-55**: La búsqueda y los filtros sobre metadatos de texto libre deben ser insensibles a mayúsculas, minúsculas y acentos, y deben ignorar los espacios al inicio y al final del término ingresado. Como SQLite no ofrece una intercalación insensible a acentos, la normalización (minúsculas, sin acentos, sin espacios sobrantes) debe resolverse en la aplicación: cada campo de texto buscable se persiste además en una columna normalizada e indexada, y el término ingresado se normaliza con la misma función antes de consultar.

### Respaldo y recuperación de infraestructura

- **RNF-34**: La infraestructura debe generar al menos un respaldo automático diario de la base de datos y conservar los respaldos durante un mínimo de 30 días. Al tratarse de una base SQLite (un único archivo en el servidor), el respaldo debe tomarse con un mecanismo consistente en caliente (`VACUUM INTO` o la API de backup en línea), nunca copiando el archivo mientras hay escrituras en curso.
- **RNF-35**: Los respaldos deben almacenarse en una cuenta o suscripción distinta de la del entorno principal, de modo que una credencial comprometida del entorno principal no permita alterarlos ni eliminarlos. En particular, no deben quedar en el mismo disco ni en la misma carpeta que el archivo de base de datos ni que el almacenamiento de archivos médicos.
- **RNF-36**: El proceso de respaldo debe incluir los metadatos necesarios para reconstruir la relación entre estudios y archivos.
- **RNF-59**: El respaldo diario debe abarcar tanto la base de datos como el almacenamiento de archivos médicos, y ambos deben corresponder a un mismo punto en el tiempo, de modo que una restauración no deje estudios con archivos faltantes ni archivos huérfanos.
- **RNF-37**: El responsable técnico debe realizar una prueba de recuperación al menos una vez cada tres meses.
- **RNF-38**: Los respaldos y su restauración deben ser administrados exclusivamente a nivel de infraestructura.
- **RNF-39**: La interfaz del usuario no debe ofrecer funciones de exportación, importación o restauración.

### Privacidad

- **RNF-40**: La aplicación no debe incluir publicidad.
- **RNF-41**: La aplicación no debe incluir herramientas de seguimiento de comportamiento.
- **RNF-42**: La aplicación no debe compartir documentos o metadatos médicos con terceros.
- **RNF-43**: Las métricas técnicas no deben incluir títulos, descripciones, nombres de archivos ni datos médicos.
- **RNF-44**: El contenido médico no debe utilizarse para entrenar modelos de inteligencia artificial.
- **RNF-51**: La aplicación no debe mostrar información médica almacenada previamente cuando el usuario no esté autenticado. *(proviene de RF-27)*

### Costos y dependencias externas

- **RNF-45**: El MVP debe poder operar con un costo mensual de infraestructura no superior a USD 15, sin considerar el dominio, dentro del límite de almacenamiento definido en RNF-52.
- **RNF-46**: El funcionamiento principal no debe depender de APIs de inteligencia artificial.
- **RNF-47**: El funcionamiento principal no debe depender de servicios de OCR.
- **RNF-48**: La búsqueda debe implementarse utilizando las capacidades de la base de datos.
- **RNF-49**: El funcionamiento principal no debe depender de un motor de búsqueda administrado.
- **RNF-50**: Cualquier servicio externo pago debe poder deshabilitarse sin impedir la carga, organización, visualización y búsqueda por metadatos.
- **RNF-52**: El almacenamiento total de archivos del MVP no debe superar los 20 GB, compartidos entre las hasta 5 cuentas y sin cuota individual. El sistema debe rechazar nuevas cargas de cualquier cuenta cuando se alcance ese límite e informarlo al usuario.

## Criterios de Aceptación

### Autenticación y autorización

- **AC-01 (RF-01)**: Dado un usuario no autenticado, cuando intenta abrir la página de estudios, entonces el sistema lo redirige al inicio de sesión y no muestra información médica.
- **AC-02 (RF-01, RNF-08)**: Dada la URL interna de un archivo, cuando una persona no autenticada intenta acceder directamente, entonces el sistema responde HTTP 401 o HTTP 403 y no entrega el archivo.
- **AC-03 (RF-02)**: Dadas credenciales válidas, cuando el usuario inicia sesión, entonces accede al listado de estudios.
- **AC-04 (RF-02)**: Dadas credenciales inválidas, cuando el usuario intenta iniciar sesión, entonces el sistema rechaza el acceso sin indicar cuál credencial fue incorrecta.
- **AC-05 (RF-03, RNF-12)**: Dada una sesión autenticada, cuando el usuario selecciona "Cerrar sesión", entonces la sesión deja de ser válida.
- **AC-06 (RF-04, RNF-04)**: Dada una sesión sin actividad durante 30 minutos, cuando el usuario vuelve a interactuar, entonces debe autenticarse nuevamente.
- **AC-07 (RF-05, RNF-05)**: Dada una sesión iniciada hace más de 24 horas, cuando el usuario realiza una nueva solicitud, entonces el sistema exige una nueva autenticación.
- **AC-08 (RNF-07)**: Dada una URL temporal generada hace más de 5 minutos, cuando se intenta utilizar, entonces el sistema rechaza el acceso al archivo.

### Creación y edición de estudios

- **AC-09 (RF-06)**: Dado un título y una fecha válidos, cuando el usuario crea un estudio, entonces el estudio queda almacenado y aparece en el listado.
- **AC-10 (RF-06)**: Dado un título vacío, cuando el usuario intenta crear un estudio, entonces el sistema no lo crea y muestra un error.
- **AC-11 (RF-06)**: Dada una fecha inválida, cuando el usuario intenta crear un estudio, entonces el sistema no lo crea y muestra un error.
- **AC-12 (RF-07)**: Dado un estudio con un informe PDF y dos imágenes, cuando se completa la carga, entonces los tres archivos aparecen agrupados dentro del mismo estudio.
- **AC-13 (RF-09)**: Dado un estudio existente, cuando el usuario agrega un profesional, una institución y una etiqueta, entonces esos datos quedan almacenados.
- **AC-14 (RF-10, RNF-18)**: Dado un estudio existente, cuando se modifica su institución, entonces el metadato se actualiza y el hash del archivo original no cambia.

### Visualización, descarga y eliminación

- **AC-15 (RF-11)**: Dado un archivo válido asociado a un estudio, cuando el usuario selecciona "Visualizar", entonces el sistema lo muestra en pantalla sin ofrecer su descarga como único medio de acceso. *(La ausencia de contenido activo se verifica en AC-26.)*
- **AC-16 (RF-12)**: Dado un archivo almacenado, cuando el usuario selecciona "Descargar", entonces recibe un archivo cuyo hash SHA-256 coincide con el registrado.
- **AC-17 (RF-13)**: Dado un estudio existente, cuando el usuario selecciona "Eliminar", entonces el sistema solicita confirmación.
- **AC-18 (RF-13)**: Dada una solicitud de eliminación, cuando el usuario cancela la confirmación, entonces el estudio y sus archivos permanecen disponibles.
- **AC-19 (RF-14)**: Dada una eliminación confirmada, cuando finaliza la operación, entonces el estudio y sus archivos dejan de estar disponibles.

### Validación de archivos

- **AC-20 (RF-08)**: Dado un PDF válido de menos de 50 MB, cuando se carga, entonces el sistema lo acepta.
- **AC-21 (RNF-14)**: Dado un archivo de más de 50 MB, cuando se intenta cargar, entonces el sistema lo rechaza antes de almacenarlo definitivamente.
- **AC-22 (RNF-15, RNF-16)**: Dado un archivo ejecutable renombrado con extensión .pdf, cuando se intenta cargar, entonces el sistema detecta la incompatibilidad y lo rechaza.
- **AC-23 (RNF-15)**: Dado un archivo con extensión .jpg cuya firma corresponde a otro formato, cuando se intenta cargar, entonces el sistema lo rechaza.
- **AC-24 (RNF-17)**: Dado un archivo de 0 bytes, cuando se intenta cargar, entonces el sistema lo rechaza.
- **AC-44 (RNF-17)**: Dado un PDF truncado que carece de la marca `%%EOF`, cuando se intenta cargar, entonces el sistema lo rechaza por no superar la validación estructural de su formato.
- **AC-25 (RNF-19)**: Dado un archivo cargado correctamente, cuando finaliza la operación, entonces el sistema registra su hash SHA-256.
- **AC-26 (RNF-20)**: Dado un PDF con JavaScript embebido, cuando se visualiza, entonces el script no se ejecuta.
- **AC-27 (RNF-21)**: Dado un archivo rechazado, cuando finaliza la operación, entonces el archivo no existe en el almacenamiento definitivo.
- **AC-65 (RNF-22)**: Dado un archivo cargado con el nombre `informe.pdf`, cuando se inspecciona el almacenamiento, entonces el nombre físico es un GUID generado por el sistema y no contiene ninguna porción del nombre original.
- **AC-66 (RNF-23)**: Dado un archivo cuyo nombre original es `../../etc/passwd.pdf`, cuando se carga, entonces el metadato almacenado no contiene separadores de ruta ni secuencias `..`, conserva la extensión `.pdf` y se muestra escapado en la interfaz.

### Búsqueda y filtros

- **AC-28 (RF-15)**: Dados estudios con fechas diferentes, cuando se abre el listado, entonces aparecen ordenados desde el más reciente hasta el más antiguo.
- **AC-29 (RF-16)**: Dado un estudio cuya institución es "Hospital Central", cuando el usuario busca "Central", entonces el estudio aparece entre los resultados.
- **AC-30 (RF-16)**: Dado un estudio etiquetado como "cardiología", cuando el usuario busca "cardiología", entonces el estudio aparece entre los resultados.
- **AC-31 (RF-17)**: Dados estudios de distintos años, cuando se aplica un rango de fechas, entonces solo aparecen los incluidos dentro del rango.
- **AC-32**: *(retirado)* Verificaba el filtro por tipo, eliminado del alcance. Identificador retirado; no reutilizar.
- **AC-33**: *(retirado)* Verificaba el filtro por especialidad, eliminado del alcance. Identificador retirado; no reutilizar.
- **AC-45 (RNF-55)**: Dado un estudio cuya institución es "Hospital Central", cuando el usuario busca " hospital central " en minúsculas y con espacios sobrantes, entonces el estudio aparece entre los resultados.
- **AC-46 (RNF-55)**: Dado un estudio etiquetado como "cardiología", cuando el usuario busca "cardiologia" sin acento, entonces el estudio aparece entre los resultados.
- **AC-34 (RF-20)**: Dados estudios de diferentes instituciones, cuando se filtra por una institución, entonces solo aparecen los estudios asociados.
- **AC-35 (RF-21)**: Dados estudios de distintas instituciones, cuando el usuario busca "ecografía" y filtra por la institución "Hospital Central", entonces solo aparecen los estudios que cumplen ambas condiciones.
- **AC-36 (RF-22)**: Dados varios filtros activos, cuando el usuario selecciona "Limpiar filtros", entonces se restablece el listado completo.
- **AC-37 (RF-23)**: Dada una búsqueda con cinco resultados, cuando se muestra el listado, entonces la interfaz informa que se encontraron cinco estudios.

### PWA y privacidad local

- **AC-38 (RF-24)**: Dado un navegador compatible, cuando el usuario instala la aplicación, entonces puede abrirla desde un acceso independiente.
- **AC-39 (RNF-29, RNF-30)**: Dada una pantalla de 360 píxeles de ancho, cuando se ejecuta cada una de las diez acciones principales enumeradas en RNF-30, entonces todas se completan sin desplazamiento horizontal.
- **AC-40 (RF-26)**: Dada una pérdida de conexión, cuando el usuario navega en la aplicación, entonces se muestra una pantalla que indica explícitamente la falta de conexión y que no contiene estudios, metadatos ni archivos.
- **AC-41 (RNF-51)**: Dado un usuario no autenticado, cuando abre la PWA sin conexión, entonces no se muestran estudios ni metadatos médicos previamente visualizados.
- **AC-42 (RF-28)**: Dada una carga interrumpida por pérdida de conexión, cuando la operación falla, entonces el sistema informa que el archivo no fue cargado.
- **AC-43 (RNF-09)**: Dada la creación, visualización o eliminación de un estudio, cuando se revisan los logs técnicos, entonces no aparecen títulos, profesionales, instituciones, descripciones ni nombres originales de archivos.

### Propiedad de los datos y control de acceso

- **AC-47 (RNF-53, RNF-08)**: Dado un estudio perteneciente a otro propietario, cuando un usuario autenticado solicita su detalle sustituyendo el identificador en la URL, entonces el sistema responde HTTP 403 o HTTP 404 y no entrega metadatos.
- **AC-48 (RNF-53, RNF-08)**: Dado un archivo perteneciente a otro propietario, cuando un usuario autenticado solicita su descarga sustituyendo el identificador en la URL, entonces el sistema responde HTTP 403 o HTTP 404 y no entrega el archivo.
- **AC-49 (RNF-53)**: Dados estudios de dos propietarios distintos, cuando un usuario autenticado abre el listado y realiza una búsqueda sin filtros, entonces solo aparecen sus propios estudios y el contador de resultados no incluye los del otro propietario.
- **AC-50 (RNF-54)**: Dada la aplicación desplegada, cuando se recorren las pantallas públicas y autenticadas, entonces no existe ninguna opción ni ruta accesible para registrar una cuenta nueva.
- **AC-62 (RNF-56)**: Dadas 5 cuentas activas, cuando se intenta dar de alta una sexta, entonces el sistema rechaza el alta e informa que se alcanzó el límite de cuentas.
- **AC-63 (RNF-57)**: Dada una sesión autenticada, cuando el usuario recorre el detalle de un estudio y sus opciones, entonces no existe ninguna acción para compartir, delegar ni transferir el estudio a otra cuenta.

### Rendimiento

- **AC-51 (RNF-24)**: Dada una colección de 2.000 estudios, cuando se ejecutan búsquedas por texto sobre metadatos, entonces el percentil 95 del tiempo de respuesta es inferior a 1 segundo.
- **AC-52 (RNF-25)**: Dada una colección de 2.000 estudios, cuando se abre el listado inicial, entonces el percentil 95 del tiempo de respuesta es inferior a 2 segundos, sin contar la descarga de archivos.
- **AC-53 (RNF-26)**: Dado un estudio con archivos que suman 10 MB y una conexión estable de 10 Mbps, cuando el usuario confirma la carga, entonces la operación finaliza en menos de 15 segundos.
- **AC-54 (RNF-27)**: Dada una colección de 26 estudios, cuando se abre el listado, entonces se muestran como máximo 25 estudios y existe un control para avanzar a la página siguiente.
- **AC-55 (RNF-52)**: Dado un almacenamiento compartido que alcanzó los 20 GB, cuando cualquiera de las cuentas intenta cargar un archivo adicional, entonces el sistema rechaza la carga e informa que se alcanzó el límite de almacenamiento.
- **AC-64 (RNF-52, RNF-53)**: Dado el aviso de límite de almacenamiento alcanzado, cuando se muestra al usuario, entonces no revela qué cuenta consumió el espacio ni ningún metadato de estudios ajenos.

### Transporte y almacenamiento seguro

- **AC-56 (RNF-01)**: Dada una solicitud HTTP sin cifrar hacia cualquier ruta de la aplicación, cuando el servidor responde, entonces redirige a HTTPS y la conexión negociada utiliza TLS 1.2 o superior.
- **AC-57 (RNF-02)**: Dado un archivo cargado correctamente, cuando se inspecciona su contenido directamente en el almacenamiento sin pasar por la aplicación, entonces los bytes no corresponden al archivo original en claro.
- **AC-58 (RNF-11)**: Dada una sesión iniciada, cuando se inspecciona la cookie de autenticación, entonces presenta los atributos Secure, HttpOnly y SameSite=Strict.

### Respaldo y recuperación

- **AC-59 (RNF-34)**: Dado un entorno en operación durante 30 días, cuando se consulta el historial de respaldos, entonces existe al menos un respaldo por día y los de los últimos 30 días siguen disponibles.
- **AC-60 (RNF-35)**: Dadas las credenciales del entorno principal, cuando se intenta eliminar o modificar un respaldo, entonces la operación es rechazada.
- **AC-61 (RNF-36)**: Dado un respaldo restaurado en un entorno limpio, cuando se abre un estudio que tenía tres archivos asociados, entonces el estudio conserva sus metadatos y sus tres archivos siguen vinculados a él.
- **AC-67 (RNF-59)**: Dado un respaldo restaurado en un entorno limpio, cuando se recorre el listado completo de estudios, entonces ningún estudio referencia un archivo inexistente y no existen archivos sin estudio asociado.
- **AC-68 (RNF-58, RNF-02)**: Dado un respaldo de la base de datos y del almacenamiento de archivos, cuando se restaura sin la clave de cifrado custodiada por separado, entonces los archivos no pueden descifrarse.

## Fuera de Alcance

> Esta sección es la **fuente única** de lo que queda fuera del MVP. Las menciones en RNF-39, RNF-46, RNF-47, RNF-49 y en "Dependencias" son derivadas: ante una discrepancia, prevalece esta lista.

- OCR y extracción automática de texto.
- Búsqueda dentro del contenido de PDFs o imágenes.
- Exportación completa de documentos y metadatos.
- Importación de documentos, metadatos o respaldos.
- Restauración iniciada por el usuario.
- Versionado de documentos.
- Historial de cambios y eliminaciones.
- Diagnósticos médicos.
- Recomendaciones médicas.
- Interpretación automática de resultados.
- Resúmenes generados mediante inteligencia artificial.
- Integraciones con clínicas, laboratorios, obras sociales u hospitales.
- Historia clínica oficial con validez institucional.
- Gestión de turnos médicos.
- Seguimiento de tratamientos.
- Recordatorios de medicación.
- Más de 5 cuentas de usuario.
- Compartir, delegar o transferir estudios entre cuentas.
- Vistas consolidadas del grupo familiar o cuentas con acceso a los datos de otro integrante.
- Autogestión del alta de cuentas desde la interfaz.
- Roles y permisos configurables.
- Aplicaciones nativas para Android o iOS.
- Enlaces públicos para compartir documentos.
- Envío de documentos por correo electrónico, SMS o WhatsApp.
- Notificaciones automáticas.
- Servicios administrados de búsqueda.
- APIs de OCR.
- APIs de inteligencia artificial.
- Edición del contenido interno de los archivos.

## Riesgos y Dependencias

### Riesgos y mitigaciones

#### Acceso no autorizado

**Riesgo**: una persona obtiene acceso a documentos médicos privados.

Mitigación:

- Autenticación obligatoria.
- Sesiones con duración máxima.
- Bloqueo por inactividad.
- Cookies seguras.
- Ausencia de URLs públicas permanentes.
- Validación de autorización antes de entregar cada archivo.

#### Archivos maliciosos o inválidos

**Riesgo**: se carga un archivo ejecutable, corrupto, manipulado o con contenido activo.

Mitigación:

- Lista explícita de formatos permitidos.
- Validación de extensión, MIME y firma binaria.
- Límite de tamaño.
- Rechazo de archivos vacíos o corruptos.
- Visualización sin contenido activo.
- Almacenamiento privado separado del servidor web.
- Eliminación de archivos rechazados.

#### Pérdida de información

**Riesgo**: un error técnico elimina documentos o metadatos.

Mitigación:

- Respaldos automáticos diarios, que abarcan la base de datos y el almacenamiento de archivos en un mismo punto en el tiempo.
- Almacenamiento de respaldos separado.
- Custodia de la clave de cifrado fuera del entorno principal y fuera de los respaldos: un respaldo sin la clave no
  permite descifrar, y la pérdida del entorno no implica la pérdida de la clave.
- Pruebas trimestrales de recuperación.
- Hash SHA-256 para validar la integridad de los archivos.
- Monitoreo del proceso de respaldo.

#### Exposición mediante logs o telemetría

**Riesgo**: información médica aparece en logs, herramientas de monitoreo o reportes de errores.

Mitigación:

- No registrar contenido médico ni metadatos identificables.
- Utilizar identificadores técnicos internos.
- Sanitizar mensajes de error y excepciones.
- Utilizar información ficticia o anonimizada en desarrollo y pruebas.
- Evitar herramientas de seguimiento de comportamiento.

#### Costos operativos crecientes

**Riesgo**: el almacenamiento, tráfico o servicios administrados incrementan el costo mensual.

Mitigación:

- Utilizar búsqueda nativa de la base de datos (SQLite embebido, sin costo de servicio administrado).
- Evitar servicios OCR y de inteligencia artificial.
- Evitar motores de búsqueda administrados.
- Definir un límite mensual de infraestructura.
- Medir almacenamiento y transferencia de datos.
- Priorizar servicios con planes gratuitos o costos predecibles.

#### Crecimiento excesivo del alcance

**Riesgo**: incorporar funciones no necesarias para resolver el problema principal.

Mitigación:

- Mantener el fuera de alcance de forma explícita.
- No agregar funciones que no estén vinculadas con un requerimiento funcional.
- Validar el MVP únicamente contra los criterios de aceptación definidos.
- Priorizar la carga, organización, seguridad, visualización y búsqueda por metadatos.

### Dependencias

El MVP dependerá de:

- Un proveedor de hosting o infraestructura.
- Una base de datos SQLite (archivo único en el servidor) para almacenar metadatos, sin servicio de base de datos administrado.
- Un sistema de almacenamiento privado para los archivos.
- Un certificado HTTPS válido.
- Un mecanismo de autenticación seguro.
- Un proceso automatizado de respaldo.
- Un almacenamiento separado para los respaldos.
- Un mecanismo de custodia de la clave de cifrado, independiente del entorno principal y de los respaldos.
- Navegadores compatibles con PWA.

El MVP **no** dependerá de APIs de inteligencia artificial, servicios de OCR, herramientas de importación o exportación, motores de búsqueda administrados, aplicaciones móviles nativas, integraciones con instituciones médicas ni servicios de mensajería o correo electrónico. El detalle y el alcance de estas exclusiones se define en [Fuera de Alcance](#fuera-de-alcance).

---

## Anexos (secciones fuera del template del curso)

### Personas

**Usuario principal**

Cada integrante del grupo familiar (hasta 5) que desea conservar sus estudios médicos en un repositorio privado y acceder a ellos desde distintos dispositivos, sin que el resto de las cuentas pueda verlos. Necesita:

- Cargar documentos con pocos pasos.
- Organizar estudios por fecha, profesional e institución.
- Encontrar rápidamente un estudio.
- Visualizar o descargar el archivo original.
- Mantener protegida su información médica.
- Utilizar la aplicación sin configuraciones técnicas complejas.

**Administrador técnico**

En el MVP, el administrador técnico será uno de los integrantes del grupo familiar que utiliza la aplicación. Necesita:

- Desplegar y actualizar el sistema.
- Dar de alta las cuentas de los demás integrantes mediante el procedimiento administrativo (RNF-54).
- Supervisar el funcionamiento de la aplicación.
- Verificar que los respaldos de infraestructura se ejecuten.
- Detectar errores sin registrar información médica en logs.
- Mantener controlados los costos operativos.

### Alcance del MVP

El MVP incluirá exclusivamente:

- Autenticación de hasta 5 usuarios, con aislamiento total de los datos entre cuentas.
- Carga segura de documentos médicos.
- Registro y edición manual de metadatos.
- Organización por fecha, profesional e institución.
- Búsqueda sobre los metadatos ingresados manualmente.
- Filtros combinables.
- Visualización individual de archivos.
- Descarga individual de archivos.
- Agrupación de varios archivos dentro de un mismo estudio.
- Eliminación de estudios con confirmación.
- Funcionamiento como PWA.
- Interfaz adaptable a computadoras, tabletas y teléfonos.
- Cifrado de archivos y comunicaciones.
- Protección, vencimiento y bloqueo de sesiones.
- Validación del tipo y contenido de los archivos cargados.
- Respaldos automáticos administrados a nivel de infraestructura.

El MVP no extraerá texto de documentos ni permitirá importar, exportar o restaurar información desde la interfaz de usuario. Las exclusiones completas se enumeran en [Fuera de Alcance](#fuera-de-alcance).

### Restricciones técnicas y económicas

- La selección tecnológica se definirá en una especificación técnica separada.
- Se priorizarán tecnologías conocidas por el desarrollador.
- El MVP deberá poder desplegarse utilizando una única aplicación, una base de datos y un sistema de almacenamiento.
- La base de datos será SQLite: un único archivo en disco del propio servidor, sin motor de base de datos separado ni servicio administrado. El archivo deberá ubicarse fuera de toda carpeta pública del servidor web, junto con sus archivos auxiliares (`-wal`, `-shm`).
- La base deberá operar en modo WAL, adecuado para las hasta 5 cuentas concurrentes previstas; el MVP no contempla escrituras de alta concurrencia.
- La normalización de texto para búsqueda (minúsculas, sin acentos) se resolverá en la aplicación y se persistirá en columnas normalizadas, porque SQLite no provee intercalaciones insensibles a acentos.
- La búsqueda por subcadena sobre esas columnas normalizadas es suficiente para el volumen definido en RNF-24; el MVP no incorporará un índice de texto completo.
- No se utilizará una arquitectura de microservicios.
- La aplicación deberá permitir reemplazar el proveedor de almacenamiento sin modificar las reglas principales del dominio.
- La búsqueda se realizará exclusivamente sobre metadatos.
- El costo operativo objetivo será de hasta USD 15 mensuales, sin considerar el dominio.
- Cualquier servicio externo deberá justificar su costo mediante una mejora medible.
- Los archivos médicos no deberán almacenarse directamente en una carpeta pública del servidor web.
- Los secretos, claves y cadenas de conexión no deberán almacenarse en el código fuente.

### Indicadores de éxito

El MVP será considerado exitoso cuando:

- El usuario pueda crear un estudio con hasta tres archivos en menos de 60 segundos.
- El usuario pueda encontrar un estudio conocido en menos de 10 segundos.
- El 100 % de las páginas privadas requieran autenticación.
- El 100 % de los archivos privados requieran autenticación y autorización.
- El 100 % de los archivos rechazados permanezcan fuera del almacenamiento definitivo.
- El hash de un archivo descargado coincida con el archivo original.
- Los logs técnicos no contengan información médica ni metadatos identificables.
- La aplicación pueda utilizarse desde una computadora, tableta y teléfono.
- La PWA no muestre información médica cuando el usuario no esté autenticado.
- Los respaldos automáticos se ejecuten diariamente.
- Una prueba de recuperación de infraestructura pueda reconstruir la relación entre estudios y archivos.
- El costo mensual permanezca dentro del límite definido.
- Todos los criterios de aceptación hayan sido validados.
