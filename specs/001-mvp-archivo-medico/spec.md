# Especificación de Feature: MVP Mi Archivo Médico

**Rama**: `v4-sdd`

**Directorio de la feature**: `specs/001-mvp-archivo-medico`

**Creada**: 2026-08-19

**Estado**: Borrador

**Entrada**: Descripción del usuario: "Generá el spec a partir del PRD en docs/PRD2.md"

**Fuente de alcance**: `PRD2.md` (revisión 2, endurecida) en la raíz del repositorio. Esta especificación
**no** amplía el PRD: lo traduce a historias de usuario verificables y traza cada requisito a sus
identificadores RF/RNF/AC, según el Principio II de la constitución del proyecto.

---

## Clarificaciones

### Sesión 2026-08-19

- Q: ¿Qué debe ocurrir con el contenido de los archivos cuando se confirma la eliminación de un estudio? → A: Borrado físico: se borran la fila del estudio y los archivos cifrados del disco en la misma operación; el cupo se libera de inmediato y no queda estado "eliminado" en el modelo.
- Q: Si al crear un estudio uno de los archivos adjuntos falla la validación, ¿qué debe hacer el sistema con el resto? → A: Carga parcial con aviso: el estudio se crea con los archivos válidos y el rechazado se informa junto a ese archivo, indicando el motivo, para poder reintentarlo solo a él.
- Q: ¿En qué momento debe rechazarse una carga por el cupo compartido de 20 GB? → A: Cuando el tamaño de la carga, sumado a lo ya ocupado, superaría los 20 GB, evaluado antes de escribir nada en el almacenamiento definitivo y medido sobre el espacio ocupado en disco.
- Q: ¿Qué reglas debe cumplir la fecha de un estudio para considerarse válida? → A: Fecha de calendario sin hora; se rechaza la inexistente o mal formada y la posterior al día de hoy. El filtro por rango incluye ambos extremos.
- Q: ¿Cómo debe elegir el usuario la institución al filtrar el listado de estudios? → A: Con una lista desplegable de las instituciones distintas presentes en sus propios estudios, ordenadas alfabéticamente y con coincidencia exacta sobre el valor elegido.

## Escenarios de Usuario y Pruebas *(obligatorio)*

Las historias están ordenadas por prioridad. Cada una es una porción independientemente construible,
probable y demostrable: implementando solo la primera ya existe un sistema que protege datos; con las
dos primeras ya hay valor de producto entregable.

### Historia de Usuario 1 - Acceso privado y aislado (Prioridad: P1)

Un integrante del grupo familiar abre la aplicación e inicia sesión con las credenciales que le entregó
el administrador técnico. Desde ese momento ve exclusivamente su propio espacio. Si no inicia sesión, no
ve absolutamente nada: ni una pantalla con estudios, ni un archivo, ni un metadato. Si otra persona
conoce el identificador de un estudio ajeno y lo escribe en la dirección, el sistema se comporta como si
ese estudio no existiera. La sesión se cierra sola por inactividad y tiene un tope de duración, y una
cuenta atacada a fuerza bruta queda bloqueada temporalmente sin revelar si el usuario existe.

**Por qué esta prioridad**: es la garantía que hace posible que cinco personas compartan una instalación
sin compartir sus datos. Sin ella ninguna otra funcionalidad puede entregarse: cargar un estudio en un
sistema que no aísla es peor que no tener sistema.

**Prueba independiente**: se puede verificar por completo sembrando dos cuentas y algunos estudios de
cada una, y comprobando que sin sesión toda ruta privada redirige o rechaza, que con sesión de la cuenta
A los recursos de la cuenta B responden 403/404, y que las reglas de expiración y bloqueo se cumplen.
Entrega valor por sí sola: es el control de acceso completo del sistema.

**Escenarios de Aceptación**:

1. **Dado** un usuario no autenticado, **cuando** intenta abrir la página de estudios, **entonces** el
   sistema lo redirige al inicio de sesión y no muestra información médica. *(AC-01)*
2. **Dada** la URL interna de un archivo, **cuando** una persona no autenticada la solicita,
   **entonces** el sistema responde 401 o 403 y no entrega el archivo. *(AC-02)*
3. **Dadas** credenciales válidas, **cuando** el usuario inicia sesión, **entonces** accede al listado de
   estudios. *(AC-03)*
4. **Dadas** credenciales inválidas, **cuando** el usuario intenta iniciar sesión, **entonces** el
   sistema rechaza el acceso con un mensaje idéntico tanto si el usuario no existe como si la contraseña
   es incorrecta. *(AC-04)*
5. **Dada** una sesión autenticada, **cuando** el usuario cierra sesión, **entonces** la sesión deja de
   ser válida. *(AC-05)*
6. **Dada** una sesión sin actividad durante 30 minutos, **cuando** el usuario vuelve a interactuar,
   **entonces** debe autenticarse nuevamente. *(AC-06)*
7. **Dada** una sesión iniciada hace más de 24 horas, **cuando** el usuario realiza una nueva solicitud,
   **entonces** el sistema exige una nueva autenticación. *(AC-07)*
8. **Dada** una cuenta con 5 intentos fallidos dentro de una ventana de 15 minutos, **cuando** se
   realiza un sexto intento con la contraseña correcta antes de cumplirse 15 minutos desde el quinto
   fallo, **entonces** el sistema rechaza el acceso con el mismo mensaje de credenciales inválidas.
   *(AC-69)*
9. **Dada** esa cuenta bloqueada, **cuando** se intenta con la contraseña correcta pasados los 15
   minutos, **entonces** el acceso se concede. *(AC-86)*
10. **Dada** una cuenta con 4 fallos, un ingreso exitoso y luego 4 fallos más, **cuando** se evalúa su
    estado, **entonces** no está bloqueada, porque el ingreso exitoso reinició el contador. *(AC-87)*
11. **Dado** un estudio de otro propietario, **cuando** un usuario autenticado solicita su detalle
    sustituyendo el identificador en la URL, **entonces** el sistema responde 403 o 404 y no entrega
    metadatos. *(AC-47)*
12. **Dado** un archivo de otro propietario, **cuando** un usuario autenticado solicita su descarga
    sustituyendo el identificador, **entonces** el sistema responde 403 o 404 y no entrega el archivo.
    *(AC-48)*
13. **Dados** estudios de dos propietarios, **cuando** un usuario abre el listado y busca sin filtros,
    **entonces** solo aparecen los propios y el contador no incluye los ajenos. *(AC-49)*
14. **Dada** la aplicación desplegada, **cuando** se enumeran sus rutas, **entonces** ninguna acepta una
    creación de cuenta y una solicitud a una ruta de registro responde 404. *(AC-50)*
15. **Dadas** 5 cuentas activas, **cuando** se intenta dar de alta una sexta, **entonces** el alta se
    rechaza informando que se alcanzó el límite. *(AC-62)*
16. **Dada** la aplicación desplegada, **cuando** se enumeran sus rutas y las acciones del detalle de un
    estudio, **entonces** ninguna permite asignar, copiar o autorizar un estudio a otro propietario, y
    un intento construido a mano responde 403, 404 o 405. *(AC-63)*
17. **Dada** una cuenta creada, **cuando** se inspecciona el valor almacenado de su contraseña,
    **entonces** no coincide con la contraseña en claro y su algoritmo y parámetros corresponden a una
    de las tres combinaciones admitidas. *(AC-76)*
18. **Dada** una sesión iniciada, **cuando** se inspecciona la cookie de autenticación, **entonces**
    presenta Secure, HttpOnly y SameSite=Strict. *(AC-58)*

---

### Historia de Usuario 2 - Guardar un estudio con sus archivos (Prioridad: P1)

El usuario acaba de recibir un informe en PDF y dos fotos de una placa. Entra a la aplicación, crea un
estudio con su título y su fecha, agrega opcionalmente profesional, institución, descripción y
etiquetas, adjunta los tres archivos y confirma. El sistema valida cada archivo antes de darlo por
guardado: formato permitido, coherencia entre extensión, tipo declarado y contenido real, tamaño, y que
no esté vacío ni truncado. Lo que no pasa la validación no llega al almacenamiento, y lo que sí pasa
queda cifrado, con su huella registrada y con un nombre físico que no revela nada.

**Por qué esta prioridad**: es la razón de existir del producto —dejar de perder estudios— y todo lo
demás (buscar, ver, descargar) opera sobre lo que esta historia produce.

**Prueba independiente**: se prueba end-to-end creando estudios con archivos válidos e inválidos y
verificando qué queda almacenado, en qué estado y con qué huella, sin necesidad de que existan búsqueda,
edición ni PWA.

**Escenarios de Aceptación**:

1. **Dado** un título y una fecha válidos, **cuando** el usuario crea un estudio, **entonces** queda
   almacenado y aparece en el listado. *(AC-09)*
2. **Dado** un título vacío, **cuando** se intenta crear el estudio, **entonces** no se crea y se muestra
   un error. *(AC-10)*
3. **Dada** una fecha inválida, **cuando** se intenta crear el estudio, **entonces** no se crea y se
   muestra un error. *(AC-11)*
4. **Dado** un estudio con un informe PDF y dos imágenes, **cuando** se completa la carga, **entonces**
   los tres archivos aparecen agrupados dentro del mismo estudio. *(AC-12)*
5. **Dado** un estudio existente, **cuando** el usuario agrega un profesional, **entonces** queda
   almacenado y se muestra en el detalle. *(AC-13)*
6. **Dado** un estudio existente, **cuando** el usuario agrega una institución, **entonces** queda
   almacenada y se muestra en el detalle. *(AC-88)*
7. **Dado** un estudio existente, **cuando** el usuario agrega una descripción, **entonces** queda
   almacenada y se muestra en el detalle. *(AC-82)*
8. **Dado** un estudio existente, **cuando** el usuario agrega dos etiquetas, **entonces** ambas quedan
   almacenadas y se muestran en el detalle. *(AC-89)*
9. **Dado** un PDF válido de menos de 50 MB, **cuando** se carga, **entonces** el sistema lo acepta.
   *(AC-20)*
10. **Dado** un JPG válido de menos de 50 MB con extensión, tipo declarado y firma compatibles,
    **cuando** se carga, **entonces** se acepta y queda asociado al estudio. *(AC-74)*
11. **Dado** un PNG válido en las mismas condiciones, **cuando** se carga, **entonces** se acepta y queda
    asociado al estudio. *(AC-75)*
12. **Dado** un archivo de más de 50 MB, **cuando** se intenta cargar, **entonces** se rechaza antes de
    almacenarlo definitivamente. *(AC-21)*
13. **Dado** un ejecutable renombrado con extensión .pdf, **cuando** se intenta cargar, **entonces** el
    sistema detecta la incompatibilidad y lo rechaza. *(AC-22)*
14. **Dado** un archivo .jpg cuya firma corresponde a otro formato, **cuando** se intenta cargar,
    **entonces** se rechaza. *(AC-23)*
15. **Dado** un archivo de 0 bytes, **cuando** se intenta cargar, **entonces** se rechaza. *(AC-24)*
16. **Dado** un PDF truncado sin la marca de fin, **cuando** se intenta cargar, **entonces** se rechaza
    por no superar la validación estructural de su formato. *(AC-44)*
17. **Dado** un archivo rechazado, **cuando** finaliza la operación, **entonces** no existe en el
    almacenamiento definitivo. *(AC-27)*
18. **Dado** un archivo cargado correctamente, **cuando** finaliza la operación, **entonces** el sistema
    registra su huella SHA-256. *(AC-25)*
19. **Dado** un archivo cargado con el nombre `informe.pdf`, **cuando** se inspecciona el almacenamiento,
    **entonces** el nombre físico es un GUID generado por el sistema y no contiene ninguna porción del
    nombre original. *(AC-65)*
20. **Dado** un archivo cuyo nombre original es `../../etc/passwd.pdf`, **cuando** se carga, **entonces**
    el metadato almacenado no contiene separadores de ruta ni secuencias `..`, conserva la extensión
    `.pdf` y se muestra escapado en la interfaz. *(AC-66)*
21. **Dado** un archivo cargado correctamente, **cuando** se inspecciona su contenido directamente en el
    almacenamiento sin pasar por la aplicación, **entonces** los bytes no corresponden al original en
    claro. *(AC-57)*
22. **Dada** una configuración de la que se removió la clave de cifrado, **cuando** se inicia la
    aplicación, **entonces** el proceso termina con error y no queda disponible para atender solicitudes.
    *(AC-83)*
23. **Dado** un estudio con 20 archivos asociados, **cuando** se intenta cargar uno más, **entonces** la
    carga se rechaza informando que se alcanzó el límite de archivos por estudio. *(AC-70)*
24. **Dado** un almacenamiento compartido que alcanzó los 20 GB, **cuando** cualquier cuenta intenta
    cargar un archivo adicional, **entonces** la carga se rechaza informando el límite. *(AC-55)*
25. **Dado** un almacenamiento compartido con espacio libre insuficiente para la carga solicitada,
    **cuando** el usuario la confirma, **entonces** se rechaza antes de escribir en el almacenamiento
    definitivo y el total ocupado sigue sin superar los 20 GB. *(FR-037)*
26. **Dado** ese aviso de límite, **cuando** se muestra al usuario, **entonces** no revela qué cuenta
    consumió el espacio ni ningún metadato de estudios ajenos. *(AC-64)*
27. **Dado** un usuario que inicia la creación de un estudio con título, fecha y un archivo, **cuando**
    completa la operación, **entonces** atraviesa como máximo tres pantallas o pasos. *(AC-79)*
28. **Dado** un formulario de creación con el título vacío y un archivo de 0 bytes, **cuando** se envía,
    **entonces** el error del título se muestra junto al campo título y el del archivo junto a ese
    archivo, no en un aviso general desvinculado del origen. *(AC-80)*
29. **Dado** un estudio nuevo con título y fecha válidos y tres archivos de los cuales el segundo es
    inválido, **cuando** se envía la carga, **entonces** el estudio queda creado con los dos archivos
    válidos, se informa el motivo de rechazo junto al segundo archivo y este no existe en el
    almacenamiento definitivo. *(FR-030b)*

---

### Historia de Usuario 3 - Consultar, descargar y eliminar (Prioridad: P2)

En el consultorio, el usuario abre su listado —lo más reciente primero—, entra a un estudio y ve el
informe dentro de la aplicación sin tener que descargarlo. Si lo necesita fuera de la aplicación, lo
descarga y obtiene exactamente el archivo original, byte por byte. Cuando un estudio ya no le sirve, lo
elimina, y el sistema le pide confirmación antes de hacerlo irreversible.

**Por qué esta prioridad**: completa el ciclo de vida del estudio y es lo que se usa durante una consulta
médica, pero requiere que ya exista contenido cargado (Historia 2).

**Prueba independiente**: con estudios sembrados, se verifica el orden del listado, la visualización
embebida, la identidad de la huella tras la descarga y el flujo de eliminación con y sin confirmación.

**Escenarios de Aceptación**:

1. **Dados** estudios con fechas diferentes, **cuando** se abre el listado, **entonces** aparecen del más
   reciente al más antiguo. *(AC-28)*
2. **Dado** un archivo válido asociado a un estudio, **cuando** el usuario elige "Visualizar",
   **entonces** el contenido se muestra dentro de la aplicación sin que deba descargarlo. *(AC-15)*
3. **Dado** un PDF con JavaScript embebido, **cuando** se visualiza, **entonces** el script no se
   ejecuta. *(AC-26)*
4. **Dado** un archivo almacenado, **cuando** el usuario elige "Descargar", **entonces** recibe un
   archivo cuya huella SHA-256 coincide con la registrada. *(AC-16)*
5. **Dado** un archivo con huella conocida antes de la carga, **cuando** se lo carga, se lo visualiza y
   se lo descarga, **entonces** la huella del archivo descargado es idéntica a la previa a la carga.
   *(AC-77)*
6. **Dado** el listado y el detalle de un estudio con archivos, **cuando** el usuario los abre sin elegir
   "Visualizar" ni "Descargar", **entonces** no se transfiere el contenido de ningún archivo médico.
   *(AC-78)*
7. **Dado** un estudio existente, **cuando** el usuario elige "Eliminar", **entonces** el sistema pide
   confirmación antes de ejecutar la operación. *(AC-17)*
8. **Dada** una solicitud de eliminación, **cuando** el usuario cancela la confirmación, **entonces** el
   estudio y sus archivos siguen disponibles. *(AC-18)*
9. **Dada** una eliminación confirmada, **cuando** finaliza, **entonces** el estudio y sus archivos dejan
   de estar disponibles. *(AC-19)*
10. **Dada** la URL con la que un usuario autorizado accedió a un archivo, **cuando** se la solicita sin
    sesión válida, de inmediato y pasados 5 minutos, **entonces** en ningún caso se entrega el archivo.
    *(AC-84)*
11. **Dada** una URL temporal generada hace más de 5 minutos, **cuando** se intenta usar, **entonces** el
    acceso al archivo se rechaza. *(AC-08)*

---

### Historia de Usuario 4 - Encontrar un estudio en segundos (Prioridad: P2)

El usuario recuerda parte de algo: el nombre de la doctora, la clínica, una palabra del título o una
etiqueta que puso hace dos años. Escribe ese fragmento —con o sin acentos, en mayúsculas o minúsculas,
con espacios de más— y el sistema le devuelve los estudios que coinciden, con la cuenta de resultados a
la vista. Puede acotar por rango de fechas o por institución, combinar todo, y volver al listado
completo con un solo clic.

**Por qué esta prioridad**: es el objetivo declarado del producto (encontrar un estudio conocido en menos
de 10 segundos), pero sin contenido cargado no tiene nada que buscar.

**Prueba independiente**: con un conjunto sembrado de estudios se ejercitan búsquedas por cada campo,
insensibilidad a mayúsculas y acentos, cada filtro por separado y combinados, el contador y la
paginación.

**Escenarios de Aceptación**:

1. **Dado** un estudio cuya institución es "Hospital Central", **cuando** el usuario busca "Central",
   **entonces** el estudio aparece entre los resultados. *(AC-29)*
2. **Dado** un estudio etiquetado como "cardiología", **cuando** el usuario busca "cardiología",
   **entonces** el estudio aparece entre los resultados. *(AC-30)*
3. **Dado** un estudio cuyo título es "Ecografía abdominal", **cuando** el usuario busca "abdominal",
   **entonces** el estudio aparece entre los resultados. *(AC-71)*
4. **Dado** un estudio cuya descripción es "control anual de rutina", **cuando** el usuario busca
   "rutina", **entonces** el estudio aparece entre los resultados. *(AC-72)*
5. **Dado** un estudio cuyo profesional es "Dra. Rivas", **cuando** el usuario busca "Rivas",
   **entonces** el estudio aparece entre los resultados. *(AC-73)*
6. **Dado** un estudio cuya institución es "Hospital Central", **cuando** el usuario busca
   " hospital central " en minúsculas y con espacios sobrantes, **entonces** el estudio aparece entre los
   resultados. *(AC-45)*
7. **Dado** un estudio etiquetado como "cardiología", **cuando** el usuario busca "cardiologia" sin
   acento, **entonces** el estudio aparece entre los resultados. *(AC-46)*
8. **Dados** estudios de distintos años, **cuando** se aplica un rango de fechas, **entonces** solo
   aparecen los incluidos en el rango. *(AC-31)*
9. **Dados** estudios de diferentes instituciones, **cuando** se filtra por una institución,
   **entonces** solo aparecen los estudios asociados. *(AC-34)*
10. **Dados** estudios de dos propietarios con instituciones distintas, **cuando** un usuario abre la
   lista de instituciones del filtro, **entonces** solo figuran las que aparecen en sus propios
   estudios. *(FR-050, RNF-53)*
11. **Dados** estudios de distintas instituciones, **cuando** el usuario busca "ecografía" y filtra por
    "Hospital Central", **entonces** solo aparecen los que cumplen ambas condiciones. *(AC-35)*
12. **Dados** varios filtros activos, **cuando** el usuario elige "Limpiar filtros", **entonces** se
    restablece el listado completo. *(AC-36)*
13. **Dada** una búsqueda con cinco resultados, **cuando** se muestra el listado, **entonces** la
    interfaz informa que se encontraron cinco estudios. *(AC-37)*
14. **Dada** una colección de 26 estudios, **cuando** se abre el listado, **entonces** se muestran como
    máximo 25 y existe un control para avanzar a la página siguiente. *(AC-54)*

---

### Historia de Usuario 5 - Corregir los datos de un estudio (Prioridad: P3)

El usuario cargó un estudio con la institución mal escrita, o quiere agregarle una etiqueta que se le
ocurrió después. Abre el estudio, edita sus metadatos y guarda. Los archivos originales no se tocan.

**Por qué esta prioridad**: mejora real de uso, pero el MVP es utilizable sin ella —se puede eliminar y
volver a crear—, así que cede ante carga, consulta y búsqueda.

**Prueba independiente**: se edita cada metadato de un estudio existente y se verifica que el cambio
persiste y que la huella de sus archivos no cambió.

**Escenarios de Aceptación**:

1. **Dado** un estudio existente, **cuando** se modifica su institución, **entonces** el metadato se
   actualiza y la huella del archivo original no cambia. *(AC-14)*
2. **Dado** un estudio de otro propietario, **cuando** un usuario autenticado intenta editarlo,
   **entonces** el sistema responde 403 o 404 y no modifica nada. *(AC-47, RNF-53)*

---

### Historia de Usuario 6 - Tenerlo a mano en el teléfono (Prioridad: P3)

El usuario instala la aplicación desde el navegador de su teléfono y le queda un ícono propio. Cuando se
queda sin señal en la sala de espera, la aplicación no muestra basura ni datos viejos: muestra una
pantalla que dice claramente que no hay conexión. Si estaba subiendo un archivo cuando se cayó la red,
se entera de que no se cargó. Todo funciona en una pantalla angosta sin tener que desplazarse de
costado.

**Por qué esta prioridad**: mejora el acceso en el momento de la consulta médica, pero el producto ya es
usable desde el navegador sin instalarlo.

**Prueba independiente**: se verifica la instalación, la pantalla sin conexión, el aviso de carga
interrumpida y las acciones principales a 360 px, sin depender de búsqueda ni edición.

**Escenarios de Aceptación**:

1. **Dado** un navegador compatible, **cuando** el usuario instala la aplicación, **entonces** el sistema
   operativo registra un ícono propio y al abrirlo la aplicación se muestra en una ventana sin la barra
   de direcciones. *(AC-38)*
2. **Dada** una pérdida de conexión, **cuando** el usuario navega, **entonces** se muestra una pantalla
   que indica explícitamente la falta de conexión y que no contiene estudios, metadatos ni archivos.
   *(AC-40)*
3. **Dado** un usuario no autenticado, **cuando** abre la aplicación instalada sin conexión,
   **entonces** no se muestran estudios ni metadatos médicos vistos anteriormente. *(AC-41)*
4. **Dada** una carga interrumpida por pérdida de conexión, **cuando** la operación falla, **entonces**
   el sistema informa que el archivo no fue cargado. *(AC-42)*
5. **Dada** una pantalla de 360 píxeles de ancho, **cuando** se ejecuta cada una de las diez acciones
   principales, **entonces** todas se completan sin desplazamiento horizontal. *(AC-39)*

---

### Historia de Usuario 7 - Operar el sistema sin filtrar ni perder datos (Prioridad: P3)

El administrador técnico —que es uno de los cinco integrantes— despliega la aplicación, da de alta las
cuentas por fuera de la interfaz, restablece una contraseña olvidada por el mismo camino, verifica que
los respaldos corran todos los días y que puedan restaurarse, y revisa los logs cuando algo falla sin
encontrarse jamás con el título de un estudio ni el nombre de un médico.

**Por qué esta prioridad**: sostiene la continuidad y la privacidad en producción. Es P3 en orden de
construcción porque buena parte se verifica por inspección de configuración e infraestructura, no por
código de la aplicación, pero es condición para poner el sistema en manos de la familia.

**Prueba independiente**: se valida con una instalación desplegada, un ciclo de respaldo y una prueba de
restauración en un entorno limpio, más la inspección de logs y métricas durante un ciclo de vida
completo de un estudio ficticio.

**Escenarios de Aceptación**:

1. **Dado** un estudio de prueba cuyos metadatos y nombre de archivo son cadenas únicas e irrepetibles,
   **cuando** se lo crea, visualiza y elimina, **entonces** ninguna de esas cadenas aparece en la
   totalidad de los logs técnicos generados. *(AC-43)*
2. **Dado** ese mismo estudio de prueba, **cuando** se lo crea, visualiza y elimina, **entonces** ninguna
   de esas cadenas aparece en las métricas técnicas emitidas, incluidos nombres de métrica, etiquetas y
   valores. *(AC-85)*
3. **Dada** una solicitud sin cifrar hacia cualquier ruta, **cuando** el servidor responde, **entonces**
   redirige a HTTPS y la conexión negociada usa TLS 1.2 o superior. *(AC-56)*
4. **Dado** un entorno en operación durante 30 días, **cuando** se consulta el historial de respaldos,
   **entonces** existe al menos un respaldo por día y los de los últimos 30 días siguen disponibles.
   *(AC-59)*
5. **Dadas** las credenciales del entorno principal, **cuando** se intenta eliminar o modificar un
   respaldo, **entonces** la operación es rechazada. *(AC-60)*
6. **Dado** un respaldo restaurado en un entorno limpio, **cuando** se abre un estudio que tenía tres
   archivos, **entonces** conserva sus metadatos y sus tres archivos siguen vinculados. *(AC-61)*
7. **Dado** un respaldo restaurado en un entorno limpio, **cuando** se recorre el listado completo,
   **entonces** ningún estudio referencia un archivo inexistente y no hay archivos huérfanos. *(AC-67)*
8. **Dado** un respaldo de la base y del almacenamiento, **cuando** se restaura sin la clave de cifrado
   custodiada por separado, **entonces** los archivos no pueden descifrarse. *(AC-68)*

---

### Casos Límite

- **Fecha en el borde**: un estudio fechado hoy se acepta y uno fechado mañana se rechaza; el filtro por
  rango devuelve los estudios fechados exactamente en el día inicial y en el día final (FR-017, FR-049).
- **Sesión al filo**: una solicitud que llega exactamente al minuto 30 de inactividad, o a la hora 24 de
  vida de la sesión, debe resolverse como expirada y exigir autenticación (RNF-04, RNF-05).
- **Bloqueo y competencia**: intentos fallidos de dos orígenes distintos contra la misma cuenta suman al
  mismo contador; el bloqueo es por cuenta y se acepta que un tercero que conozca un nombre de usuario
  pueda dejar a esa cuenta sin acceso 15 minutos (riesgo aceptado en el PRD).
- **Sexto archivo sobre el límite y último byte del cupo**: una carga debe rechazarse si supera los 20
  archivos por estudio o si su tamaño llevaría el total por encima de los 20 GB, sin dejar archivos
  parcialmente aceptados ni residuos en el almacenamiento definitivo (FR-023, FR-037, RNF-21).
- **Archivo que se corrompe a mitad de la subida**: la conexión se corta con el cuerpo incompleto; el
  usuario debe recibir un aviso de que el archivo no se cargó, y nada debe quedar en el almacenamiento
  definitivo (RF-28, RNF-21).
- **Carga mixta**: en una carga de varios archivos donde solo algunos son inválidos, los válidos quedan
  asociados al estudio, los inválidos se informan uno por uno y ninguno de estos últimos llega al
  almacenamiento definitivo (FR-030, FR-030b).
- **Todos los archivos rechazados**: si ningún archivo de la carga es válido pero el título y la fecha
  lo son, el estudio se crea sin archivos y se informa el rechazo de cada uno (FR-030b).
- **Nombre de archivo hostil**: separadores de ruta, secuencias `..`, caracteres de control, nombres de
  más de 255 caracteres y nombres con marcado que podría interpretarse en la interfaz (RNF-23).
- **Extensión, tipo declarado y contenido en desacuerdo**: cualquier combinación de las tres que no sea
  coherente se rechaza, incluido un archivo cuyo contenido real sea un formato permitido pero distinto
  del declarado (RNF-15, RNF-16).
- **Filtro de institución sin opciones**: un usuario cuyos estudios no tienen institución cargada ve la
  lista de filtro vacía o deshabilitada, no un filtro que no devuelve nada sin explicación (FR-050).
- **Búsqueda sin resultados y término solo con espacios**: el contador debe informar cero resultados y
  el término normalizado vacío debe comportarse como ausencia de búsqueda, no como error (RF-23,
  RNF-55).
- **Estudio sin archivos**: un estudio con título y fecha válidos pero sin ningún archivo adjunto es un
  estado alcanzable; el listado, el detalle y la eliminación deben funcionar sobre él (RF-07 admite
  "uno o más" archivos asociados, pero no los exige en la creación).
- **Identificador ajeno, inexistente o mal formado**: los tres casos deben ser indistinguibles desde
  afuera —403 o 404— para no filtrar la existencia de recursos de otras cuentas (RNF-53).
- **Última cuenta y cuenta bloqueada**: alcanzado el máximo de 5 cuentas, el alta de una sexta se
  rechaza; el límite se evalúa sobre cuentas activas (RNF-56).
- **Arranque sin configuración crítica**: sin clave de cifrado o sin cadena de conexión, la aplicación
  no debe iniciar ni atender solicitudes en modo degradado (RNF-62).
- **Eliminación con el cupo lleno**: eliminado un estudio, el espacio que ocupaban sus archivos vuelve
  a estar disponible y una carga que antes se rechazaba por cupo debe poder completarse (FR-037,
  FR-044).
- **Eliminación a medio camino**: si el borrado del contenido de un archivo falla, el estudio no debe
  quedar visible con archivos irrecuperables ni el contenido debe quedar huérfano en el almacenamiento
  (FR-044, RNF-59).
- **Colección en el techo previsto**: con 2.000 estudios y 20 GB ocupados, búsqueda, listado y paginación
  deben seguir dentro de los tiempos comprometidos (RNF-24, RNF-25).

## Requisitos *(obligatorio)*

Cada requisito traza al identificador de `PRD2.md` que lo origina. Los identificadores del PRD son la
referencia autoritativa: si esta sección y el PRD difirieran, prevalece el PRD.

### Requisitos Funcionales

**Autenticación y sesión**

- **FR-001**: El sistema DEBE exigir autenticación antes de mostrar cualquier estudio, archivo o
  metadato. *(RF-01, AC-01, AC-02)*
- **FR-002**: El sistema DEBE permitir iniciar sesión con credenciales válidas. *(RF-02, AC-03)*
- **FR-003**: El sistema DEBE devolver un mensaje de error de autenticación idéntico cuando el usuario no
  existe y cuando la contraseña es incorrecta. *(RNF-13, AC-04)*
- **FR-004**: El sistema DEBE permitir cerrar la sesión manualmente e invalidarla en ese acto. *(RF-03,
  RNF-12, AC-05)*
- **FR-005**: El sistema DEBE expirar la sesión tras 30 minutos de inactividad y exigir una nueva
  autenticación. *(RF-04, RNF-04, AC-06)*
- **FR-006**: El sistema DEBE limitar la duración absoluta de la sesión a 24 horas y exigir una nueva
  autenticación al alcanzarla. *(RF-05, RNF-05, AC-07)*
- **FR-007**: El sistema DEBE rechazar todo inicio de sesión de una cuenta con 5 intentos fallidos dentro
  de una ventana de 15 minutos, mantener el rechazo 15 minutos desde el quinto fallo, reiniciar el
  contador ante un ingreso exitoso y usar durante el bloqueo un mensaje indistinguible del de
  credenciales inválidas. *(RNF-60, AC-69, AC-86, AC-87)*
- **FR-008**: Las cookies de autenticación DEBEN emitirse con Secure, HttpOnly y SameSite=Strict.
  *(RNF-11, AC-58)*
- **FR-009**: Las contraseñas DEBEN almacenarse con Argon2id (memoria ≥ 19 MiB, 2 iteraciones,
  paralelismo 1), bcrypt (costo ≥ 12) o PBKDF2-HMAC-SHA256 con ≥ 100.000 iteraciones. *(RNF-03, AC-76)*
- **FR-010**: La aplicación NO DEBE exponer ninguna ruta de registro de cuentas; el alta se realiza por
  un procedimiento administrativo externo. *(RNF-54, AC-50)*
- **FR-011**: El sistema DEBE admitir un máximo de 5 cuentas activas y rechazar el alta de una adicional
  informando el límite. *(RNF-56, AC-62)*

**Propiedad de los datos y aislamiento**

- **FR-012**: Cada estudio y cada archivo DEBEN tener un propietario, y el sistema DEBE entregar
  exclusivamente los recursos cuyo propietario sea el usuario autenticado, respondiendo 403 o 404 ante
  cualquier solicitud sobre un recurso ajeno aunque se conozca su identificador. *(RNF-53, RNF-08,
  AC-47, AC-48)*
- **FR-013**: El listado, la búsqueda, los filtros y el contador de resultados DEBEN considerar
  únicamente los estudios del usuario autenticado. *(RNF-53, AC-49)*
- **FR-014**: El sistema NO DEBE ofrecer ningún mecanismo para compartir, delegar o transferir estudios a
  otra cuenta, ni roles con visibilidad sobre datos ajenos; un intento construido a mano DEBE responder
  403, 404 o 405. *(RNF-57, AC-63)*

**Creación y metadatos del estudio**

- **FR-015**: Los usuarios DEBEN poder crear un estudio médico. *(RF-33, AC-09)*
- **FR-016**: El sistema DEBE rechazar la creación de un estudio con título vacío. *(RF-34, AC-10)*
- **FR-017**: El sistema DEBE rechazar la creación de un estudio con fecha ausente o inválida. La fecha
  del estudio es una fecha de calendario sin hora, y es inválida si no existe en el calendario, si no
  respeta el formato esperado o si es posterior al día en curso. *(RF-35, AC-11)*
- **FR-018**: Los usuarios DEBEN poder registrar el profesional del estudio como texto libre. *(RF-29,
  AC-13)*
- **FR-019**: Los usuarios DEBEN poder registrar la institución del estudio como texto libre. *(RF-30,
  AC-88)*
- **FR-020**: Los usuarios DEBEN poder registrar la descripción del estudio como texto libre. *(RF-31,
  AC-82)*
- **FR-021**: Los usuarios DEBEN poder registrar una o más etiquetas del estudio como texto libre.
  *(RF-32, AC-89)*
- **FR-022**: Los usuarios DEBEN poder editar los metadatos de un estudio sin que ello altere sus
  archivos. *(RF-10, RNF-18, AC-14)*
- **FR-023**: El sistema DEBE permitir asociar uno o más archivos a un mismo estudio, con un máximo de 20
  por estudio, rechazando la carga que lo supere e informando el límite. *(RF-07, RNF-61, AC-12, AC-70)*
- **FR-024**: La creación de un estudio DEBE completarse en un máximo de tres pantallas o pasos.
  *(RNF-31, AC-79)*
- **FR-025**: Los errores de validación DEBEN mostrarse junto al campo o al archivo que los produjo.
  *(RNF-32, AC-80)*

**Validación y custodia de archivos**

- **FR-026**: El sistema DEBE aceptar archivos PDF, JPG, JPEG y PNG. *(RF-08, AC-20, AC-74, AC-75)*
- **FR-027**: El sistema DEBE aceptar archivos de hasta 50 MB y rechazar los mayores antes de
  almacenarlos definitivamente. *(RNF-14, AC-21)*
- **FR-028**: El sistema DEBE verificar la compatibilidad entre extensión declarada, tipo MIME y firma
  binaria, y rechazar ejecutables, scripts y formatos no permitidos aunque se les haya cambiado la
  extensión. *(RNF-15, RNF-16, AC-22, AC-23)*
- **FR-029**: El sistema DEBE rechazar archivos de 0 bytes y archivos que no superen la validación
  estructural de su formato declarado. *(RNF-17, AC-24, AC-44)*
- **FR-030**: Los archivos rechazados NO DEBEN permanecer en el almacenamiento definitivo. *(RNF-21,
  AC-27)*
- **FR-030b**: Cuando una carga incluya varios archivos y alguno sea rechazado, el sistema DEBE aceptar
  los archivos válidos y crear o actualizar el estudio con ellos, e informar por separado cada archivo
  rechazado junto a su motivo, sin descartar los válidos ni exigir volver a adjuntarlos. El rechazo de
  un archivo NO DEBE impedir la creación del estudio cuando el título y la fecha son válidos. *(RNF-32,
  AC-80; derivado de la sesión de clarificación)*
- **FR-031**: El sistema DEBE calcular y almacenar una huella SHA-256 por cada archivo cargado. *(RNF-19,
  AC-25)*
- **FR-032**: Los archivos originales NO DEBEN modificarse durante la carga, la visualización ni la
  descarga: la huella SHA-256 DEBE ser idéntica antes de la carga, tras almacenarse y tras descargarse.
  *(RNF-18, AC-77)*
- **FR-033**: El nombre físico en el almacenamiento DEBE ser un GUID generado por el sistema, sin ninguna
  porción derivada del nombre original. *(RNF-22, AC-65)*
- **FR-034**: El sistema DEBE sanitizar el nombre original antes de almacenarlo o mostrarlo: sin
  separadores de ruta, sin caracteres de control, sin secuencias `..`, truncado a 255 caracteres,
  conservando la extensión y escapado en la interfaz. *(RNF-23, AC-66)*
- **FR-035**: El 100 % de los archivos DEBE almacenarse cifrado en reposo con AES-256. *(RNF-02, AC-57)*
- **FR-036**: La clave de cifrado NO DEBE residir en el código fuente ni en archivos de configuración
  versionados, y si no puede resolverse desde la configuración externa la aplicación DEBE fallar al
  iniciar en lugar de almacenar archivos sin cifrar. *(RNF-62, AC-83)*
- **FR-037**: El sistema DEBE limitar el almacenamiento total de archivos a 20 GB compartidos entre todas
  las cuentas, e informar el límite sin revelar qué cuenta consumió el espacio ni metadatos ajenos.
  DEBE rechazar toda carga cuyo tamaño, sumado al espacio ya ocupado, superaría el cupo, evaluando la
  condición antes de escribir nada en el almacenamiento definitivo, de modo que el total nunca lo exceda.
  El espacio se mide por el que los archivos ocupan en disco. *(RNF-52, AC-55, AC-64)*

**Consulta, descarga y eliminación**

- **FR-038**: El sistema DEBE listar los estudios del más reciente al más antiguo. *(RF-15, AC-28)*
- **FR-039**: Los usuarios DEBEN poder visualizar individualmente cada archivo del estudio dentro de la
  aplicación, sin necesidad de descargarlo. *(RF-11, AC-15)*
- **FR-040**: La visualización NO DEBE ejecutar macros, scripts, JavaScript embebido ni ningún contenido
  activo del archivo. *(RNF-20, AC-26)*
- **FR-041**: Los usuarios DEBEN poder descargar individualmente cada archivo del estudio. *(RF-12,
  AC-16)*
- **FR-042**: El sistema NO DEBE transferir el contenido de un archivo médico hasta que el usuario
  solicite visualizarlo o descargarlo. *(RNF-28, AC-78)*
- **FR-043**: El sistema DEBE solicitar confirmación explícita antes de eliminar un estudio y NO DEBE
  eliminarlo si la confirmación se cancela. *(RF-13, RNF-33, AC-17, AC-18)*
- **FR-044**: El sistema DEBE eliminar el estudio y sus archivos asociados cuando la eliminación se
  confirma. La eliminación es física y definitiva: la misma operación DEBE borrar los metadatos del
  estudio y el contenido cifrado de cada uno de sus archivos del almacenamiento, sin conservar estado
  "eliminado" ni copia recuperable desde la aplicación. El espacio liberado DEBE volver a estar
  disponible en el cupo compartido de inmediato. *(RF-14, AC-19; el historial de eliminaciones está
  fuera de alcance)*
- **FR-045**: Los archivos privados NO DEBEN estar disponibles mediante URLs públicas permanentes, y toda
  entrega DEBE validar autenticación y autorización. *(RNF-06, RNF-08, AC-84, AC-02)*
- **FR-046**: Las URLs temporales de acceso a archivos DEBEN expirar en un máximo de 5 minutos. *(RNF-07,
  AC-08)*

**Búsqueda, filtros y paginación**

- **FR-047**: Los usuarios DEBEN poder buscar estudios por título, descripción, profesional, institución
  y etiquetas. *(RF-16, AC-29, AC-30, AC-71, AC-72, AC-73)*
- **FR-048**: La búsqueda y los filtros de texto libre DEBEN ser insensibles a mayúsculas y acentos e
  ignorar los espacios al inicio y al final del término ingresado. *(RNF-55, AC-45, AC-46)*
- **FR-049**: Los usuarios DEBEN poder filtrar estudios por rango de fechas, con ambos extremos
  incluidos en el resultado y cada extremo opcional por separado. *(RF-17, AC-31)*
- **FR-050**: Los usuarios DEBEN poder filtrar estudios por institución eligiéndola de una lista de las
  instituciones distintas presentes en sus propios estudios, ordenada alfabéticamente. La coincidencia
  DEBE ser exacta sobre el valor elegido, y la lista NO DEBE incluir instituciones provenientes de
  estudios de otra cuenta. *(RF-20, RNF-53, AC-34)*
- **FR-051**: Los usuarios DEBEN poder combinar la búsqueda textual con uno o más filtros. *(RF-21,
  AC-35)*
- **FR-052**: Los usuarios DEBEN poder limpiar todos los filtros con una única acción. *(RF-22, AC-36)*
- **FR-053**: El sistema DEBE mostrar la cantidad de estudios encontrados tras aplicar una búsqueda o un
  filtro. *(RF-23, AC-37)*
- **FR-054**: El sistema DEBE paginar el listado mostrando como máximo 25 estudios por página y ofrecer
  un control para avanzar. *(RNF-27, AC-54)*

**PWA, disponibilidad y adaptabilidad**

- **FR-055**: El sistema DEBE poder instalarse como PWA en navegadores compatibles, con ícono propio y
  ventana sin barra de direcciones. *(RF-24, AC-38)*
- **FR-056**: El sistema DEBE mostrar una pantalla controlada sin conexión, que indique explícitamente la
  falta de conexión y no contenga estudios, metadatos ni archivos. *(RF-26, AC-40)*
- **FR-057**: El sistema NO DEBE mostrar información médica almacenada previamente cuando el usuario no
  esté autenticado, ni siquiera sin conexión. *(RNF-51, AC-41)*
- **FR-058**: El sistema DEBE informar al usuario cuando una carga no pueda completarse por pérdida de
  conexión. *(RF-28, AC-42)*
- **FR-059**: La interfaz DEBE ser utilizable desde 360 píxeles de ancho, y las diez acciones principales
  DEBEN completarse sin desplazamiento horizontal. *(RNF-29, RNF-30, AC-39)*

**Transporte, privacidad y operación**

- **FR-060**: El 100 % de las comunicaciones DEBE usar HTTPS con TLS 1.2 o superior, redirigiendo las
  solicitudes sin cifrar. *(RNF-01, AC-56)*
- **FR-061**: Los títulos, profesionales, instituciones, descripciones, etiquetas, nombres originales de
  archivo y resultados médicos NO DEBEN registrarse en logs técnicos. *(RNF-09, AC-43)*
- **FR-062**: Esos mismos datos NO DEBEN aparecer en métricas técnicas, incluidos nombres de métrica,
  etiquetas y valores. *(RNF-43, AC-85)*
- **FR-063**: La aplicación NO DEBE incluir publicidad ni herramientas de seguimiento de comportamiento,
  NO DEBE compartir documentos ni metadatos médicos con terceros, y el contenido médico NO DEBE usarse
  para entrenar modelos de inteligencia artificial. *(RNF-40, RNF-41, RNF-42, RNF-44)*
- **FR-064**: La infraestructura DEBE generar al menos un respaldo diario de la base de datos, tomado con
  un mecanismo consistente en caliente, y conservarlo un mínimo de 30 días. *(RNF-34, AC-59)*
- **FR-065**: Los respaldos DEBEN residir en una cuenta o suscripción distinta de la del entorno
  principal, de modo que las credenciales del entorno principal no permitan alterarlos ni eliminarlos.
  *(RNF-35, AC-60)*
- **FR-066**: El respaldo DEBE incluir, por cada estudio, sus metadatos y la referencia a cada archivo
  asociado, de modo que la relación pueda reconstruirse. *(RNF-36, AC-61)*
- **FR-067**: El respaldo diario DEBE abarcar la base de datos y el almacenamiento de archivos en un
  mismo punto en el tiempo, sin dejar estudios con archivos faltantes ni archivos huérfanos al restaurar.
  *(RNF-59, AC-67)*
- **FR-068**: La clave de cifrado DEBE custodiarse fuera del entorno principal y fuera de los respaldos,
  de modo que un respaldo por sí solo no permita descifrar los archivos. *(RNF-58, AC-68)*
- **FR-069**: El responsable técnico DEBE realizar una prueba de recuperación al menos una vez cada tres
  meses. *(RNF-37)*
- **FR-070**: Los respaldos y su restauración DEBEN administrarse exclusivamente a nivel de
  infraestructura, y la interfaz de usuario NO DEBE ofrecer exportación, importación ni restauración.
  *(RNF-38, RNF-39)*
- **FR-071**: El funcionamiento principal NO DEBE depender de APIs de inteligencia artificial, servicios
  de OCR ni motores de búsqueda administrados; la búsqueda DEBE resolverse con las capacidades de la
  base de datos. *(RNF-46, RNF-47, RNF-48, RNF-49)*
- **FR-072**: Todo servicio externo pago que se integre DEBE poder deshabilitarse por configuración, sin
  desplegar código nuevo, y con él deshabilitado DEBEN seguir funcionando la carga, la organización, la
  visualización y la búsqueda por metadatos. *(RNF-50)*
- **FR-073**: Los entornos de desarrollo y prueba DEBEN utilizar exclusivamente documentos y datos
  ficticios o anonimizados. *(RNF-10)*

### Entidades Clave

- **Cuenta**: identidad de un integrante del grupo familiar. Atributos: nombre de usuario, correo,
  credencial almacenada de forma irreversible, estado activo. Máximo 5 activas. Se da de alta y se
  restablece fuera de la aplicación. No tiene roles ni visibilidad sobre otras cuentas.
- **Estudio**: unidad de organización del repositorio. Atributos: título (obligatorio), fecha del
  estudio (obligatoria, fecha de calendario sin hora, no posterior al día en curso), profesional,
  institución, descripción, etiquetas y propietario. Agrupa de 0 a
  20 archivos. Todo estudio pertenece a exactamente una cuenta y nunca cambia de propietario.
- **Archivo asociado**: documento adjunto a un estudio. Atributos: nombre original sanitizado, formato,
  tamaño, huella SHA-256, referencia al contenido almacenado bajo un identificador GUID, propietario
  heredado del estudio. El contenido se guarda cifrado; su nombre físico no deriva del original.
- **Etiqueta**: término libre asociado a un estudio para clasificarlo y buscarlo. Un estudio puede tener
  varias.
- **Sesión**: vínculo autenticado entre una cuenta y un dispositivo, con expiración por inactividad (30
  minutos) y duración máxima absoluta (24 horas). Se invalida al cerrar sesión.
- **Registro de intentos de inicio de sesión**: acumulado por cuenta que sustenta el bloqueo temporal:
  cantidad de fallos dentro de una ventana de 15 minutos y momento del último fallo. Se reinicia con un
  ingreso exitoso.
- **Cupo de almacenamiento**: espacio total ocupado por los archivos de todas las cuentas, con un tope
  compartido de 20 GB y sin cuota individual.

## Criterios de Éxito *(obligatorio)*

### Resultados Medibles

- **SC-001**: Un usuario crea un estudio con hasta tres archivos en menos de 60 segundos.
- **SC-002**: Un usuario encuentra un estudio que sabe que existe en menos de 10 segundos.
- **SC-003**: Sobre una colección de 2.000 estudios, el percentil 95 del tiempo de respuesta de una
  búsqueda por metadatos es inferior a 1 segundo. *(RNF-24, AC-51)*
- **SC-004**: Sobre esa misma colección, el percentil 95 del tiempo de apertura del listado inicial es
  inferior a 2 segundos, sin contar la transferencia de archivos. *(RNF-25, AC-52)*
- **SC-005**: La carga de un estudio con archivos que suman 10 MB finaliza en menos de 15 segundos con
  una conexión estable de 10 Mbps. *(RNF-26, AC-53)*
- **SC-006**: El 100 % de las pantallas que muestran datos médicos exige autenticación; cero pantallas
  privadas accesibles sin sesión.
- **SC-007**: Cero entregas de estudios, metadatos o archivos a una cuenta que no sea su propietaria, en
  el 100 % de los intentos de acceso cruzado ensayados.
- **SC-008**: El 100 % de los archivos rechazados por validación permanece fuera del almacenamiento
  definitivo.
- **SC-009**: El 100 % de los archivos descargados presenta una huella SHA-256 idéntica a la del archivo
  original antes de cargarlo.
- **SC-010**: Cero coincidencias de metadatos médicos en la totalidad de los logs y las métricas técnicas
  generados durante un ciclo de vida completo de un estudio de prueba.
- **SC-011**: Las diez acciones principales se completan sin desplazamiento horizontal en una pantalla de
  360 píxeles de ancho, y la aplicación se usa desde computadora, tableta y teléfono.
- **SC-012**: Sin sesión iniciada, y también sin conexión, la aplicación no muestra ningún dato médico
  visto anteriormente.
- **SC-013**: En una ventana de 30 días existe al menos un respaldo por día, y una prueba de recuperación
  en un entorno limpio reconstruye la relación entre estudios y archivos sin faltantes ni huérfanos.
- **SC-014**: El costo mensual de infraestructura se mantiene en USD 15 o menos, sin considerar el
  dominio, dentro del límite de 20 GB. *(RNF-45)*
- **SC-015**: El 100 % de los criterios de aceptación vigentes de `PRD2.md` está validado antes de
  considerar entregado el MVP.

## Supuestos

- **Ubicación del PRD**: la invocación mencionó `docs/PRD2.md`, pero el archivo vive en la raíz del
  repositorio (`PRD2.md`). Se tomó ese como fuente; no existe un directorio `docs/`.
- **Alcance de esta especificación**: cubre el MVP completo descrito en `PRD2.md`, no una funcionalidad
  aislada. El repositorio no contiene todavía código de aplicación, así que las siete historias
  describen construcción desde cero, no modificaciones sobre lo existente.
- **Selección tecnológica**: queda deliberadamente fuera de esta especificación. El PRD la delega en la
  especificación técnica, que en este proyecto es `AGENTS.md`, y se concreta en `/speckit-plan`. Las
  menciones a SQLite, columnas normalizadas y modo WAL que aparecen en el PRD son restricciones ya
  tomadas, no decisiones a resolver aquí.
- **Un estudio puede existir sin archivos**: `RF-07` habla de asociar "uno o más" archivos pero la
  creación solo exige título y fecha (`RF-33`, `RF-34`, `RF-35`); se asume que un estudio sin archivos
  es un estado válido y navegable.
- **Alta y restablecimiento de credenciales**: son procedimientos administrativos fuera de la
  aplicación, ejecutados por el administrador técnico, que es uno de los cinco integrantes. La
  aplicación no ofrece registro, cambio ni recuperación de contraseña.
- **Instalación única**: una sola instancia de la aplicación, un solo almacenamiento de archivos y una
  sola base, compartidos por hasta 5 cuentas con concurrencia baja. No hay multi-inquilino más allá del
  aislamiento por propietario.
- **Los usuarios usan navegadores compatibles con PWA** y disponen de conexión para toda operación con
  datos médicos: sin conexión el sistema solo ofrece la pantalla controlada, nunca datos guardados.
- **Respaldos, custodia de la clave y TLS son responsabilidad de la infraestructura**, provista por el
  administrador técnico. La aplicación debe ser compatible con ellos y fallar de forma segura si faltan,
  pero no los implementa.
- **Verificación por inspección**: RNF-10, RNF-37 a RNF-42, RNF-44 a RNF-50 se validan por inspección
  documental, de configuración o de dependencias. Su ausencia de prueba automatizada es deliberada según
  el PRD y no un hueco de cobertura.

## Fuera de Alcance

`PRD2.md` es la fuente única de las exclusiones; se reproducen aquí las de mayor riesgo de reaparecer
durante la implementación. Ninguna de ellas se implementa sin modificar antes el PRD:

- OCR, extracción automática de texto y búsqueda dentro del contenido de los archivos.
- Exportación, importación y restauración iniciadas desde la interfaz.
- Versionado de documentos e historial de cambios o eliminaciones.
- Diagnósticos, recomendaciones, interpretación automática de resultados y resúmenes generados con
  inteligencia artificial.
- Integraciones con clínicas, laboratorios, obras sociales u hospitales; historia clínica oficial;
  turnos, tratamientos y recordatorios.
- Más de 5 cuentas; compartir, delegar o transferir estudios; vistas consolidadas del grupo familiar;
  roles y permisos configurables.
- Autogestión del alta de cuentas, cambio y recuperación de contraseña desde la aplicación.
- Aplicaciones nativas Android o iOS, enlaces públicos para compartir, envío por correo, SMS o WhatsApp,
  y notificaciones automáticas.
- Servicios administrados de búsqueda, APIs de OCR y APIs de inteligencia artificial.
- Edición del contenido interno de los archivos.

## Verificación de la Constitución

- **I. Seguridad y privacidad por construcción**: FR-001, FR-012 a FR-014, FR-035, FR-036, FR-045,
  FR-057, FR-061 y FR-062 fijan aislamiento total, autenticación por omisión, cifrado, falla segura ante
  configuración faltante y ausencia de datos médicos en logs, métricas y caché.
- **II. Trazabilidad al PRD**: los 73 requisitos funcionales y los criterios de éxito citan sus RF/RNF/AC
  de origen. Esta especificación no incorpora ninguna capacidad ausente del PRD.
- **III. Invariantes centralizadas**: la especificación describe comportamiento observable y no prescribe
  dónde vive cada regla; el plan deberá resolver aislamiento, normalización, búsqueda, pipeline de
  archivos y tiempo en un único punto autoritativo.
- **IV. Testing guiado por criterios de aceptación**: cada escenario de aceptación arrastra el
  identificador AC que verifica, de modo que los tests puedan nombrarlo en su `DisplayName`. FR-073 fija
  el uso exclusivo de datos ficticios.
- **V. Simplicidad antes que abstracción**: no se introduce ninguna capacidad que exija capas,
  integraciones o servicios adicionales; FR-071 y FR-072 acotan explícitamente las dependencias externas.
