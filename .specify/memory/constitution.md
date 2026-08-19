<!--
SYNC IMPACT REPORT
==================
Cambio de version: (ninguna) -> 1.0.0
Motivo: ratificacion inicial de la constitucion del proyecto. Se completa la plantilla
        `.specify/templates/constitution-template.md` con los cinco principios definidos por el
        equipo. Al no existir version previa, no hay redefiniciones incompatibles: MAJOR 1.

Principios definidos (todos nuevos):
  - [PRINCIPLE_1_NAME] -> I. Seguridad y Privacidad por Construccion (NO NEGOCIABLE)
  - [PRINCIPLE_2_NAME] -> II. Trazabilidad al PRD y Control Estricto del Alcance (NO NEGOCIABLE)
  - [PRINCIPLE_3_NAME] -> III. Invariantes Centralizadas y una Unica Implementacion de Cada Regla
  - [PRINCIPLE_4_NAME] -> IV. Testing Guiado por Criterios de Aceptacion
  - [PRINCIPLE_5_NAME] -> V. Simplicidad antes que Abstraccion

Secciones agregadas:
  - [SECTION_2_NAME]  -> Restricciones Tecnologicas y Operativas
  - [SECTION_3_NAME]  -> Flujo de Trabajo y Puertas de Calidad
  - Gobernanza (orden de autoridad documental, enmiendas, versionado, excepciones)

Secciones eliminadas: ninguna.

Consistencia con artefactos dependientes (leen la constitucion en tiempo de ejecucion, no se
modifican aca):
  - .specify/templates/plan-template.md    -> tiene "Constitution Check": compatible, sin cambios.
  - .specify/templates/spec-template.md    -> compatible; las specs deben citar RF/RNF/AC (Principio II).
  - .specify/templates/tasks-template.md   -> compatible; la definicion de terminado vive aca.
  - .specify/templates/checklist-template.md -> compatible, sin cambios.
  - AGENTS.md / CLAUDE.md                  -> alineados; son fuente de detalle de implementacion.

TODOs diferidos: ninguno.
-->

# Constitución de Mi Archivo Médico

Esta constitución define los principios de ingeniería obligatorios para todo el proyecto
**Mi Archivo Médico**: una PWA de uso familiar (hasta 5 cuentas) para cargar, organizar y encontrar
estudios médicos, donde cada cuenta accede exclusivamente a sus propios datos.

El alcance funcional y no funcional vive en `PRD2.md`. Esta constitución no lo reemplaza ni lo
duplica: fija **cómo** se construye y qué es inaceptable, y delega el detalle en `PRD2.md`,
`AGENTS.md` y `CLAUDE.md`.

## Principios Fundamentales

### I. Seguridad y Privacidad por Construcción (NO NEGOCIABLE)

La seguridad y la privacidad de los datos médicos son la prioridad principal del sistema. DEBEN estar
garantizadas por la arquitectura y NUNCA depender de que cada persona recuerde aplicar un control a
mano. Las protecciones críticas DEBEN implementarse de forma centralizada, automática y con
comportamiento seguro por omisión: autenticación obligatoria, autorización, aislamiento total entre
propietarios, cifrado de archivos en reposo, protección de sesiones, validación de archivos, custodia
externa de secretos y ausencia de datos médicos en logs, métricas y cachés del navegador.

Reglas de cumplimiento obligatorio:

- **Aislamiento total entre cuentas.** Cada cuenta accede exclusivamente a sus propios estudios,
  archivos y metadatos. Ninguna funcionalidad puede habilitar acceso cruzado entre cuentas, directa
  ni indirectamente. Un recurso ajeno responde 403 o 404 aunque se conozca su identificador.
- **Aislamiento automático para entidades nuevas.** Toda entidad que contenga datos médicos DEBE
  quedar aislada por el mecanismo centralizado existente. NO se implementan controles de `OwnerId`
  a mano en cada consulta cuando existe un mecanismo central que lo garantiza.
- **Nunca `Find`/`FindAsync`** de Entity Framework Core para recuperar datos médicos, porque omiten
  los filtros globales de propietario.
- **Autenticación por omisión.** Toda pantalla nueva nace protegida. El acceso anónimo es una
  excepción explícita y justificada, nunca el resultado de olvidar un atributo.
- **Sin URLs públicas ni permanentes** para archivos médicos, y sin almacenarlos en carpetas
  servidas públicamente. Todo acceso pasa por autenticación y autorización.
- **Secretos fuera del repositorio.** Claves de cifrado, cadenas de conexión y credenciales NUNCA se
  versionan en código ni en archivos de configuración del repositorio.
- **Falla segura ante configuración faltante.** Si falta una configuración necesaria para sostener
  una garantía de seguridad, la aplicación DEBE rechazar el arranque en lugar de degradar la
  protección. Ejemplo: sin clave de cifrado, la aplicación no arranca; nunca guarda archivos en claro.
- **Cero datos médicos en observabilidad.** NUNCA registrar en logs, métricas, excepciones ni
  telemetría títulos, descripciones, profesionales, instituciones, etiquetas, nombres originales de
  archivo ni ningún otro dato médico o identificable. Solo identificadores técnicos internos.
- **La PWA no cachea datos médicos.** El service worker NUNCA guarda páginas de la aplicación,
  metadatos ni archivos médicos.

**Racional**: el sistema custodia información de salud de una familia. Un olvido puntual no puede
traducirse en una filtración; por eso las garantías viven en el mecanismo, no en la disciplina.

### II. Trazabilidad al PRD y Control Estricto del Alcance (NO NEGOCIABLE)

`PRD2.md` es la fuente única de verdad del alcance funcional y no funcional del producto.

- Todo cambio funcional DEBE poder relacionarse explícitamente con uno o más identificadores RF, RNF
  o AC de `PRD2.md`, y citarlos.
- Antes de diseñar o implementar, se DEBE identificar qué requerimiento y qué criterio de aceptación
  justifican la funcionalidad.
- Un cambio que no corresponde a ningún RF, RNF o AC vigente está **fuera de alcance** y NO se
  implementa por cuenta propia: se pregunta primero.
- NO se agregan funcionalidades porque parezcan útiles, sean práctica habitual de la industria o las
  sugiera una herramienta o un agente de inteligencia artificial.
- Lo declarado explícitamente fuera de alcance en `PRD2.md` permanece prohibido hasta que el propio
  PRD se modifique formalmente.
- `PRD.md` es una revisión histórica: NO se cita ni se implementa contra él.
- Ante contradicción entre `PRD2.md`, `AGENTS.md`, `CLAUDE.md`, el código, comentarios u otra
  documentación, prevalece `PRD2.md` en materia de alcance y requerimientos. La documentación
  secundaria se corrige para alinearse al PRD, nunca al revés.
- Las especificaciones creadas con Spec Kit DEBEN referenciar los RF, RNF y AC correspondientes
  cuando describan funcionalidad ya prevista en `PRD2.md`.

**Racional**: el producto es un MVP con límites deliberados. El crecimiento no controlado del alcance
es la vía más rápida a superar el presupuesto y a introducir superficie de riesgo no analizada.

### III. Invariantes Centralizadas y una Única Implementación de Cada Regla

Cada regla transversal crítica DEBE tener una única implementación autoritativa, y NO DEBE duplicarse
entre controladores, vistas, consultas ni otros componentes. Se reutilizan los mecanismos ya
existentes del proyecto:

- **Aislamiento por propietario**: los filtros globales de `ArchivoMedicoDbContext`.
- **Asignación de `OwnerId` y recálculo de columnas normalizadas**: el procesamiento centralizado de
  `SaveChanges` / `SaveChangesAsync`. Ningún camino de escritura lo replica ni lo saltea.
- **Normalización de texto**: exclusivamente `NormalizadorDeTexto.Normalizar`, tanto al persistir
  como al buscar. Las dos puntas usan la misma función o la búsqueda deja de coincidir.
- **Búsqueda de estudios**: `BuscadorDeEstudios` sobre las columnas normalizadas existentes. NO se
  crean implementaciones alternativas de búsqueda que puedan producir resultados inconsistentes.
- **Procesamiento de archivos**: se mantiene el pipeline existente —recepción en tránsito,
  validación, cálculo de SHA-256, cifrado y traslado al almacenamiento definitivo—. Un archivo
  rechazado NUNCA llega al almacenamiento definitivo.
- **Tiempo y expiraciones**: siempre `TimeProvider`. NO usar `DateTime.Now`, `DateTime.UtcNow`,
  `DateTimeOffset.Now` ni equivalentes cuando intervienen reglas temporales.

NO se duplican reglas de dominio o de seguridad dentro de un controlador solo para "hacer explícito"
un comportamiento que ya está garantizado centralmente. Cuando aparece una regla transversal nueva,
se incorpora en el punto central adecuado antes que repetirla en varios lugares.

**Racional**: una invariante repetida en N lugares se rompe en el lugar N+1 que alguien olvidó tocar.

### IV. Testing Guiado por Criterios de Aceptación

El comportamiento observable del sistema DEBE estar protegido por tests de integración que ejerciten
la aplicación real.

- Los tests usan el flujo HTTP real cuando corresponde, incluyendo autenticación, autorización,
  antiforgery, Entity Framework Core y SQLite.
- Cada test asociado a un requerimiento DEBE identificar en su `DisplayName` el RF, RNF o AC que
  verifica. Los métodos siguen las convenciones de nombres vigentes del repositorio.
- Los datos de tests, fixtures y desarrollo son **completamente ficticios**. NUNCA se usan documentos
  médicos, nombres, instituciones ni información real.
- Los archivos de prueba se generan programáticamente siempre que sea posible; NUNCA se copian de un
  caso médico real.
- Toda corrección de defecto reproducible mediante un test DEBE incorporar un test de regresión que
  falle antes de la corrección y pase después.
- La cobertura NO se persigue por porcentaje de líneas: se prioriza invariantes, seguridad, criterios
  de aceptación, límites, errores y regresiones.

**Racional**: los requerimientos críticos de este sistema son de seguridad y aislamiento; solo se
verifican de punta a punta, sobre la aplicación real, no sobre un controlador aislado.

### V. Simplicidad antes que Abstracción

Se usa la solución más simple, explícita y mantenible que satisfaga completamente los requerimientos.

- La arquitectura actual es deliberadamente pequeña —una aplicación ASP.NET Core MVC, Entity
  Framework Core, SQLite, almacenamiento privado de archivos y un proyecto de tests— y se preserva
  mientras siga siendo suficiente.
- NO se agregan capas, patrones, frameworks, dependencias ni infraestructura sin una necesidad
  concreta derivada de un requerimiento.
- En particular, NO se introducen por defecto repositorios genéricos, un Unit of Work adicional sobre
  Entity Framework Core, CQRS, MediatR, microservicios, buses de mensajes, arquitecturas
  distribuidas, motores de búsqueda externos ni servicios administrados solo porque se los considere
  buena práctica general.
- NO se crea una capa de servicios de aplicación si la funcionalidad puede quedar clara y correcta
  siguiendo la estructura existente.
- Toda dependencia nueva DEBE justificar qué problema concreto resuelve y por qué la implementación
  existente o la plataforma .NET no alcanzan.
- Se prefiere código directo y legible antes que abstracciones anticipadas para necesidades futuras
  hipotéticas.
- La simplicidad también rige en la PWA: service worker propio y mínimo, sin frameworks de caching
  mientras no exista un requerimiento que lo justifique.

**Racional**: el proyecto tiene restricciones económicas y de mantenimiento explícitas. Cada
abstracción no exigida por un requerimiento es costo permanente sin beneficio verificable.

## Restricciones Tecnológicas y Operativas

Estas restricciones son estables y transversales; el detalle de configuración y de comandos vive en
`AGENTS.md` y `CLAUDE.md`, y el detalle de requerimientos en `PRD2.md`.

- El proyecto se mantiene dentro de las restricciones económicas y tecnológicas de `PRD2.md`,
  incluyendo el objetivo de bajo costo operativo.
- NO se incorporan dependencias obligatorias de IA, OCR ni motores de búsqueda administrados. La
  búsqueda se resuelve con las capacidades de SQLite vía Entity Framework Core, sobre metadatos
  cargados a mano: el sistema nunca lee el contenido interno de los archivos.
- El contenido médico NUNCA se usa para entrenar modelos.
- La base de datos y sus archivos auxiliares viven fuera de toda carpeta pública y fuera del
  repositorio; NO se versionan ni se exponen.
- El alta de cuentas es administrativa y externa a la aplicación, con el máximo definido en
  `PRD2.md`. NO se agrega registro abierto, recuperación de contraseña en la app, ni ninguna función
  que cruce datos entre cuentas.
- Toda dependencia con restricciones de licencia o de costo se mantiene en la línea de versión
  permitida por `PRD2.md`; actualizar fuera de esa línea requiere una decisión explícita.

## Flujo de Trabajo y Puertas de Calidad

**Definición de terminado.** Una tarea de implementación no está terminada hasta que:

1. `dotnet build` compila sin errores y sin warnings nuevos.
2. `dotnet test` pasa la suite completa.
3. Los criterios de aceptación afectados están cubiertos por tests cuando corresponde.
4. Se verificó el aislamiento entre propietarios para todo acceso nuevo a datos médicos.
5. No se introdujeron datos médicos en logs, métricas, mensajes de error ni excepciones.
6. El cambio mantiene trazabilidad con `PRD2.md`, citando los RF/RNF/AC que satisface.

**Granularidad de commits.** Un commit por paso que funciona, no uno por feature: cada vez que el
árbol vuelve a un estado consistente —compila y los tests pasan— eso es un punto de retorno. El
formato de mensaje está en `AGENTS.md`.

**Revisión.** Toda revisión de cambios verifica explícitamente el cumplimiento de estos principios,
además de la corrección funcional.

## Gobernanza

Estos principios son obligatorios para todas las especificaciones, planes, tareas, implementaciones y
revisiones creadas con Spec Kit.

**Orden de autoridad documental** (de mayor a menor):

1. `PRD2.md` — alcance, requerimientos y criterios de aceptación.
2. Esta constitución — principios globales de ingeniería.
3. `AGENTS.md` y `CLAUDE.md` — instrucciones concretas de implementación y operación.
4. La especificación de cada feature — comportamiento específico de esa feature.
5. El código existente — es la implementación actual, NUNCA una justificación para contradecir un
   documento superior.

**Cumplimiento.** Toda especificación y todo plan DEBEN verificar explícitamente su cumplimiento con
esta constitución. Una propuesta que viole alguno de los principios se modifica antes de pasar a
implementación; no se implementa "y después se arregla".

**Excepciones.** Una excepción a un principio DEBE ser explícita, estar justificada técnicamente y
quedar registrada en el plan de la feature. Si la excepción afecta alcance o requerimientos, primero
se modifica formalmente `PRD2.md`.

**Enmiendas.** Una enmienda requiere: (a) la redacción propuesta, (b) el motivo y el impacto sobre
artefactos dependientes, y (c) la actualización de este archivo con su Sync Impact Report. Los cinco
principios son reglas duraderas del proyecto: una enmienda puede precisarlos o endurecerlos, pero NO
debilitarlos ni eliminarlos sin una decisión deliberada registrada como cambio MAJOR.

**Versionado.** Versionado semántico sobre esta constitución:

- **MAJOR**: se elimina o redefine un principio de forma incompatible con lo anterior.
- **MINOR**: se agrega un principio o una sección, o se amplía materialmente una guía existente.
- **PATCH**: aclaraciones, redacción, correcciones sin cambio semántico.

**Alcance de este documento.** Esta constitución NO es una copia de `PRD2.md` ni de `AGENTS.md`:
contiene principios estables y transversales, y usa esos documentos como fuente de detalle.

**Versión**: 1.0.0 | **Ratificada**: 2026-08-19 | **Última enmienda**: 2026-08-19
