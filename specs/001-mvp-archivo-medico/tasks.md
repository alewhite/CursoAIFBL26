---
description: "Lista de tareas para el MVP de Mi Archivo Médico"
---

# Tareas: MVP Mi Archivo Médico

**Entrada**: documentos de diseño de `specs/001-mvp-archivo-medico/`

**Prerrequisitos**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md),
[data-model.md](./data-model.md), [contracts/rutas.md](./contracts/rutas.md)

**Tests**: **obligatorios**. El Principio IV de la constitución exige que el comportamiento observable
esté protegido por tests de integración sobre la aplicación real, y cada test nombra en su
`DisplayName` el RF, RNF o AC de `PRD2.md` que verifica.

**Organización**: por historia de usuario, para poder construir, probar y demostrar cada una por
separado.

## Formato: `[ID] [P?] [Historia] Descripción con ruta de archivo`

- **[P]**: se puede hacer en paralelo con otras [P] de la misma fase (archivos distintos, sin
  dependencias pendientes entre sí).
- **[US1]…[US7]**: historia de usuario de `spec.md` a la que pertenece la tarea.

## Convenciones de rutas

Proyecto web en `src/MiArchivoMedico.Web/`, tests en `tests/MiArchivoMedico.Tests/`, según la estructura
fijada en [plan.md](./plan.md).

## Regla de commits

Un commit por paso que funciona, no uno por historia: cada vez que el árbol compila y los tests pasan,
eso es un commit, aunque la historia esté a medio camino.

---

## Fase 1: Preparación (infraestructura compartida)

**Propósito**: crear el árbol, las dependencias y la configuración. Nada de dominio todavía.

- [ ] T001 Crear la solución `MiArchivoMedico.sln` en la raíz del repositorio
- [ ] T002 Crear el proyecto web ASP.NET Core MVC sobre .NET 8 en `src/MiArchivoMedico.Web/MiArchivoMedico.Web.csproj`
- [ ] T003 Crear el proyecto de tests xUnit en `tests/MiArchivoMedico.Tests/MiArchivoMedico.Tests.csproj` con referencia al proyecto web
- [ ] T004 [P] Agregar los paquetes de datos e identidad (`Microsoft.EntityFrameworkCore.Sqlite`, `Microsoft.EntityFrameworkCore.Design`, `Microsoft.AspNetCore.Identity.EntityFrameworkCore`) en `src/MiArchivoMedico.Web/MiArchivoMedico.Web.csproj`
- [ ] T005 [P] Agregar `SixLabors.ImageSharp` fijado en `3.1.12` en `src/MiArchivoMedico.Web/MiArchivoMedico.Web.csproj`, con comentario de que la línea 4.x exige licencia paga y rompe RNF-45
- [ ] T006 [P] Agregar los paquetes de prueba (`Microsoft.AspNetCore.Mvc.Testing`, `Microsoft.Extensions.TimeProvider.Testing`, `SixLabors.ImageSharp`) en `tests/MiArchivoMedico.Tests/MiArchivoMedico.Tests.csproj`
- [ ] T007 [P] Crear `.gitignore` en la raíz excluyendo `bin/`, `obj/`, `*.db`, `*.db-wal`, `*.db-shm` y la carpeta de almacenamiento local (RNF-34)
- [ ] T008 Crear `src/MiArchivoMedico.Web/appsettings.json` sin ningún secreto, y documentar en él que la cadena de conexión, la ruta y la clave de cifrado se cargan por user-secrets o variables de entorno

**Punto de control**: `dotnet build` compila una solución vacía pero completa.

---

## Fase 2: Fundaciones (prerrequisitos bloqueantes)

**Propósito**: las invariantes que el Principio I y el Principio III exigen que vivan en un único punto.
Todo lo de esta fase se escribe una vez y ninguna historia lo repite.

**⚠️ CRÍTICO**: ninguna historia de usuario puede empezar hasta que esta fase esté completa.

- [ ] T009 [P] Crear la interfaz marcadora `IPropiedadDeUsuario` con `OwnerId` en `src/MiArchivoMedico.Web/Dominio/IPropiedadDeUsuario.cs`
- [ ] T010 [P] Crear `IUsuarioActual` y su implementación en `src/MiArchivoMedico.Web/Servicios/UsuarioActual.cs`, devolviendo `Id` nulo cuando no hay sesión (RNF-53)
- [ ] T011 [P] Crear `NormalizadorDeTexto.Normalizar` en `src/MiArchivoMedico.Web/Servicios/NormalizadorDeTexto.cs`: minúsculas, sin acentos, sin espacios sobrantes (RNF-55)
- [ ] T012 [P] Crear la entidad `Usuario` derivada de `IdentityUser` en `src/MiArchivoMedico.Web/Dominio/Usuario.cs`
- [ ] T013 [P] Crear la entidad `Estudio` con sus columnas originales y normalizadas en `src/MiArchivoMedico.Web/Dominio/Estudio.cs`, implementando `IPropiedadDeUsuario` (data-model.md)
- [ ] T014 [P] Crear la entidad `ArchivoDeEstudio` en `src/MiArchivoMedico.Web/Dominio/ArchivoDeEstudio.cs`, implementando `IPropiedadDeUsuario`
- [ ] T015 [P] Crear la entidad `EtiquetaDeEstudio` con su columna normalizada en `src/MiArchivoMedico.Web/Dominio/EtiquetaDeEstudio.cs`, implementando `IPropiedadDeUsuario`
- [ ] T016 Crear `ArchivoMedicoDbContext` en `src/MiArchivoMedico.Web/Data/ArchivoMedicoDbContext.cs` con el filtro global por reflexión sobre `IPropiedadDeUsuario`, el conversor de `DateTimeOffset` a ticks UTC y los índices de las columnas normalizadas (RNF-53, RNF-55)
- [ ] T017 Implementar la interceptación de `SaveChanges`/`SaveChangesAsync` (`PrepararEntidades`) en `src/MiArchivoMedico.Web/Data/ArchivoMedicoDbContext.cs`: estampa `OwnerId` en las entidades nuevas y recalcula todas las columnas normalizadas
- [ ] T018 Reemplazar el hasher de Identity por `HasherPbkdf2Sha256` (PBKDF2-HMAC-SHA256, formato Identity V3) en `src/MiArchivoMedico.Web/Servicios/HasherPbkdf2Sha256.cs`, que falla al construirse si `IterationCount` es menor a 100.000 (RNF-03)
- [ ] T019 Configurar en `src/MiArchivoMedico.Web/Program.cs` la validación de configuración obligatoria al arrancar: cadena de conexión, ruta de almacenamiento y clave AES-256; sin clave, el arranque falla (RNF-62, AC-83)
- [ ] T020 Configurar en `src/MiArchivoMedico.Web/Program.cs` el `FallbackPolicy` que exige sesión en todo el sitio, el registro de `TimeProvider` como singleton y el estado de sesión del servidor (research.md §3)
- [ ] T021 Crear `InicializadorDeBaseDeDatos.InicializarAsync` en `src/MiArchivoMedico.Web/Data/InicializadorDeBaseDeDatos.cs`: aplica migraciones, reafirma `PRAGMA journal_mode=WAL` y siembra `CuentasIniciales` omitiendo las existentes, con tope de 5 cuentas (RNF-56)
- [ ] T022 Generar la migración inicial en `src/MiArchivoMedico.Web/Data/Migraciones/` con `dotnet ef migrations add Inicial`
- [ ] T023 [P] Crear el layout base y el CSS adaptable desde 360 píxeles en `src/MiArchivoMedico.Web/Views/Shared/_Layout.cshtml` y `src/MiArchivoMedico.Web/wwwroot/css/sitio.css` (RNF-29, RNF-30)
- [ ] T024 [P] Crear la fábrica de pruebas `AplicacionDePrueba` en `tests/MiArchivoMedico.Tests/AplicacionDePrueba.cs`: `WebApplicationFactory<Program>`, base SQLite descartable, almacenamiento temporal, `FakeTimeProvider` fijado en 2026-01-15 e inyectado también en el manejador de la cookie, y dos cuentas ficticias sembradas por `UseSetting`
- [ ] T025 [P] Crear los ayudantes de HTTP `ClienteDeSesion` y `ClienteDeEstudios` en `tests/MiArchivoMedico.Tests/Apoyo/`, que resuelven el formulario y el token antifalsificación
- [ ] T026 [P] Crear `ArchivosFicticios` en `tests/MiArchivoMedico.Tests/Apoyo/ArchivosFicticios.cs`, que **genera** PDF mínimo válido, PDF con JavaScript embebido, PDF truncado sin `%%EOF`, JPG y PNG con ImageSharp, y un binario que simula un ejecutable (RNF-10)
- [ ] T027 Escribir `AislamientoPorPropietarioTests` en `tests/MiArchivoMedico.Tests/AislamientoPorPropietarioTests.cs`, que recorre el modelo y falla si alguna entidad `IPropiedadDeUsuario` quedó sin filtro global (RNF-53)

**Punto de control**: la aplicación arranca, la base se crea sola, y el test que guarda el aislamiento
está en verde. Recién ahora pueden empezar las historias.

---

## Fase 3: Historia 1 — Acceso privado y aislado (P1) 🎯 MVP

**Objetivo**: cinco personas comparten la instalación sin compartir un solo dato. Sin sesión no se ve
nada; con sesión ajena, tampoco.

**Prueba independiente**: sembrar dos cuentas con estudios propios y comprobar que sin sesión toda ruta
privada redirige o rechaza, que los recursos de la cuenta B responden 404 a la cuenta A, y que las
reglas de expiración y bloqueo se cumplen.

### Tests de la Historia 1

- [ ] T028 [P] [US1] Escribir los tests de ingreso y cierre de sesión en `tests/MiArchivoMedico.Tests/AutenticacionTests.cs` (AC-01, AC-02, AC-03, AC-04, AC-05)
- [ ] T029 [P] [US1] Escribir los tests de expiración por inactividad y de tope absoluto, con tiempo simulado, en `tests/MiArchivoMedico.Tests/SesionTests.cs` (AC-06, AC-07)
- [ ] T030 [P] [US1] Escribir los tests de bloqueo por intentos fallidos en `tests/MiArchivoMedico.Tests/BloqueoDeIntentosTests.cs` (AC-69, AC-86, AC-87)
- [ ] T031 [P] [US1] Escribir el test de indistinguibilidad ante un nombre de usuario inexistente en `tests/MiArchivoMedico.Tests/BloqueoDeIntentosTests.cs` (AC-98, RNF-65)
- [ ] T032 [P] [US1] Escribir los tests de aislamiento entre propietarios sobre detalle y listado en `tests/MiArchivoMedico.Tests/PropiedadDeDatosTests.cs` (AC-47, AC-49)
- [ ] T033 [P] [US1] Escribir el test que enumera las rutas registradas y comprueba que ninguna acepta alta de cuenta ni cambio de propietario en `tests/MiArchivoMedico.Tests/RutasExpuestasTests.cs` (AC-50, AC-63)
- [ ] T034 [P] [US1] Escribir el test del tope de 5 cuentas activas en `tests/MiArchivoMedico.Tests/CuentasInicialesTests.cs` (AC-62)
- [ ] T035 [P] [US1] Escribir los tests de atributos de la cookie y de formato del hash almacenado en `tests/MiArchivoMedico.Tests/SeguridadDeCredencialesTests.cs` (AC-58, AC-76)
- [ ] T036 [P] [US1] Escribir el test de arranque sin clave de cifrado en `tests/MiArchivoMedico.Tests/ArranqueTests.cs`, construyendo el host directamente y no con la fábrica (AC-83)

### Implementación de la Historia 1

- [ ] T037 [P] [US1] Crear la entidad `IntentoDeInicioDeSesion` en `src/MiArchivoMedico.Web/Dominio/IntentoDeInicioDeSesion.cs`, **sin** implementar `IPropiedadDeUsuario`, porque debe consultarse sin sesión (data-model.md)
- [ ] T038 [US1] Agregar `IntentoDeInicioDeSesion` al contexto y generar la migración correspondiente en `src/MiArchivoMedico.Web/Data/Migraciones/`
- [ ] T039 [US1] Implementar `ControlDeIntentosDeInicioDeSesion` en `src/MiArchivoMedico.Web/Servicios/ControlDeIntentosDeInicioDeSesion.cs`: cuenta contra el nombre ingresado normalizado, bloquea a los 5 fallos en 15 minutos, mantiene 15 minutos, reinicia ante ingreso exitoso y depura las entradas vencidas (RNF-60, RNF-65)
- [ ] T040 [US1] Implementar `CuentaController` en `src/MiArchivoMedico.Web/Controllers/CuentaController.cs`: ingreso con `[AllowAnonymous]`, verificación contra un hash señuelo cuando el usuario no existe, mensaje único y cierre de sesión que invalida (RF-02, RF-03, RNF-12, RNF-13)
- [ ] T041 [P] [US1] Crear la vista de ingreso en `src/MiArchivoMedico.Web/Views/Cuenta/InicioDeSesion.cshtml`, sin layout completo y sin ningún dato médico
- [ ] T042 [US1] Configurar la cookie de autenticación en `src/MiArchivoMedico.Web/Program.cs`: `Secure`, `HttpOnly`, `SameSite=Strict`, expiración deslizante de 30 minutos y tope absoluto de 24 horas que prevalece (RNF-04, RNF-05, RNF-11)
- [ ] T043 [US1] Implementar la respuesta 404 uniforme para recurso ajeno, inexistente o mal formado en `src/MiArchivoMedico.Web/Controllers/EstudiosController.cs` (RNF-53, contracts/rutas.md regla 2)
- [ ] T044 [US1] Configurar la redirección a HTTPS y las cabeceras de transporte en `src/MiArchivoMedico.Web/Program.cs` (RNF-01)

**Punto de control**: la Historia 1 funciona y se demuestra sola. Es el MVP mínimo defendible: un
sistema que todavía no guarda estudios, pero que ya garantiza que nadie ve lo ajeno.

---

## Fase 4: Historia 2 — Guardar un estudio con sus archivos (P1)

**Objetivo**: cargar un estudio con sus archivos, validados, cifrados y con su huella registrada.

**Prueba independiente**: crear estudios con archivos válidos e inválidos y verificar qué queda
almacenado, en qué estado y con qué huella, sin necesidad de búsqueda, edición ni PWA.

### Tests de la Historia 2

- [ ] T045 [P] [US2] Escribir los tests de creación y de metadatos obligatorios en `tests/MiArchivoMedico.Tests/CreacionDeEstudiosTests.cs` (AC-09, AC-10, AC-11, AC-91)
- [ ] T046 [P] [US2] Escribir los tests de metadatos opcionales en `tests/MiArchivoMedico.Tests/MetadatosDeEstudioTests.cs` (AC-13, AC-82, AC-88, AC-89)
- [ ] T047 [P] [US2] Escribir los tests de formatos aceptados en `tests/MiArchivoMedico.Tests/ValidacionDeArchivosTests.cs` (AC-12, AC-20, AC-74, AC-75)
- [ ] T048 [P] [US2] Escribir los tests de rechazo por tamaño, firma incoherente, 0 bytes y PDF truncado en `tests/MiArchivoMedico.Tests/ValidacionDeArchivosTests.cs` (AC-21, AC-22, AC-23, AC-24, AC-44)
- [ ] T049 [P] [US2] Escribir el test de que un archivo rechazado no queda en el almacenamiento definitivo en `tests/MiArchivoMedico.Tests/ValidacionDeArchivosTests.cs` (AC-27)
- [ ] T050 [P] [US2] Escribir los tests de huella SHA-256, nombre físico GUID y nombre original sanitizado en `tests/MiArchivoMedico.Tests/CustodiaDeArchivosTests.cs` (AC-25, AC-65, AC-66)
- [ ] T051 [P] [US2] Escribir el test de cifrado en reposo, leyendo el archivo directamente del almacenamiento temporal, en `tests/MiArchivoMedico.Tests/CustodiaDeArchivosTests.cs` (AC-57)
- [ ] T052 [P] [US2] Escribir los tests de límite de 20 archivos por estudio y de cupo compartido, con cupo diminuto configurado, en `tests/MiArchivoMedico.Tests/LimitesDeCargaTests.cs` (AC-70, AC-55, AC-64, AC-97)
- [ ] T053 [P] [US2] Escribir el test de carga parcial con un archivo inválido entre tres en `tests/MiArchivoMedico.Tests/CargaParcialTests.cs` (AC-90)
- [ ] T054 [P] [US2] Escribir los tests de ubicación de los errores de validación y de conservación de los metadatos ingresados en `tests/MiArchivoMedico.Tests/FormularioDeEstudioTests.cs` (AC-80, AC-100)
- [ ] T055 [P] [US2] Escribir el test de doble envío del mismo formulario, que debe producir un único estudio, en `tests/MiArchivoMedico.Tests/FormularioDeEstudioTests.cs` (AC-99, RNF-66)

### Implementación de la Historia 2

- [ ] T056 [P] [US2] Implementar `ValidadorDeArchivos` en `src/MiArchivoMedico.Web/Servicios/ValidadorDeArchivos.cs`: extensión, tipo declarado, firma binaria y validación estructural de PDF e imágenes con ImageSharp (RNF-15, RNF-16, RNF-17)
- [ ] T057 [P] [US2] Definir `IAlmacenamientoDeArchivos` en `src/MiArchivoMedico.Web/Servicios/IAlmacenamientoDeArchivos.cs`
- [ ] T058 [US2] Implementar `AlmacenamientoCifradoEnDisco` en `src/MiArchivoMedico.Web/Servicios/AlmacenamientoCifradoEnDisco.cs`: AES-256-CBC en flujo, `[IV 16 bytes][cifrado]`, nombre físico GUID, sin materializar el archivo entero en memoria (RNF-02, RNF-22, research.md §5)
- [ ] T059 [US2] Implementar `ServicioDeCargaDeArchivos` en `src/MiArchivoMedico.Web/Servicios/ServicioDeCargaDeArchivos.cs`: recepción en tránsito, validación, cálculo de SHA-256, control de cupo y de cantidad, traslado al almacenamiento definitivo y borrado de los rechazados del tránsito (RNF-19, RNF-21, RNF-61, RNF-64)
- [ ] T060 [US2] Implementar la sanitización del nombre original en `src/MiArchivoMedico.Web/Servicios/ServicioDeCargaDeArchivos.cs`: sin separadores de ruta, sin caracteres de control, sin `..`, truncado a 255, conservando la extensión (RNF-23)
- [ ] T061 [US2] Implementar el alta de estudio en `src/MiArchivoMedico.Web/Controllers/EstudiosController.cs`, con carga parcial: acepta los archivos válidos e informa cada rechazado junto a ese archivo (RF-33, RF-36)
- [ ] T062 [US2] Implementar la validación de título y fecha en `src/MiArchivoMedico.Web/Models/EstudioFormulario.cs`, incluyendo el rechazo de la fecha posterior al día en curso usando `TimeProvider` (RF-34, RF-35, RF-37)
- [ ] T063 [US2] Implementar la devolución del formulario con los metadatos intactos y el aviso de readjuntar los archivos en `src/MiArchivoMedico.Web/Controllers/EstudiosController.cs` (RNF-67)
- [ ] T064 [P] [US2] Crear la vista de alta en `src/MiArchivoMedico.Web/Views/Estudios/Crear.cshtml`, con los mensajes de error junto a su campo y junto a su archivo, en un máximo de tres pasos (RNF-31, RNF-32)
- [ ] T065 [P] [US2] Implementar `carga.js` en `src/MiArchivoMedico.Web/wwwroot/js/carga.js`: indicador de operación en curso y bloqueo del reenvío del formulario (RNF-66)
- [ ] T066 [US2] Implementar el control de envío único en `src/MiArchivoMedico.Web/Controllers/EstudiosController.cs`: el formulario porta una marca de envío que el servidor consume, de modo que un segundo envío del mismo formulario no cree un estudio duplicado aunque el navegador no ejecute JavaScript (RNF-66, AC-99)
- [ ] T067 [US2] Implementar la ruta de agregar archivos a un estudio existente en `src/MiArchivoMedico.Web/Controllers/EstudiosController.cs` (RF-07)

**Punto de control**: las Historias 1 y 2 funcionan por separado. Ya es un producto entregable: guarda
estudios de forma privada y segura, aunque todavía no los muestre bien ni permita buscarlos.

---

## Fase 5: Historia 3 — Consultar, descargar y eliminar (P2)

**Objetivo**: ver el listado, abrir un estudio, mirar un archivo sin descargarlo, descargarlo idéntico
al original y eliminar lo que ya no sirve.

**Prueba independiente**: con estudios sembrados, verificar el orden del listado, la visualización
incrustada, la identidad de la huella tras la descarga y el flujo de eliminación con y sin confirmación.

### Tests de la Historia 3

- [ ] T068 [P] [US3] Escribir el test de orden del listado en `tests/MiArchivoMedico.Tests/ListadoTests.cs` (AC-28)
- [ ] T069 [P] [US3] Escribir los tests de visualización incrustada y de cabeceras que bloquean contenido activo en `tests/MiArchivoMedico.Tests/EntregaDeArchivosTests.cs` (AC-15, y la parte automatizable de AC-26)
- [ ] T070 [P] [US3] Escribir los tests de descarga con huella idéntica antes y después en `tests/MiArchivoMedico.Tests/EntregaDeArchivosTests.cs` (AC-16, AC-77)
- [ ] T071 [P] [US3] Escribir el test de que el listado y el detalle no incluyen ningún elemento que descargue contenido por su cuenta en `tests/MiArchivoMedico.Tests/EntregaDeArchivosTests.cs` (parte automatizable de AC-78)
- [ ] T072 [P] [US3] Escribir los tests de token de archivo vencido y de token vigente sin sesión, con tiempo simulado, en `tests/MiArchivoMedico.Tests/TokenDeArchivoTests.cs` (AC-08, AC-84)
- [ ] T073 [P] [US3] Escribir el test de acceso a un archivo de otro propietario en `tests/MiArchivoMedico.Tests/PropiedadDeDatosTests.cs` (AC-48)
- [ ] T074 [P] [US3] Escribir los tests de eliminación con confirmación, cancelación y liberación del cupo en `tests/MiArchivoMedico.Tests/EliminacionTests.cs` (AC-17, AC-18, AC-19, AC-102)

### Implementación de la Historia 3

- [ ] T075 [P] [US3] Implementar `GeneradorDeTokenDeArchivo` en `src/MiArchivoMedico.Web/Servicios/GeneradorDeTokenDeArchivo.cs` con la protección de datos de ASP.NET Core y vencimiento de 5 minutos (RNF-07, research.md §4)
- [ ] T076 [US3] Implementar el listado ordenado del más reciente al más antiguo en `src/MiArchivoMedico.Web/Controllers/EstudiosController.cs` (RF-15)
- [ ] T077 [US3] Implementar el detalle del estudio en `src/MiArchivoMedico.Web/Controllers/EstudiosController.cs`, sin transferir el contenido de los archivos (RF-11, RNF-28)
- [ ] T078 [US3] Implementar `ArchivosController` en `src/MiArchivoMedico.Web/Controllers/ArchivosController.cs`: visualización y descarga exigiendo token vigente **y** sesión del propietario, con las cabeceras que impiden interpretar el tipo y ejecutar contenido activo (RNF-06, RNF-08, RNF-20)
- [ ] T079 [P] [US3] Crear las vistas de detalle y de visualización en `src/MiArchivoMedico.Web/Views/Estudios/Detalle.cshtml` y `src/MiArchivoMedico.Web/Views/Archivos/Ver.cshtml`, incrustando el archivo en un marco restringido
- [ ] T080 [US3] Implementar la confirmación y la eliminación física del estudio, sus etiquetas, sus filas de archivo y su contenido en disco en `src/MiArchivoMedico.Web/Controllers/EstudiosController.cs` (RF-13, RF-14, RNF-33)
- [ ] T081 [P] [US3] Crear la vista de confirmación en `src/MiArchivoMedico.Web/Views/Estudios/Eliminar.cshtml`, que declara que la operación es irreversible y cuántos archivos alcanza

**Punto de control**: el ciclo de vida completo de un estudio funciona de punta a punta.

---

## Fase 6: Historia 4 — Encontrar un estudio en segundos (P2)

**Objetivo**: encontrar un estudio conocido en menos de 10 segundos, escribiendo un fragmento con o sin
acentos, y acotando por fecha o institución.

**Prueba independiente**: con un conjunto sembrado, ejercitar la búsqueda por cada campo, la
insensibilidad a mayúsculas y acentos, cada filtro por separado y combinados, el contador, la
paginación y los estados vacíos.

### Tests de la Historia 4

- [ ] T082 [P] [US4] Escribir los tests de búsqueda por los cinco campos en `tests/MiArchivoMedico.Tests/BusquedaTests.cs` (AC-29, AC-30, AC-71, AC-72, AC-73)
- [ ] T083 [P] [US4] Escribir los tests de insensibilidad a mayúsculas, acentos y espacios sobrantes en `tests/MiArchivoMedico.Tests/BusquedaTests.cs` (AC-45, AC-46)
- [ ] T084 [P] [US4] Escribir los tests de filtro por rango de fechas, por institución y combinados en `tests/MiArchivoMedico.Tests/FiltrosTests.cs` (AC-31, AC-34, AC-35)
- [ ] T085 [P] [US4] Escribir el test de que la lista de instituciones del filtro no incluye las de otra cuenta en `tests/MiArchivoMedico.Tests/FiltrosTests.cs` (AC-92)
- [ ] T086 [P] [US4] Escribir los tests de limpiar filtros y de contador de resultados en `tests/MiArchivoMedico.Tests/FiltrosTests.cs` (AC-36, AC-37)
- [ ] T087 [P] [US4] Escribir los tests de paginación con avance, retroceso y página indicada en `tests/MiArchivoMedico.Tests/ListadoTests.cs` (AC-54, AC-101)
- [ ] T088 [P] [US4] Escribir el test de persistencia del criterio al paginar y al volver del detalle en `tests/MiArchivoMedico.Tests/EstadoDeBusquedaTests.cs` (AC-95)
- [ ] T089 [P] [US4] Escribir los tests de los dos estados de listado vacío en `tests/MiArchivoMedico.Tests/ListadoVacioTests.cs` (AC-93, AC-94)
- [ ] T090 [P] [US4] Escribir el test de que el término buscado no aparece en la dirección de ninguna solicitud en `tests/MiArchivoMedico.Tests/EstadoDeBusquedaTests.cs` (AC-96, RNF-63)

### Implementación de la Historia 4

- [ ] T091 [P] [US4] Crear `CriterioDeBusqueda` en `src/MiArchivoMedico.Web/Servicios/CriterioDeBusqueda.cs`: término, rango de fechas con ambos extremos incluidos y opcionales, institución y página
- [ ] T092 [US4] Implementar `BuscadorDeEstudios` en `src/MiArchivoMedico.Web/Servicios/BuscadorDeEstudios.cs`: única implementación de búsqueda, sobre las columnas normalizadas, normalizando el término con `NormalizadorDeTexto.Normalizar` (RF-16, RNF-55, RNF-48)
- [ ] T093 [US4] Implementar la paginación de 25 por página con avance, retroceso y página actual en `src/MiArchivoMedico.Web/Servicios/BuscadorDeEstudios.cs` (RNF-27)
- [ ] T094 [US4] Implementar `EstadoDeBusqueda` sobre el estado de sesión en `src/MiArchivoMedico.Web/Servicios/EstadoDeBusqueda.cs`, para conservar el criterio entre solicitudes (RF-40, RNF-63)
- [ ] T095 [US4] Implementar las rutas `Buscar`, `Pagina` y `LimpiarFiltros` por POST en `src/MiArchivoMedico.Web/Controllers/EstudiosController.cs`, sin que ningún dato médico viaje en la dirección (contracts/rutas.md)
- [ ] T096 [US4] Implementar la lista de instituciones del propio usuario para el filtro en `src/MiArchivoMedico.Web/Servicios/BuscadorDeEstudios.cs` (RF-38)
- [ ] T097 [P] [US4] Crear la vista del listado con formulario de búsqueda, filtros, contador, paginación y los dos estados vacíos en `src/MiArchivoMedico.Web/Views/Estudios/Index.cshtml` (RF-23, RF-39)

**Punto de control**: las cuatro historias principales están completas. El producto cumple su objetivo
declarado.

---

## Fase 7: Historia 5 — Corregir los datos de un estudio (P3)

**Objetivo**: arreglar un metadato mal cargado sin tocar los archivos.

**Prueba independiente**: editar cada metadato de un estudio existente y verificar que el cambio
persiste y que la huella de sus archivos no cambió.

- [ ] T098 [P] [US5] Escribir los tests de edición de metadatos y de huella intacta en `tests/MiArchivoMedico.Tests/EdicionDeEstudioTests.cs` (AC-14)
- [ ] T099 [P] [US5] Escribir el test de edición de un estudio ajeno en `tests/MiArchivoMedico.Tests/PropiedadDeDatosTests.cs` (RNF-53)
- [ ] T100 [US5] Implementar la edición de metadatos en `src/MiArchivoMedico.Web/Controllers/EstudiosController.cs`, apoyándose en la interceptación para recalcular las columnas normalizadas (RF-10)
- [ ] T101 [P] [US5] Crear la vista de edición en `src/MiArchivoMedico.Web/Views/Estudios/Editar.cshtml`

**Punto de control**: la Historia 5 funciona sin haber tocado nada de las anteriores.

---

## Fase 8: Historia 6 — Tenerlo a mano en el teléfono (P3)

**Objetivo**: instalarla como aplicación y que sin señal muestre una pantalla honesta en lugar de datos
viejos.

**Prueba independiente**: verificar la instalación, la pantalla sin conexión, el aviso de carga
interrumpida y las acciones principales a 360 píxeles.

- [ ] T102 [P] [US6] Escribir el test que pide cada entrada de la lista `ESTATICOS` sin sesión en `tests/MiArchivoMedico.Tests/PwaTests.cs` (RNF-51, AC-41)
- [ ] T103 [P] [US6] Escribir el test de que la pantalla sin conexión es anónima y no contiene datos médicos en `tests/MiArchivoMedico.Tests/PwaTests.cs` (AC-40)
- [ ] T104 [P] [US6] Crear `manifest.webmanifest` con ícono propio y presentación en ventana propia en `src/MiArchivoMedico.Web/wwwroot/manifest.webmanifest` (RF-24)
- [ ] T105 [P] [US6] Crear los íconos de la aplicación en `src/MiArchivoMedico.Web/wwwroot/iconos/`
- [ ] T106 [US6] Escribir el service worker en `src/MiArchivoMedico.Web/wwwroot/sw.js` con una única regla: guarda solo la lista `ESTATICOS` y **nunca** escribe una respuesta de red (RNF-51)
- [ ] T107 [US6] Implementar `HomeController.SinConexion` con `[AllowAnonymous]` y su vista sin layout en `src/MiArchivoMedico.Web/Views/Home/SinConexion.cshtml` (RF-26)
- [ ] T108 [US6] Agregar al `carga.js` el aviso de carga interrumpida por pérdida de conexión, del lado del navegador, en `src/MiArchivoMedico.Web/wwwroot/js/carga.js` (RF-28, AC-42)

**Punto de control**: la aplicación se instala y se comporta bien sin conexión.

---

## Fase 9: Historia 7 — Operar sin filtrar ni perder datos (P3)

**Objetivo**: que el administrador técnico pueda operar el sistema sin encontrarse jamás con un dato
médico en un registro, y con respaldos que se puedan restaurar.

**Prueba independiente**: un ciclo de vida completo de un estudio ficticio con cadenas únicas, más un
ciclo de respaldo y una restauración en un entorno limpio.

- [ ] T109 [P] [US7] Escribir el test que crea, visualiza y elimina un estudio con cadenas únicas y busca esas cadenas en todo lo emitido por el canal de registro en `tests/MiArchivoMedico.Tests/PrivacidadEnLogsTests.cs` (AC-43)
- [ ] T110 [P] [US7] Escribir el test equivalente sobre el canal de métricas, incluidos nombres, etiquetas y valores, en `tests/MiArchivoMedico.Tests/PrivacidadEnLogsTests.cs` (AC-85)
- [ ] T111 [US7] Revisar todo el registro de la aplicación para que use solo identificadores técnicos, en `src/MiArchivoMedico.Web/` (RNF-09, RNF-43)
- [ ] T112 [P] [US7] Documentar el procedimiento de alta y de restablecimiento de contraseña por configuración externa en `docs/operacion.md` (RNF-54)
- [ ] T113 [P] [US7] Documentar el procedimiento de respaldo diario con `VACUUM INTO`, su retención de 30 días, su destino en cuenta separada y la coordinación con el respaldo de archivos en `docs/operacion.md` (RNF-34, RNF-35, RNF-59)
- [ ] T114 [P] [US7] Documentar el procedimiento de custodia de la clave de cifrado, separado del entorno y de los respaldos, en `docs/operacion.md` (RNF-58)
- [ ] T115 [P] [US7] Documentar el procedimiento de prueba de recuperación trimestral y su registro de evidencia en `docs/operacion.md` (RNF-37)

**Punto de control**: el sistema es operable y auditable por una persona sola.

---

## Fase 10: Cierre y aspectos transversales

**Propósito**: lo que no pertenece a una sola historia, incluidas las verificaciones que la suite no
puede cubrir.

- [ ] T116 Crear `specs/001-mvp-archivo-medico/verificacion-manual.md` con el procedimiento escrito de las 17 comprobaciones que no son alcanzables por un test de integración (research.md §6)
- [ ] T117 [P] Escribir en `specs/001-mvp-archivo-medico/verificacion-manual.md` el procedimiento de las 8 comprobaciones con navegador real: AC-26, AC-38, AC-39, AC-40, AC-41, AC-42, AC-78, AC-79
- [ ] T118 [P] Escribir en `specs/001-mvp-archivo-medico/verificacion-manual.md` el procedimiento de las 6 comprobaciones sobre la instalación desplegada: AC-56, AC-59, AC-60, AC-61, AC-67, AC-68
- [ ] T119 Crear el sembrador reproducible de 2.000 estudios ficticios en `tests/MiArchivoMedico.Tests/Apoyo/SembradorDeVolumen.cs` (RNF-24)
- [ ] T120 Crear las mediciones de rendimiento fuera de la suite habitual en `tests/MiArchivoMedico.Tests/Rendimiento/MedicionesTests.cs`, marcadas para ejecución a pedido (AC-51, AC-52, AC-53)
- [ ] T121 [P] Actualizar `AGENTS.md` y `CLAUDE.md` con la arquitectura real una vez construida, corrigiendo cualquier diferencia con lo que el árbol hace
- [ ] T122 Ejecutar la validación de punta a punta de [quickstart.md](./quickstart.md) sobre una instalación limpia
- [ ] T123 Recorrer `checklists/security.md`, `checklists/ux.md` y `checklists/testabilidad.md` y cerrar o justificar cada ítem abierto
- [ ] T124 Recorrer la definición de terminado de `specs/001-mvp-archivo-medico/quickstart.md` sobre el árbol completo: `dotnet build` sin warnings nuevos, `dotnet test` en verde, aislamiento verificado en todo acceso nuevo y ningún dato médico en registros

---

## Dependencias y orden de ejecución

### Dependencias entre fases

- **Preparación (Fase 1)**: sin dependencias, arranca de inmediato.
- **Fundaciones (Fase 2)**: depende de la Fase 1 y **bloquea todas las historias**. Es la fase que
  vuelve imposible olvidarse del aislamiento, así que no se saltea ni se hace a medias.
- **Historias (Fases 3 a 9)**: todas dependen de la Fase 2.
- **Cierre (Fase 10)**: depende de las historias que se decidan entregar.

### Dependencias entre historias

- **Historia 1 (P1)**: solo depende de las Fundaciones. Es el MVP.
- **Historia 2 (P1)**: solo depende de las Fundaciones. Se puede construir en paralelo con la 1.
- **Historia 3 (P2)**: necesita contenido cargado para demostrarse, así que conviene después de la 2,
  aunque sus tests pueden sembrar datos por contexto y correr antes.
- **Historia 4 (P2)**: igual que la 3; usa las columnas normalizadas creadas en Fundaciones.
- **Historia 5 (P3)**: independiente, se apoya en la interceptación de Fundaciones.
- **Historia 6 (P3)**: independiente de las demás; toca solo `wwwroot` y una vista anónima.
- **Historia 7 (P3)**: independiente; en su mayoría es revisión de registro y documentación operativa.

### Dentro de cada historia

Los tests se escriben **antes** de la implementación y deben fallar primero. Después: entidades,
servicios, controladores, vistas.

### Oportunidades de paralelismo

- Fase 1: T004 a T007 en paralelo.
- Fase 2: T009 a T015 en paralelo; T023 a T027 en paralelo una vez que existe el contexto.
- Todas las historias: los tests marcados [P] se escriben en paralelo, porque son archivos distintos.
- Historias 6 y 7 se pueden hacer en paralelo con cualquier otra: casi no comparten archivos.

---

## Ejemplo de paralelismo: Historia 1

```bash
# Los tests de la Historia 1, todos en archivos distintos:
T028 tests/MiArchivoMedico.Tests/AutenticacionTests.cs
T029 tests/MiArchivoMedico.Tests/SesionTests.cs
T030 tests/MiArchivoMedico.Tests/BloqueoDeIntentosTests.cs
T032 tests/MiArchivoMedico.Tests/PropiedadDeDatosTests.cs
T033 tests/MiArchivoMedico.Tests/RutasExpuestasTests.cs
T035 tests/MiArchivoMedico.Tests/SeguridadDeCredencialesTests.cs
T036 tests/MiArchivoMedico.Tests/ArranqueTests.cs
```

---

## Estrategia de implementación

### Primero el MVP: Historias 1 y 2

1. Fase 1: Preparación.
2. Fase 2: Fundaciones. **Crítica**: bloquea todo lo demás.
3. Fase 3: Historia 1. **Parar y validar**: el aislamiento funciona.
4. Fase 4: Historia 2. **Parar y validar**: se guarda un estudio y nada rechazado llega al disco.

A diferencia del caso habitual, el MVP defendible acá son **dos** historias, no una: la 1 sola no
guarda nada y la 2 sola no protege nada. Juntas ya son un producto que una familia puede usar.

### Entrega incremental

1. Preparación + Fundaciones → base lista.
2. Historia 1 → validar → demostrar.
3. Historia 2 → validar → demostrar (MVP entregable).
4. Historia 3 → el ciclo de vida completo.
5. Historia 4 → el objetivo declarado del producto.
6. Historias 5, 6 y 7 en cualquier orden, según convenga.
7. Fase 10 antes de poner el sistema en manos de la familia. Las 17 comprobaciones manuales no son
   opcionales: sin ellas hay criterios de aceptación del PRD que nadie verificó.
