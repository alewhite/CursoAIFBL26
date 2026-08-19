# AGENTS.md — contexto del proyecto

Este archivo describe **el proyecto**: propósito, stack, arquitectura, convenciones, restricciones e
invariantes. El proceso de trabajo (fases, gates, cuándo testear, cuándo commitear y cuándo avanzar)
pertenece a DAW y no debe duplicarse acá.

`docs/daw/prd/PRD2.md` es la **fuente única de verdad del alcance, requerimientos y criterios de aceptación**.
Ante una contradicción sobre alcance o comportamiento del producto, prevalece `docs/daw/prd/PRD2.md`.

---

## Git conventions

- Base branch: `v5-daw`
- All DAW feature, fix, and discovery branches must be created from `v5-daw`.
- Do not work directly on `v5-daw` or `main`.

---

## Idioma

**Idioma de trabajo: español.**

- Responder al usuario en español salvo que solicite explícitamente otro idioma.
- Escribir en español los artefactos del proyecto: PRD, specs, ADR, reportes, documentación y mensajes
  de commit.
- Mantener los nombres técnicos propios de tecnologías, protocolos, clases y APIs cuando traducirlos
  genere ambigüedad.
- No inventar sinónimos para conceptos del dominio: usar la terminología definida en este archivo y en
  `docs/daw/prd/PRD2.md`.

---

## Qué es este proyecto

**Mi Archivo Médico** es una aplicación web progresiva (PWA) de uso familiar para hasta 5 cuentas,
destinada a cargar, organizar, consultar y encontrar estudios médicos de forma segura.

Cada cuenta accede exclusivamente a sus propios estudios, archivos y metadatos. Las cuentas comparten
la instalación, nunca los datos. La búsqueda se realiza únicamente sobre metadatos ingresados
manualmente: el sistema no analiza ni extrae el contenido interno de los archivos.

**PRD de referencia:** `docs/daw/prd/PRD2.md`

### Objetivos principales

- Centralizar los estudios médicos de hasta 5 integrantes de una familia.
- Mantener aislamiento total entre cuentas.
- Permitir encontrar un estudio conocido en menos de 10 segundos.
- Proteger archivos y metadatos contra accesos no autorizados.
- Funcionar desde computadoras, tabletas y teléfonos sin aplicaciones móviles nativas.
- Mantener el MVP pequeño, construible y con bajo costo operativo.
- Conservar los archivos originales sin modificaciones.
- Reducir el riesgo de pérdida de información mediante respaldos automáticos de infraestructura.

---

## Stack

Este es el stack de referencia del proyecto. No agregar ni reemplazar componentes sin que exista una
necesidad concreta compatible con `docs/daw/prd/PRD2.md`.

| Campo | Valor |
|---|---|
| Lenguaje | C# |
| Runtime | .NET 8 (LTS) |
| Framework | ASP.NET Core MVC con Razor Views |
| Persistencia | Entity Framework Core |
| Base de datos | SQLite, archivo único en disco del servidor, modo WAL |
| Autenticación | ASP.NET Core Identity |
| Almacenamiento de archivos | Carpeta privada en disco del servidor, fuera de toda carpeta pública |
| PWA | `manifest.webmanifest` + service worker propio, sin framework de caché |
| Test runner | `dotnet test` con tests de integración sobre la aplicación |
| Gestor de paquetes | NuGet |
| Linter / formatter | No se fija uno adicional en el PRD; respetar la configuración existente del repositorio |

### Restricciones técnicas del stack

- La base SQLite debe vivir fuera del repositorio y fuera de cualquier carpeta pública del servidor,
  junto con sus archivos auxiliares `-wal` y `-shm`.
- SQLite debe operar en modo WAL.
- No se utilizarán microservicios.
- La búsqueda debe resolverse con las capacidades de la base de datos y únicamente sobre metadatos.
- La normalización de texto se resuelve en la aplicación y se persiste en columnas normalizadas.
- El proveedor de almacenamiento de archivos debe poder reemplazarse sin modificar las reglas principales
  del dominio.
- Los secretos, claves y cadenas de conexión deben resolverse desde configuración externa y nunca
  versionarse.
- El costo mensual objetivo de infraestructura es de hasta USD 15, sin considerar el dominio.
- Cualquier servicio externo pago debe poder deshabilitarse por configuración sin impedir las funciones
  principales del MVP.

---

## Convenciones de arquitectura

### Estructura general

- Mantener una única aplicación web y un proyecto de tests; no introducir una arquitectura distribuida.
- Mantener separadas las responsabilidades de presentación, persistencia, seguridad, dominio y
  almacenamiento de archivos.
- No agregar capas de abstracción, frameworks ni patrones arquitectónicos sin una necesidad concreta del
  producto.
- La interfaz nunca debe acceder directamente a archivos físicos mediante rutas públicas.
- Todo acceso a archivos médicos debe pasar por autenticación y autorización.

### Propiedad y aislamiento de datos

El aislamiento por propietario es una invariante crítica del sistema (RNF-53).

- Cada estudio y cada archivo pertenece a una única cuenta.
- Una cuenta nunca puede ver, consultar, descargar, compartir, delegar ni transferir datos de otra cuenta.
- Un acceso a un recurso de otro propietario debe responder HTTP 403 o 404 y no revelar metadatos.
- Las listas, búsquedas, filtros, contadores y opciones de filtro deben considerar únicamente datos del
  usuario autenticado.
- Cualquier mecanismo central existente para aplicar el aislamiento debe preferirse frente a repetir
  controles manuales en cada consulta.

### Autenticación y sesión

- Todas las pantallas privadas requieren autenticación.
- No existe registro abierto de cuentas.
- El alta y el restablecimiento de contraseña son procedimientos administrativos fuera de la interfaz.
- Se admiten como máximo 5 cuentas activas.
- La contraseña administrativa debe tener al menos 12 caracteres.
- La cookie de autenticación debe ser `Secure`, `HttpOnly` y `SameSite=Strict`.
- La cookie debe ser de sesión del navegador, sin fecha de expiración propia.
- Cada cuenta puede tener como máximo una sesión activa.
- Un nuevo inicio de sesión invalida la sesión previa de la misma cuenta en cualquier otro dispositivo.
- La sesión expira después de 30 minutos de inactividad y tiene una duración absoluta máxima de 24 horas.
- Después de 5 intentos fallidos dentro de 15 minutos, el acceso debe bloquearse durante 15 minutos desde
  el quinto fallo.
- El comportamiento ante usuario inexistente, contraseña incorrecta y cuenta bloqueada debe ser
  indistinguible tanto en mensaje como en demora observable.

### Manejo de archivos

- Formatos admitidos: PDF, JPG, JPEG y PNG.
- Tamaño máximo por archivo: 50 MB.
- Máximo de 20 archivos por estudio.
- Validar extensión, MIME, firma binaria y estructura real del archivo.
- Rechazar archivos vacíos, corruptos, ejecutables, scripts y formatos no permitidos aunque hayan sido
  renombrados.
- Sanitizar el nombre original antes de persistirlo o mostrarlo.
- El nombre físico de almacenamiento debe ser un GUID y no contener ninguna parte del nombre original.
- Calcular y almacenar SHA-256 para cada archivo aceptado.
- Conservar el archivo original sin modificarlo durante carga, visualización y descarga.
- Los archivos rechazados nunca deben permanecer en el almacenamiento definitivo.
- Si una carga múltiple contiene archivos válidos e inválidos, aceptar los válidos e informar cada rechazo
  por separado; el estudio puede crearse si título y fecha son válidos.
- La visualización no debe ejecutar macros, scripts, JavaScript embebido ni contenido activo.
- No descargar archivos médicos al abrir listados o detalles; solo transferirlos cuando el usuario solicite
  visualizarlos o descargarlos.
- Mientras una carga está en curso, indicar progreso e impedir reenvíos duplicados del formulario.

### Cifrado y almacenamiento

- El 100 % de los archivos médicos debe almacenarse cifrado en reposo mediante AES-256.
- La clave de cifrado debe residir fuera del código fuente, fuera de la configuración versionada, fuera del
  entorno principal y fuera de los respaldos.
- Si la clave no está disponible al iniciar, la aplicación debe fallar en lugar de almacenar archivos sin
  cifrar.
- Los archivos cifrados deben almacenarse fuera de cualquier carpeta pública del servidor.
- La rotación o reemplazo de la clave de cifrado desde la aplicación está fuera de alcance.
- La base SQLite no está obligada por el PRD a estar cifrada en reposo; los metadatos se protegen mediante
  aislamiento, permisos del sistema operativo y ubicación privada del archivo de base de datos.

### Estudios y validación de entrada

- El título y la fecha son obligatorios.
- La fecha del estudio es una fecha de calendario sin hora y no puede ser posterior al día en curso.
- Límites máximos:
  - título: 200 caracteres;
  - profesional: 200 caracteres;
  - institución: 200 caracteres;
  - descripción: 2.000 caracteres;
  - cada etiqueta: 50 caracteres.
- Los errores de validación deben mostrarse junto al campo o archivo que los produjo.
- Si un formulario se rechaza por validación, conservar los metadatos ingresados y advertir que los archivos
  deben adjuntarse nuevamente.
- Ningún archivo queda retenido para un segundo envío.

### Búsqueda, filtros y paginación

- Buscar únicamente sobre título, descripción, profesional, institución y etiquetas.
- La búsqueda y los filtros de texto libre deben ser insensibles a mayúsculas, minúsculas y acentos, e
  ignorar espacios al inicio y al final.
- Persistir versiones normalizadas de los campos buscables y normalizar el término ingresado con la misma
  lógica antes de consultar.
- No depender de FTS, motores de búsqueda administrados ni servicios externos.
- El filtro por institución debe usar una selección de instituciones presentes en los estudios del propio
  usuario, ordenadas alfabéticamente y con coincidencia exacta.
- Paginar cuando existan más de 25 estudios.
- Mantener búsqueda y filtros al cambiar de página y al volver al listado desde el detalle.
- El término de búsqueda y los filtros no deben viajar en la URL; deben enviarse en el cuerpo de la
  solicitud.
- No debe existir una URL que reproduzca una búsqueda o un listado filtrado.
- Distinguir entre:
  - cuenta sin estudios: informar la situación y ofrecer crear el primero;
  - búsqueda/filtros sin resultados: informar cero resultados y ofrecer limpiar filtros.

### PWA y privacidad local

- La PWA debe ser instalable en navegadores compatibles.
- Sin conexión debe mostrarse una pantalla controlada sin estudios, metadatos ni archivos médicos.
- La PWA no debe mostrar información médica previamente almacenada cuando el usuario no está autenticado.
- El service worker no debe guardar páginas privadas, respuestas de navegación, metadatos ni archivos
  médicos.
- Informar al usuario cuando una carga no pueda completarse por pérdida de conexión.

### Errores, logs, métricas y telemetría

- Nunca registrar títulos, descripciones, profesionales, instituciones, etiquetas, resultados médicos ni
  nombres originales de archivos.
- No incluir datos médicos en logs, métricas, etiquetas de métricas, mensajes de error, excepciones ni
  telemetría.
- Usar únicamente identificadores técnicos internos cuando sea necesario diagnosticar un problema.
- Sanitizar errores antes de exponerlos o registrarlos.
- No utilizar herramientas de seguimiento de comportamiento.

### Dependencias

- No agregar APIs de inteligencia artificial.
- No agregar servicios de OCR.
- No agregar motores de búsqueda administrados.
- No introducir servicios externos pagos que sean obligatorios para las funciones principales.
- Toda dependencia nueva debe justificar un requerimiento concreto y respetar el límite de costo del MVP.
- Evitar dependencias que agreguen complejidad operacional desproporcionada para una aplicación familiar de
  hasta 5 cuentas.

---

## Convenciones de código

- Mantener el código simple y explícito; evitar abstracciones anticipadas para necesidades hipotéticas.
- Reutilizar las invariantes centrales del proyecto en lugar de duplicar reglas de seguridad o dominio en
  múltiples controladores o consultas.
- Evitar lógica de negocio crítica en las vistas.
- Las consultas sobre estudios, archivos, instituciones, contadores y filtros nunca deben cruzar propietarios.
- La normalización de texto debe tener una única implementación reutilizable.
- Las reglas de validación de archivos deben tener una única implementación coherente para todos los puntos
  de carga.
- Las operaciones temporales relacionadas con expiraciones, bloqueos y sesión deben poder probarse de manera
  determinista; reutilizar el mecanismo de tiempo inyectable existente si el proyecto ya lo proporciona.
- Los mensajes de error de autenticación no deben revelar si una cuenta existe.
- No incluir datos médicos en `ToString()`, excepciones, logs de depuración ni mensajes de diagnóstico.
- No almacenar secretos en código, fixtures, archivos de configuración versionados ni datos de prueba.
- Los datos de desarrollo, tests y fixtures deben ser ficticios o anonimizados.
- Los archivos de prueba deben ser sintéticos o generados; no copiar archivos médicos reales.
- Conservar el estilo y las convenciones existentes del repositorio antes de introducir una nueva
  convención de nombres o estructura.

---

## Qué NO hacer en este proyecto

- No agregar funcionalidad fuera del alcance definido en `docs/daw/prd/PRD2.md`.
- No implementar OCR ni extracción automática de texto.
- No buscar dentro del contenido de PDFs o imágenes.
- No agregar diagnósticos, recomendaciones, interpretaciones ni resúmenes médicos generados por IA.
- No integrar APIs de IA.
- No utilizar contenido médico para entrenar modelos.
- No agregar exportación, importación o restauración desde la interfaz.
- No agregar historial de cambios, versionado de documentos ni historial de eliminaciones.
- No agregar roles o permisos configurables.
- No crear vistas consolidadas entre cuentas.
- No compartir, delegar ni transferir estudios entre cuentas.
- No agregar registro abierto, cambio de contraseña ni recuperación de contraseña desde la interfaz.
- No superar las 5 cuentas activas.
- No agregar aplicaciones móviles nativas.
- No crear enlaces públicos para compartir documentos.
- No enviar documentos por correo, SMS o WhatsApp.
- No agregar notificaciones automáticas.
- No integrar clínicas, laboratorios, hospitales, obras sociales ni sistemas externos.
- No agregar gestión de turnos, tratamientos o recordatorios de medicación.
- No agregar motores de búsqueda administrados ni FTS si no existe un cambio explícito de alcance.
- No exponer archivos médicos por rutas públicas.
- No guardar secretos, claves, cadenas de conexión, archivos `.db`, `-wal` o `-shm` en el repositorio.
- No copiar en caliente el archivo SQLite para hacer respaldos; usar un mecanismo consistente como
  `VACUUM INTO` o la API de backup en línea.
- No guardar datos médicos en la caché del service worker.
- No poner términos de búsqueda ni filtros en la URL.
- No implementar rotación de la clave de cifrado desde la aplicación.
- No asumir requisitos de accesibilidad que el MVP no define: el alcance solo exige uso desde 360 px y
  ausencia de desplazamiento horizontal en las acciones principales.
- No introducir microservicios.
- No aumentar complejidad arquitectónica o costos sin un requerimiento concreto que lo justifique.

---

## Glosario del dominio

- **Cuenta:** identidad individual de uno de los hasta 5 integrantes que usan la instalación.
- **Usuario principal:** integrante del grupo familiar que carga, organiza, busca, visualiza y descarga sus
  propios estudios.
- **Administrador técnico:** integrante responsable de despliegue, altas administrativas, restablecimiento
  de contraseñas, respaldos y operación técnica; no obtiene acceso a los estudios de otras cuentas por ese
  rol.
- **Propietario:** cuenta a la que pertenece un estudio o archivo. Determina el aislamiento de datos.
- **Estudio:** registro médico organizado por fecha y metadatos, al que pueden asociarse uno o más archivos.
- **Archivo médico:** PDF, JPG, JPEG o PNG asociado a un estudio y almacenado cifrado.
- **Metadatos:** título, fecha, profesional, institución, descripción y etiquetas ingresados manualmente.
- **Etiqueta:** texto libre asociado a un estudio, con máximo de 50 caracteres por valor.
- **Institución:** texto libre asociado a un estudio y también valor exacto utilizado en el filtro por
  institución.
- **Búsqueda textual:** búsqueda sobre metadatos normalizados; nunca sobre el contenido interno de los
  archivos.
- **Filtro:** criterio aplicado al listado, por ejemplo rango de fechas o institución.
- **Almacenamiento definitivo:** ubicación privada donde permanecen únicamente los archivos aceptados y
  cifrados.
- **Cupo compartido:** límite total de 20 GB para los archivos de todas las cuentas; no existen cuotas
  individuales.
- **PWA:** aplicación web progresiva instalable desde un navegador compatible.
- **Pantalla sin conexión:** pantalla controlada de la PWA que no contiene datos médicos.
- **Sesión activa:** sesión autenticada vigente de una cuenta; solo puede existir una por cuenta.
- **Respaldo de infraestructura:** copia automática administrada fuera de la interfaz que incluye base de
  datos y archivos de un mismo punto temporal.
- **Datos médicos:** documentos y cualquier metadato identificable asociado a ellos, incluidos títulos,
  descripciones, profesionales, instituciones, etiquetas y nombres originales de archivo.

---

## Invariantes no negociables

Estas reglas describen propiedades permanentes del producto y deben mantenerse en cualquier cambio:

1. **Aislamiento total:** una cuenta nunca accede a datos de otra cuenta.
2. **Privacidad por defecto:** ninguna página privada ni archivo médico está disponible sin autenticación y
   autorización.
3. **Archivos cifrados:** ningún archivo aceptado llega al almacenamiento definitivo en texto claro.
4. **Sin datos médicos en observabilidad:** logs, métricas, errores y telemetría no contienen datos médicos.
5. **Archivos originales inmutables:** el SHA-256 antes de la carga y después de la descarga debe coincidir.
6. **Búsqueda solo por metadatos:** el contenido interno de archivos nunca se analiza.
7. **URLs limpias:** búsquedas y filtros no aparecen en la dirección solicitada.
8. **Cupo estricto:** el almacenamiento definitivo nunca supera los 20 GB.
9. **Sin datos médicos offline:** la PWA no cachea ni muestra información médica privada sin sesión.
10. **Alcance controlado:** las funciones declaradas fuera de alcance en `docs/daw/prd/PRD2.md` no forman parte del MVP.

---

> ℹ️ **Lo que NO pertenece en este archivo porque DAW lo proporciona:** el orden en que ocurre el trabajo,
> cuándo se escribe la spec, cuándo se ejecutan tests, cuándo se hace commit y qué hace falta para avanzar
> entre fases. Todo eso vive en `.daw/` y aplica por separado.

<!-- BEGIN DAW (managed by DAW — do not edit by hand) -->
# DAW — Dilux Agentic Workflow

This repo uses **DAW**: an agent-driven development pipeline with the phases
`CLASSIFY → DEFINE → PLAN → CODE → VERIFY → RELEASE`.

Before answering, read `.daw/orchestrator.md` and run its Boot Sequence. It is a strict state
machine: it decides what you are allowed to do based on the phase recorded in `.daw-state.json`.

The project's own context — stack, architecture, domain — is elsewhere in this file. It lives here,
in `AGENTS.md`, and not in any one tool's file, on purpose: it is tool-agnostic and comes along
unchanged when the pipeline is ported to another agent.
<!-- END DAW -->
