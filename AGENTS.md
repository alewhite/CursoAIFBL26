# AGENTS.md — Mi Archivo Médico

## Propósito
Aplicación web progresiva (PWA) de uso familiar (hasta 5 cuentas) para cargar, organizar y encontrar estudios médicos.
Cada cuenta accede exclusivamente a sus propios estudios: comparten la instalación, nunca los datos.
La búsqueda es exclusivamente sobre metadatos cargados a mano: el sistema nunca lee el contenido interno de los archivos.

## Stack
- .NET 8 (LTS), C#
- ASP.NET Core MVC
- Entity Framework Core (SQL Server provider)
- Microsoft SQL Server
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
Antes de levantar por primera vez: definir la cadena de conexión a SQL Server y la ruta de almacenamiento de archivos
por user-secrets (`dotnet user-secrets set`) o variables de entorno, nunca en `appsettings.json` versionado.

Nueva migración: `dotnet ef migrations add <Nombre>`.

## Qué NO hacer
- **No escribir datos médicos en logs, métricas ni mensajes de error.** Nada de títulos, descripciones, profesionales,
  instituciones, etiquetas ni nombres originales de archivo. Usar solo identificadores técnicos internos (RNF-09, RNF-43).
- **No incorporar APIs de IA, OCR ni motores de búsqueda administrados.** La búsqueda se resuelve con las capacidades
  de SQL Server vía EF Core. Tampoco usar contenido médico para entrenar modelos (RNF-44, RNF-46 a RNF-49).
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
