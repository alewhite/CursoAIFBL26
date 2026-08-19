# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

@AGENTS.md

## Fuente de verdad del alcance

`PRD2.md` (revisión 2, endurecida) es la fuente única del alcance y de los requerimientos (RF/RNF/AC).
No se importa acá por su tamaño: leerlo cuando la tarea toque alcance, requerimientos o criterios de
aceptación, y citar los identificadores al proponer o implementar un cambio.
`PRD.md` es la revisión 1 y se conserva solo como referencia histórica: no citarlo ni implementar contra él
(los AC-69 en adelante y buena parte de los RNF existen únicamente en `PRD2.md`).

## Comandos

Los de uso diario están en AGENTS.md. Detalles propios de este árbol:

```bash
dotnet run --project src/MiArchivoMedico.Web          # https://localhost:7028
dotnet test --filter "FullyQualifiedName~AislamientoPorPropietarioTests"   # un archivo
dotnet test --filter "DisplayName~RNF-53"                                  # por requerimiento
dotnet ef migrations add <Nombre> --project src/MiArchivoMedico.Web --output-dir Data/Migraciones
```

Las migraciones se aplican solas al arrancar (`InicializadorDeBaseDeDatos.InicializarAsync`, llamado al
final de `Program.cs`), que además reafirma `PRAGMA journal_mode=WAL` y siembra las cuentas de
`CuentasIniciales`. Correr `dotnet ef database update` a mano no hace falta salvo para inspeccionar la base.

## Arquitectura

Un solo proyecto web (`src/MiArchivoMedico.Web`, MVC clásico con vistas Razor) y un proyecto de tests de
integración (`tests/MiArchivoMedico.Tests`). No hay capa de servicios de aplicación ni repositorios: los
controladores hablan con `ArchivoMedicoDbContext` y con tres colaboradores de dominio
(`BuscadorDeEstudios`, `ServicioDeCargaDeArchivos`, `IAlmacenamientoDeArchivos`).

Las invariantes críticas están puestas en lugares donde no se pueden olvidar. Vale la pena conocerlas antes
de tocar algo, porque cambian la forma de escribir el código:

- **El aislamiento por cuenta no se escribe en cada consulta.** `ArchivoMedicoDbContext.OnModelCreating`
  recorre el modelo por reflexión y le pone un `HasQueryFilter(e => e.OwnerId == _usuarioActual.Id)` a toda
  entidad que implemente `IPropiedadDeUsuario`. Una entidad médica nueva queda aislada con solo implementar
  esa interfaz, y `AislamientoPorPropietarioTests` falla si alguna queda afuera. Corolario: no escribir
  `Where(e => e.OwnerId == ...)` a mano, y **nunca** usar `Find`/`FindAsync` para datos médicos, porque no
  aplica filtros globales.
- **`IUsuarioActual.Id` es `null` sin sesión**, y ninguna fila iguala `null`: una consulta sin autenticar
  devuelve vacío en lugar de todo.
- **`SaveChanges`/`SaveChangesAsync` están interceptados** (`PrepararEntidades`): estampan el `OwnerId` en
  las entidades nuevas y recalculan las columnas normalizadas de `Estudio` y `EtiquetaDeEstudio`. Ningún
  camino de escritura puede saltearse eso, y por lo tanto tampoco hay que replicarlo en los controladores.
- **Autorización por omisión**: `FallbackPolicy` exige sesión en todo el sitio. Una pantalla nueva nace
  protegida; abrirla requiere `[AllowAnonymous]` explícito.
- **Búsqueda sobre columnas normalizadas**: cada campo buscable se persiste además en su versión
  normalizada (`NormalizadorDeTexto.Normalizar`, con índice propio), y `BuscadorDeEstudios` normaliza el
  término entrante con la misma función. Las dos puntas tienen que usar esa función o la búsqueda deja de
  coincidir.
- **Carga de archivos en dos tiempos**: `ServicioDeCargaDeArchivos` recibe en un área de tránsito, valida
  (extensión + MIME + firma binaria + validación estructural), calcula el SHA-256 y recién entonces
  `AlmacenamientoCifradoEnDisco` mueve el contenido cifrado (AES-256-CBC en flujo, `[IV 16 bytes][cifrado]`,
  nombre físico = GUID). Los rechazados se borran del tránsito. Un archivo aceptado nunca se materializa
  entero en memoria.
- **Entrega de archivos**: `ArchivosController` sirve el contenido con CSP `sandbox` y `nosniff`, y la vista
  lo incrusta en un `iframe` con `sandbox`. Un identificador ajeno responde 404 por el filtro global, no por
  un chequeo explícito.
- **El tiempo se inyecta**: todo usa `TimeProvider` (registrado como singleton). No usar `DateTimeOffset.Now`
  ni `DateTime.UtcNow` directo, o el test correspondiente no puede ejercitar expiraciones ni ventanas de
  bloqueo.

## Convenciones de los tests

Son de integración, sobre la aplicación real: `AplicacionDePrueba` es un `WebApplicationFactory<Program>`
con base SQLite descartable en `Path.GetTempPath()`, `FakeTimeProvider` fijado en 2026-01-15 (también
inyectado en el manejador de cookies) y dos cuentas ficticias sembradas por configuración.

- La configuración se pasa con `builder.UseSetting`, no con `ConfigureAppConfiguration`: `Program` lee la
  cadena de conexión durante `CreateBuilder`, antes de que corran los callbacks de la fábrica.
- Se ejercita el HTTP real, no los controladores: `ClienteDeSesion` (login/logout con token
  antifalsificación) y `ClienteDeEstudios` son las extensiones de `HttpClient` que ya resuelven el
  formulario y el antiforgery. Reusarlas en vez de armar el POST a mano.
- Los archivos de prueba se **generan** con `ArchivosFicticios` (PDF mínimo válido, PDF con JavaScript,
  imágenes con ImageSharp). Nunca se copia un archivo de un caso real (RNF-10).
- Cada `[Fact]` lleva `DisplayName` con el identificador del PRD que verifica
  (`[Fact(DisplayName = "RNF-53: ...")]`), y el método se nombra en español con la forma
  `Condicion_ResultadoEsperado`. Un archivo por área.

## Commits

El formato y la granularidad están en AGENTS.md; el historial sirve de ejemplo del asunto
(`feat(busqueda): agrega busqueda por metadatos, filtros y paginacion`). Lo que más se olvida: **commitear
cada vez que el árbol vuelve a compilar y pasar los tests**, no una vez por feature terminada.
