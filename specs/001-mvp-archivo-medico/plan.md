# Plan de Implementación: MVP Mi Archivo Médico

**Rama git**: `v4-sdd` | **Directorio de feature**: `specs/001-mvp-archivo-medico` | **Fecha**: 2026-08-19
| **Spec**: [spec.md](./spec.md)

**Entrada**: especificación de `specs/001-mvp-archivo-medico/spec.md`, sobre `PRD2.md` revisión 3.

## Resumen

Construir desde cero una PWA de uso familiar (hasta 5 cuentas) para cargar, organizar y encontrar
estudios médicos, donde cada cuenta accede exclusivamente a sus propios datos. El repositorio no
contiene código todavía: este plan describe un árbol nuevo, no una modificación.

El enfoque técnico es deliberadamente pequeño y está fijado por `AGENTS.md`: **una** aplicación
ASP.NET Core MVC con vistas Razor, **una** base SQLite en archivo, **una** carpeta privada de archivos
cifrados y **un** proyecto de tests de integración. Sin capa de servicios de aplicación, sin
repositorios, sin microservicios.

La decisión estructural del plan es **dónde viven las invariantes**, porque de eso depende que el
Principio I se cumpla por construcción y no por disciplina:

| Invariante | Punto autoritativo único |
|---|---|
| Aislamiento por propietario | Filtros globales de EF Core aplicados por reflexión en `ArchivoMedicoDbContext.OnModelCreating` a toda entidad `IPropiedadDeUsuario` |
| Estampado de `OwnerId` y columnas normalizadas | Interceptación de `SaveChanges`/`SaveChangesAsync` |
| Normalización de texto | `NormalizadorDeTexto.Normalizar`, usada al persistir y al buscar |
| Búsqueda | `BuscadorDeEstudios` sobre columnas normalizadas |
| Pipeline de archivos | `ServicioDeCargaDeArchivos` + `IAlmacenamientoDeArchivos` |
| Tiempo | `TimeProvider` inyectado |
| Autorización | `FallbackPolicy` global; el acceso anónimo es excepción explícita |

## Contexto Técnico

**Lenguaje/Versión**: C# sobre .NET 8 (LTS).

**Dependencias principales**: ASP.NET Core MVC, Entity Framework Core con
`Microsoft.EntityFrameworkCore.Sqlite`, ASP.NET Core Identity (solo para el modelo de usuario y el
hashing), SixLabors.ImageSharp fijado en 3.1.12 (Apache-2.0) para la validación estructural de
imágenes, `Microsoft.Extensions.TimeProvider.Testing` en los tests. Sin librerías de PWA: manifiesto y
service worker escritos a mano.

**Almacenamiento**: SQLite en un único archivo del servidor, en modo WAL, fuera de toda carpeta
pública, para los metadatos. Los archivos médicos van a una carpeta privada del servidor, cifrados con
AES-256 y con nombre físico GUID.

**Testing**: xUnit con `WebApplicationFactory<Program>` sobre la aplicación real, base SQLite
descartable por corrida y `FakeTimeProvider`. Ejercita HTTP real, no controladores.

**Plataforma de destino**: servidor Windows/IIS o Linux; navegadores compatibles con PWA en
computadora, tableta y teléfono.

**Tipo de proyecto**: aplicación web con render en servidor. Un solo proyecto ejecutable, sin frontend
separado.

**Objetivos de rendimiento**: búsqueda p95 < 1 s y listado inicial p95 < 2 s sobre 2.000 estudios
(SC-003, SC-004); carga de 10 MB en < 15 s a 10 Mbps (SC-005).

**Restricciones**: costo de infraestructura ≤ USD 15/mes (SC-014); cupo total de archivos de 20 GB;
hasta 5 cuentas; sin dependencias de IA, OCR ni motores de búsqueda administrados; un archivo aceptado
nunca se materializa entero en memoria.

**Escala/Alcance**: hasta 5 cuentas concurrentes, hasta 2.000 estudios, hasta 20 archivos por estudio,
20 GB en total. Alrededor de 12 pantallas.

## Verificación de la Constitución

*PUERTA: debe pasar antes de la Fase 0 y volver a evaluarse después de la Fase 1.*

### Evaluación previa a la Fase 0

| Principio | Estado | Cómo lo satisface este plan |
|---|---|---|
| I. Seguridad y privacidad por construcción | **Pasa** | Las siete invariantes de la tabla del Resumen viven en un único punto central. `FallbackPolicy` protege por omisión. La clave de cifrado se resuelve de configuración externa y la aplicación falla al arrancar si no está. Ninguna ruta expone datos médicos en la dirección (RNF-63). |
| II. Trazabilidad al PRD y control del alcance | **Pasa** | El spec traza 200/200 identificadores vigentes de `PRD2.md` revisión 3, sin deuda. Este plan no introduce ninguna capacidad ausente del PRD. |
| III. Invariantes centralizadas | **Pasa** | Cada regla transversal tiene un único dueño, enumerado en el Resumen. Los controladores no repiten filtros de propietario ni normalización. |
| IV. Testing guiado por criterios de aceptación | **Pasa con condición** | La suite es de integración sobre la aplicación real y cada test nombra su RF/RNF/AC. La condición: 12 criterios no son alcanzables por un test de integración y necesitan método declarado. Resuelto en `research.md` §6. |
| V. Simplicidad antes que abstracción | **Pasa con dos anotaciones** | Un proyecto web y uno de tests; sin repositorios, sin Unit of Work adicional, sin CQRS ni mediador. Dos incorporaciones de infraestructura requieren justificación: estado de sesión del lado del servidor y protección de datos para los tokens de archivo. Ver Seguimiento de Complejidad. |

**Resultado: puerta superada.** Sin violaciones sin justificar.

### Reevaluación posterior a la Fase 1

Los artefactos de diseño (`data-model.md`, `contracts/rutas.md`, `quickstart.md`) no introdujeron
ninguna capa, patrón ni dependencia adicional respecto de esta evaluación. Las dos anotaciones del
Principio V siguen siendo las únicas, y ambas quedaron acotadas: el estado de sesión guarda un criterio
de búsqueda y nada más, y la protección de datos es un componente de la plataforma, no un paquete
nuevo. **La puerta sigue superada.**

## Estructura del Proyecto

### Documentación (esta feature)

```text
specs/001-mvp-archivo-medico/
├── plan.md              # Este archivo
├── research.md          # Fase 0: decisiones técnicas y método de verificación por criterio
├── data-model.md        # Fase 1: entidades, columnas normalizadas, invariantes de persistencia
├── quickstart.md        # Fase 1: cómo levantar, configurar y validar de punta a punta
├── contracts/
│   └── rutas.md         # Fase 1: contrato de rutas HTTP, autorización y respuestas
├── checklists/          # requirements.md, security.md, ux.md, testabilidad.md
└── tasks.md             # Fase 2: lo genera /speckit-tasks, no este comando
```

### Código fuente (raíz del repositorio)

```text
src/MiArchivoMedico.Web/
├── Program.cs                          # Composición: servicios, middleware, FallbackPolicy, arranque
├── Controllers/
│   ├── HomeController.cs               # Inicio y /sin-conexion (único [AllowAnonymous] con vista)
│   ├── CuentaController.cs             # Inicio y cierre de sesión
│   ├── EstudiosController.cs           # Listado, búsqueda, detalle, alta, edición, eliminación
│   └── ArchivosController.cs           # Visualización y descarga con token temporal
├── Dominio/
│   ├── IPropiedadDeUsuario.cs          # Marca que dispara el filtro global
│   ├── Usuario.cs                      # IdentityUser
│   ├── Estudio.cs
│   ├── ArchivoDeEstudio.cs
│   ├── EtiquetaDeEstudio.cs
│   └── IntentoDeInicioDeSesion.cs      # Contador de RNF-60 y RNF-65
├── Data/
│   ├── ArchivoMedicoDbContext.cs       # Filtros globales, conversores, PrepararEntidades
│   ├── InicializadorDeBaseDeDatos.cs   # Migraciones, PRAGMA WAL, siembra de CuentasIniciales
│   └── Migraciones/
├── Servicios/
│   ├── IUsuarioActual.cs               # Id null sin sesión
│   ├── NormalizadorDeTexto.cs          # Única función de normalización
│   ├── BuscadorDeEstudios.cs           # Única implementación de búsqueda y filtros
│   ├── CriterioDeBusqueda.cs           # Término, rango de fechas, institución, página
│   ├── EstadoDeBusqueda.cs             # Persistencia del criterio en la sesión (RNF-63, RF-40)
│   ├── ServicioDeCargaDeArchivos.cs    # Tránsito, validación, SHA-256, cupo, traslado
│   ├── ValidadorDeArchivos.cs          # Extensión + MIME + firma + validación estructural
│   ├── IAlmacenamientoDeArchivos.cs
│   ├── AlmacenamientoCifradoEnDisco.cs # AES-256-CBC en flujo, [IV 16][cifrado], nombre GUID
│   ├── GeneradorDeTokenDeArchivo.cs    # Token temporal de 5 minutos (RNF-07)
│   ├── ControlDeIntentosDeInicioDeSesion.cs # RNF-60 y RNF-65
│   └── HasherPbkdf2Sha256.cs           # PBKDF2-HMAC-SHA256, formato Identity V3 (RNF-03)
├── Models/                             # ViewModels por pantalla
├── Views/
│   ├── Shared/                         # Layout, parciales de mensajes y de paginación
│   ├── Home/                           # Index, SinConexion (sin layout)
│   ├── Cuenta/                         # InicioDeSesion
│   ├── Estudios/                       # Index, Detalle, Crear, Editar, Eliminar
│   └── Archivos/                       # Ver
└── wwwroot/
    ├── manifest.webmanifest
    ├── sw.js                           # Lista ESTATICOS y nada más
    ├── js/carga.js                     # Progreso, bloqueo de reenvío, aviso de corte (RNF-66, RF-28)
    ├── css/
    └── iconos/

tests/MiArchivoMedico.Tests/
├── AplicacionDePrueba.cs               # WebApplicationFactory, SQLite descartable, FakeTimeProvider
├── Apoyo/
│   ├── ClienteDeSesion.cs              # Login y logout resolviendo el antiforgery
│   ├── ClienteDeEstudios.cs            # Alta, edición y búsqueda por HTTP
│   └── ArchivosFicticios.cs            # Genera PDF, PDF con JavaScript, JPG y PNG
├── AislamientoPorPropietarioTests.cs   # RNF-53: falla si una entidad médica queda sin filtro
├── AutenticacionTests.cs
├── BloqueoDeIntentosTests.cs
├── EstudiosTests.cs
├── ValidacionDeArchivosTests.cs
├── EntregaDeArchivosTests.cs
├── BusquedaYFiltrosTests.cs
├── ListadoYPaginacionTests.cs
├── PwaTests.cs
├── PrivacidadEnLogsTests.cs
└── ArranqueTests.cs                    # AC-83: sin clave de cifrado, el host no arranca
```

**Decisión de estructura**: un único proyecto ejecutable más un proyecto de tests. La carpeta
`Servicios/` agrupa colaboradores de dominio, **no** es una capa de servicios de aplicación: los
controladores hablan directamente con `ArchivoMedicoDbContext` y con esos colaboradores. No hay
repositorios ni interfaces por entidad. La única abstracción con interfaz es
`IAlmacenamientoDeArchivos`, exigida por la restricción del PRD de poder reemplazar el proveedor de
almacenamiento sin tocar las reglas de dominio.

## Estrategia de Verificación

`research.md` §6 clasifica los 99 criterios de aceptación vigentes por método. Resumen:

| Método | Cantidad | Ejemplos |
|---|---|---|
| Test de integración sobre HTTP real | 75 | AC-01 a AC-05, AC-09 a AC-14, AC-47 a AC-50, AC-90 a AC-100 |
| Test de integración con tiempo simulado | 6 | AC-06, AC-07, AC-08, AC-69, AC-86, AC-87 |
| Test de arranque del host | 1 | AC-83 |
| Medición de rendimiento con instrumental propio | 3 | AC-51, AC-52, AC-53 |
| Comprobación manual en navegador | 8 | AC-26, AC-38, AC-39, AC-40, AC-41, AC-42, AC-78, AC-79 |
| Inspección de infraestructura | 6 | AC-56, AC-59, AC-60, AC-61, AC-67, AC-68 |

Suman 99, que son todos los criterios vigentes.

**Corrección respecto de lo estimado antes de este plan**: al clasificar uno por uno resultaron **17**
los criterios fuera del alcance de la suite habitual —8 manuales, 6 de infraestructura y 3 de
medición—, no los 12 que había contado por encima al armar `checklists/testabilidad.md`. Ninguno de
esos 17 genera una tarea de test automatizado: generan una tarea de comprobación con procedimiento
escrito, ejecutada antes de la entrega. Que no sean automatizables no los vuelve opcionales.

Dos de ellos tienen una parte que sí se automatiza y conviene no perderla: de AC-26 se verifica en la
suite que la respuesta lleve las cabeceras que impiden la ejecución de contenido activo y que la vista
incruste el archivo en un marco restringido —lo que no se puede verificar sin navegador es que el
script efectivamente no corra—; de AC-78, que ni el listado ni el detalle incluyan un elemento que
descargue contenido por su cuenta.

## Seguimiento de Complejidad

> Solo las dos anotaciones del Principio V. Ninguna es una violación: son incorporaciones de
> infraestructura que un requisito exige y que no tienen alternativa más simple.

| Incorporación | Por qué hace falta | Alternativa más simple, y por qué se descartó |
|---|---|---|
| Estado de sesión del lado del servidor para el criterio de búsqueda | RNF-63 prohíbe que el término de búsqueda viaje en la dirección, y RF-40 exige conservarlo al paginar y al volver del detalle de un estudio. Sin un lugar del lado del servidor donde guardarlo, ambos requisitos son incompatibles. | Campos ocultos en un formulario: obligaría a que cada enlace al detalle de un estudio fuera un formulario con envío por POST, y aun así el botón de volver del navegador perdería el criterio. Se descartó por ser más código y peor resultado. |
| Protección de datos de ASP.NET Core para el token temporal de archivo | RNF-07 exige que el acceso al archivo caduque a los 5 minutos y AC-84 que ninguna dirección entregue sin sesión. Un token firmado con vencimiento resuelve la primera mitad; la sesión, la segunda. | Guardar los tokens emitidos en la base: agrega una tabla, escrituras en cada visualización y una tarea de limpieza, para obtener lo mismo que da un componente que ya viene en la plataforma. |

Ninguna de las dos agrega un paquete NuGet: ambas son parte de ASP.NET Core.
