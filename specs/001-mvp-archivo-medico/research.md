# Fase 0 — Investigación y Decisiones Técnicas

**Feature**: MVP Mi Archivo Médico · **Fecha**: 2026-08-19 · **Plan**: [plan.md](./plan.md)

El stack está fijado por `AGENTS.md` y por las restricciones técnicas de `PRD2.md`, así que no hubo
que investigar la selección tecnológica. Lo que sí quedaba abierto era **cómo** sostener ciertas
garantías con ese stack. Cada sección registra una decisión, su motivo y lo que se descartó.

## 1. Cómo se garantiza el aislamiento por propietario

**Decisión**: `ArchivoMedicoDbContext.OnModelCreating` recorre el modelo por reflexión y aplica
`HasQueryFilter(e => e.OwnerId == _usuarioActual.Id)` a toda entidad que implemente
`IPropiedadDeUsuario`. `IUsuarioActual.Id` vale `null` sin sesión, y como ninguna fila iguala a `null`,
una consulta sin autenticar devuelve vacío en lugar de devolver todo. Un test recorre el modelo y falla
si alguna entidad médica quedó sin filtro.

**Motivo**: el Principio I exige que la protección esté en el mecanismo. Con el filtro global, una
entidad médica nueva queda aislada por el solo hecho de implementar la interfaz, y olvidarse no es
posible sin romper un test.

**Descartado**: repetir `Where(e => e.OwnerId == ...)` en cada consulta. Es la forma habitual y es
exactamente la que falla el día que alguien escribe una consulta nueva con apuro.

**Consecuencia operativa**: `Find`/`FindAsync` **no** aplican filtros globales, así que quedan
prohibidos para datos médicos. Se cargan siempre por `FirstOrDefaultAsync` sobre la consulta filtrada.

## 2. Cómo se responde ante un recurso ajeno

**Decisión**: 404 uniforme, sin cuerpo que lo distinga, para recurso ajeno, inexistente y mal formado.

**Motivo**: RNF-53 admite 403 o 404; un 403 confirma que el recurso existe y filtra información. Con el
filtro global, un identificador ajeno ya devuelve `null`, así que el 404 sale del camino natural del
código en lugar de requerir un chequeo explícito que alguien pueda olvidar.

**Descartado**: 403 para lo ajeno y 404 para lo inexistente, por la enumeración que habilita.

## 3. Cómo viajan la búsqueda y los filtros

**Decisión**: el criterio de búsqueda —término, rango de fechas, institución, página— se envía en el
cuerpo de la solicitud y se guarda en el estado de sesión del servidor (`EstadoDeBusqueda`). El listado
lo lee de ahí, de modo que paginar o volver desde el detalle de un estudio conserva el criterio sin que
viaje nada en la dirección.

**Motivo**: RNF-63 prohíbe que el término aparezca en la dirección, porque la infraestructura registra
las direcciones solicitadas y el término es un dato médico. RF-40 exige conservar el criterio. Las dos
cosas juntas obligan a un lugar del lado del servidor.

**Descartado**: campos ocultos en formularios, que obligarían a convertir cada enlace al detalle en un
envío por POST y aun así perderían el criterio al usar el botón de volver del navegador. También se
descartó un identificador opaco de búsqueda en la dirección, que agrega estado con expiración propia
para resolver algo que el estado de sesión ya resuelve.

**Costo aceptado**: no existe una dirección que reproduzca un listado filtrado. Ningún requerimiento lo
pide y quedó asentado como exclusión en `PRD2.md`.

## 4. Cómo se entrega un archivo

**Decisión**: la ruta de entrega lleva un token generado con la protección de datos de ASP.NET Core,
con vida máxima de 5 minutos, **y** exige sesión válida del propietario. Las dos condiciones son
acumulativas: token vencido rechaza, ausencia de sesión rechaza aunque el token siga vigente. La
respuesta lleva las cabeceras que impiden la interpretación del contenido y la ejecución de contenido
activo, y la vista lo incrusta en un marco restringido.

**Motivo**: reconcilia RNF-06, RNF-07, AC-08 y AC-84, que leídos por separado parecían pedir cosas
incompatibles. El componente de protección de datos ya viene en la plataforma y produce tokens
firmados con vencimiento sin guardar nada.

**Descartado**: tokens persistidos en la base, que agregan una tabla, escrituras en cada visualización
y una tarea de limpieza para lograr lo mismo. También se descartó un token de un solo uso, porque la
visualización incrustada puede pedir el recurso más de una vez y rompería con una recarga.

## 5. Cómo se cifran los archivos

**Decisión**: AES-256 en modo CBC, cifrando en flujo, con el vector de inicialización generado al azar
por archivo y escrito como los primeros 16 bytes del archivo físico: `[IV 16 bytes][contenido cifrado]`.
El nombre físico es un GUID sin ninguna porción del nombre original. La clave se resuelve de
configuración externa y la aplicación **falla al arrancar** si no está.

**Motivo**: cifrar en flujo evita materializar en memoria un archivo de hasta 50 MB. El
`SHA-256` que exige RNF-19 se calcula sobre el contenido en claro antes de cifrar y se almacena, lo que
da detección de alteración sin necesidad de un modo autenticado.

**Descartado**: AES-GCM, que aporta integridad propia pero exige dividir el contenido en bloques con su
etiqueta para poder cifrarlo en flujo. Es más maquinaria para una garantía que el hash ya cubre, y el
Principio V pide la solución más simple que satisfaga el requisito. Queda anotado que si en el futuro
el hash dejara de almacenarse, esta decisión debe revisarse.

## 6. Cómo se verifica cada criterio de aceptación

**Decisión**: se clasifican los 99 criterios vigentes en seis métodos. Los que no son alcanzables por
la suite de integración no desaparecen: se convierten en una comprobación con procedimiento escrito,
ejecutada antes de la entrega.

**Motivo**: sin esta clasificación, `/speckit-tasks` generaría tareas de "escribir el test de AC-38"
que nadie puede escribir, y los criterios no automatizables se perderían de vista.

### Test de integración sobre HTTP real — 75 criterios

AC-01, AC-02, AC-03, AC-04, AC-05, AC-76, AC-84, AC-98, AC-09, AC-10, AC-11, AC-12, AC-13, AC-14,
AC-70, AC-80, AC-82, AC-88, AC-89, AC-90, AC-91, AC-99, AC-100, AC-15, AC-16, AC-17, AC-18, AC-19,
AC-77, AC-102, AC-20, AC-21, AC-22, AC-23, AC-24, AC-25, AC-27, AC-44, AC-65, AC-66, AC-74, AC-75,
AC-28, AC-29, AC-30, AC-31, AC-34, AC-35, AC-36, AC-37, AC-45, AC-46, AC-71, AC-72, AC-73, AC-92,
AC-93, AC-94, AC-95, AC-101, AC-43, AC-85, AC-96, AC-47, AC-48, AC-49, AC-50, AC-62, AC-63, AC-54,
AC-55, AC-64, AC-97, AC-57, AC-58.

Notas de método para los menos evidentes:

- **AC-50 y AC-63** se resuelven enumerando las rutas registradas desde la propia aplicación, no con
  una lista escrita a mano que envejece.
- **AC-43 y AC-85** capturan el canal de registro y el de métricas del host de prueba y buscan en todo
  lo emitido las cadenas únicas del estudio ficticio.
- **AC-57** lee el archivo directamente de la carpeta de almacenamiento descartable y comprueba que sus
  bytes no son los del original.
- **AC-55, AC-64 y AC-97** configuran un cupo diminuto en lugar de cargar 20 GB.
- **AC-96** comprueba que la dirección de cada solicitud emitida durante una búsqueda no contiene el
  término.

### Test de integración con tiempo simulado — 6 criterios

AC-06, AC-07, AC-08, AC-69, AC-86, AC-87. Todos dependen de ventanas de 30 minutos, 24 horas, 15
minutos o 5 minutos. Se ejercitan adelantando el `TimeProvider` simulado, que también se inyecta en el
manejador de la cookie de autenticación para que la expiración sea observable sin esperar.

### Test de arranque del host — 1 criterio

AC-83. No usa la fábrica de aplicación habitual, porque el fallo ocurre antes de que la aplicación esté
disponible: construye el host con la configuración sin clave de cifrado y comprueba que la construcción
termina con error.

### Medición de rendimiento con instrumental propio — 3 criterios

AC-51, AC-52, AC-53. Requieren una colección sembrada de 2.000 estudios y, en el caso de AC-53, limitar
el ancho de banda a 10 Mbps. No forman parte de la suite habitual porque la volverían lenta; se ejecutan
a pedido, con la colección generada por un sembrador reproducible.

### Comprobación manual en navegador — 8 criterios

AC-26, AC-38, AC-39, AC-40, AC-41, AC-42, AC-78, AC-79. Dependen de un navegador real: instalación de
la aplicación, renderizado a 360 píxeles, comportamiento sin conexión, ejecución o no de contenido
activo, y solicitudes efectivamente emitidas.

De dos de ellos se automatiza la mitad que sí es observable desde el servidor: de **AC-26**, que la
respuesta lleve las cabeceras que bloquean el contenido activo y que la vista use un marco restringido;
de **AC-78**, que ni el listado ni el detalle incluyan un elemento que descargue contenido por su
cuenta. Lo que queda manual es que el script no corra y que no se emita la solicitud.

**AC-79** depende de una definición de "pantalla o paso" que todavía no existe (CHK015 de
`checklists/ux.md`). Hasta que se defina, se comprueba a mano contando las pantallas que atraviesa el
usuario.

### Inspección de infraestructura — 6 criterios

AC-56, AC-59, AC-60, AC-61, AC-67, AC-68. Dependen de un entorno desplegado, de 30 días de calendario o
de credenciales que no existen en desarrollo. Se comprueban sobre la instalación real, con evidencia
registrada por el administrador técnico, y su periodicidad la fija RNF-37.

## 7. Cómo se cuentan los intentos fallidos de inicio de sesión

**Decisión**: una entidad propia, `IntentoDeInicioDeSesion`, acumula fallos contra el **nombre de
usuario ingresado normalizado**, exista o no una cuenta con ese nombre, con la marca de tiempo del
último fallo. Cuando el nombre no corresponde a ninguna cuenta, se verifica igualmente una contraseña
contra un hash señuelo, para que la demora sea comparable.

**Motivo**: RNF-65 exige que el comportamiento observable sea indistinguible. El bloqueo que trae
Identity cuenta contra la entidad de usuario y por lo tanto no puede contar nombres inexistentes, que
es justo el canal por el que se enumeran cuentas.

**Descartado**: usar el bloqueo incorporado de Identity. Resuelve RNF-60 pero deja abierto RNF-65.

**Consecuencia**: la tabla no está acotada a 5 filas, así que necesita depuración de las entradas
vencidas. Esta entidad **no** implementa `IPropiedadDeUsuario`: no es un dato médico y debe consultarse
sin sesión.

## 8. Cómo se ordena por fecha en SQLite

**Decisión**: toda columna `DateTimeOffset` que participe de un ordenamiento se persiste con un
conversor a ticks UTC declarado en el contexto.

**Motivo**: SQLite no compara `DateTimeOffset` en un `ORDER BY`, y AC-28 exige ordenar del más reciente
al más antiguo. La fecha del estudio, en cambio, es una fecha de calendario sin hora y se persiste como
tal.

## 9. Cómo se busca sin intercalación insensible a acentos

**Decisión**: cada campo buscable se persiste además en una columna normalizada e indexada, calculada
con `NormalizadorDeTexto.Normalizar` durante la interceptación de `SaveChanges`. El término ingresado se
normaliza con la misma función antes de consultar, y la búsqueda es por subcadena sobre esas columnas.

**Motivo**: SQLite no ofrece intercalación insensible a acentos, y RNF-55 lo declara explícitamente. Al
calcular la columna normalizada en la interceptación, ningún camino de escritura puede saltearla.

**Descartado**: FTS5 o cualquier índice de texto completo, prohibidos por las restricciones técnicas del
PRD para el volumen del MVP.

## 10. Qué entra en la caché del navegador

**Decisión**: el service worker guarda únicamente lo que declara su lista `ESTATICOS` —archivos
estáticos y la pantalla sin conexión— y nunca escribe una respuesta de red. Las navegaciones y la
entrega de archivos son red o pantalla sin conexión, sin nada intermedio. La pantalla sin conexión no
usa el layout, para que la copia guardada sea idéntica para cualquiera.

**Motivo**: RNF-51 exige que sin sesión no se vea información médica guardada antes. Una caché con una
sola regla se puede leer entera y auditar; una con estrategias por ruta, no.

**Verificación**: un test pide cada entrada de `ESTATICOS` sin sesión; si alguna exigiera
autenticación, falla.
