# AGENTS.md — Mi Archivo Médico

`PRD.md` es la fuente única del alcance y de los requerimientos (RF/RNF/AC). Este archivo define **cómo** se
construye; el PRD define **qué**. Ante una contradicción, prevalece el PRD: corregir este archivo, no el PRD.

## Propósito
Aplicación web progresiva (PWA) de uso familiar (hasta 5 cuentas) para cargar, organizar y encontrar estudios médicos.
Cada cuenta accede exclusivamente a sus propios estudios: comparten la instalación, nunca los datos.
La búsqueda es exclusivamente sobre metadatos cargados a mano: el sistema nunca lee el contenido interno de los archivos.

## Stack
- .NET 8 (LTS), C#
- ASP.NET Core MVC
- Entity Framework Core (provider `Microsoft.EntityFrameworkCore.Sqlite`)
- SQLite: un único archivo en disco del servidor, en modo WAL, ubicado fuera de toda carpeta pública
- ASP.NET Core Identity (autenticación, hashing de contraseñas)
- NuGet como gestor de paquetes
- Archivos subidos: carpeta en disco del propio servidor (IIS / inetpub), servida solo a través de un controlador con autorización

## Cómo correr
```bash
dotnet restore                  # instalar dependencias
dotnet ef database update       # aplicar migraciones (requiere dotnet-ef: dotnet tool install --global dotnet-ef)
dotnet run                      # levantar en desarrollo (URL en Properties/launchSettings.json)
dotnet test                     # correr tests
dotnet build                    # verificación de compilación antes de dar algo por terminado
```
Antes de levantar por primera vez: definir la cadena de conexión a SQLite (`Data Source=<ruta>/archivo-medico.db`) y la ruta de almacenamiento de archivos
por user-secrets (`dotnet user-secrets set`) o variables de entorno, nunca en `appsettings.json` versionado.

Claves de configuración externa (todas por user-secrets o variables de entorno):

| Clave | Para qué |
|---|---|
| `ConnectionStrings:ArchivoMedico` | Base SQLite. Sin ella la aplicación falla al arrancar. |
| `CuentasIniciales:<n>:NombreDeUsuario` / `:Email` / `:Contrasena` | Alta administrativa de cuentas, máximo 5 (RNF-54, RNF-56). Se aplica en cada arranque y omite las que ya existen. |

El alta y el restablecimiento de contraseñas se hacen por esta vía: la aplicación no expone ninguna ruta de
registro ni de recuperación.

Nueva migración: `dotnet ef migrations add <Nombre>`.

## Definición de terminado
Una tarea no está terminada hasta que:
1. `dotnet build` compila sin errores ni warnings nuevos.
2. `dotnet test` pasa completo.
3. Toda query nueva sobre datos médicos filtra por el propietario autenticado, y existe un test que verifica
   que un identificador ajeno responde 403 o 404 (ver AC-47 a AC-49).
4. Ningún log, mensaje de error ni excepción nueva contiene metadatos médicos.
5. El cambio referencia el RF/RNF/AC del PRD que satisface. Si no hay ninguno, el cambio está fuera de alcance:
   preguntar antes de implementar, no ampliar el PRD por cuenta propia.

## Datos de prueba
Nunca usar estudios, nombres, instituciones ni archivos reales en desarrollo, tests o fixtures: solo datos
ficticios (RNF-10). Los archivos de prueba se generan; no se copian de un caso real.

## Puntos de atención al implementar
- **Aislamiento por propietario**: preferir un filtro global de EF Core por `OwnerId` sobre repetir el `Where`
  en cada consulta, para que olvidarlo no sea posible. Un `Find`/`FindAsync` por clave primaria **no** aplica
  filtros globales: nunca usarlo para cargar datos médicos.
- **Hashing de contraseñas**: el `PasswordHasher<T>` de Identity deriva con PBKDF2-**HMAC-SHA512**, que no es
  ninguna de las tres combinaciones que admite RNF-03. Por eso el hasher está reemplazado por
  `HasherPbkdf2Sha256` (PBKDF2-HMAC-SHA256, formato Identity V3). `PasswordHasherOptions.IterationCount`
  sigue siendo la fuente de las iteraciones y debe ser ≥ 100.000: el hasher falla al construirse si no lo es.
- **Archivos**: nombre físico GUID, nunca derivado del nombre original (RNF-22); el nombre original se sanitiza
  antes de guardarlo como metadato (RNF-23). Validar extensión + MIME + firma binaria antes de mover el archivo
  al almacenamiento definitivo, y borrar los rechazados (RNF-15 a RNF-17, RNF-21).
- **Cifrado en reposo**: la clave se resuelve desde configuración externa; si falta, la aplicación debe fallar al
  arrancar en vez de guardar archivos en claro (RNF-02, RNF-58).

## Qué NO hacer
- **No escribir datos médicos en logs, métricas ni mensajes de error.** Nada de títulos, descripciones, profesionales,
  instituciones, etiquetas ni nombres originales de archivo. Usar solo identificadores técnicos internos (RNF-09, RNF-43).
- **No incorporar APIs de IA, OCR ni motores de búsqueda administrados.** La búsqueda se resuelve con las capacidades
  de SQLite vía EF Core. Tampoco usar contenido médico para entrenar modelos (RNF-44, RNF-46 a RNF-49).
- **No asumir intercalaciones insensibles a acentos: SQLite no las tiene.** Las búsquedas de texto libre consultan
  columnas normalizadas (minúsculas, sin acentos, sin espacios sobrantes) que la aplicación calcula y persiste al
  guardar, y el término ingresado se normaliza con la misma función (RNF-55, AC-45, AC-46). `LIKE`/`EqualsIgnoreCase`
  sobre las columnas *originales* no cumple el requisito; sobre las *normalizadas*, `LIKE '%término%'` alcanza y sobra
  para el volumen del MVP (hasta 2.000 estudios, RNF-24). No agregar FTS5 ni ningún índice de texto completo.
- **No versionar ni exponer el archivo `.db`.** El archivo de base de datos y sus auxiliares (`-wal`, `-shm`) viven
  fuera de cualquier carpeta servida y fuera del repositorio. Respaldarlo con `VACUUM INTO` o la API de backup en
  línea, nunca copiando el archivo en caliente (RNF-34, RNF-35).
- **No agregar funciones fuera del alcance del MVP**: OCR, exportar/importar/restaurar desde la UI, enlaces públicos
  para compartir, envío por mail o WhatsApp, roles y permisos configurables, versionado, notificaciones
  (ver "Fuera de Alcance" en el PRD, que es la fuente única).
- **No agregar nada que cruce datos entre cuentas**: ni compartir/delegar/transferir estudios, ni vistas consolidadas
  del grupo familiar, ni registro abierto de usuarios. El alta de cuentas es administrativa, fuera de la app,
  con un máximo de 5 (RNF-54, RNF-56, RNF-57).
- **Nunca consultar estudios ni archivos sin filtrar por el propietario autenticado.** Toda query de EF Core sobre
  datos médicos filtra por el `OwnerId` del usuario de la sesión; un recurso de otra cuenta responde 403 o 404 aunque
  se conozca su identificador. Es el requisito crítico del sistema (RNF-53, RNF-08, AC-47 a AC-49).
- **No exponer archivos por URL pública ni permanente.** Todo acceso a un archivo pasa por autenticación y autorización;
  las URLs temporales expiran en 5 minutos como máximo. Secretos y cadenas de conexión, fuera del código fuente
  (RNF-06 a RNF-08, sección 11).
