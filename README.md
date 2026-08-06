# Mi Archivo Médico

Aplicación web progresiva (PWA) de uso familiar —hasta 5 cuentas— para cargar, organizar y encontrar
estudios médicos.

Cada cuenta accede **exclusivamente a sus propios estudios**: comparten la instalación, nunca los datos.
La búsqueda es sobre metadatos cargados a mano; el sistema no lee el contenido interno de los archivos.

## Estado

MVP en construcción. Lo que ya está implementado:

| Etapa | Alcance | Estado |
|---|---|---|
| Andamiaje | Proyecto MVC, EF Core sobre SQLite en modo WAL, proyecto de tests | ✅ |
| Feature 1 | Autenticación, sesiones, bloqueo por intentos fallidos, aislamiento por propietario | ✅ |
| Feature 2 | Estudios y archivos: carga, validación, cifrado, visualización, descarga, borrado | ✅ |
| Feature 3 | Listado, búsqueda por metadatos y filtros combinables | ⏳ |
| Pendiente | PWA (instalación, pantalla offline) y respaldos de infraestructura | ⏳ |

## Documentos del repositorio

- **[`PRD2.md`](PRD2.md)** — fuente única del alcance y de los requerimientos (RF/RNF/AC). Define **qué** se
  construye. Ante cualquier contradicción, prevalece.
- **[`AGENTS.md`](AGENTS.md)** — define **cómo** se construye: stack, comandos, definición de terminado y
  puntos de atención al implementar.
- `PRD.md` — revisión 1, conservada como referencia histórica.

## Stack

- .NET 8 (LTS), C#, ASP.NET Core MVC
- Entity Framework Core con `Microsoft.EntityFrameworkCore.Sqlite`
- SQLite: un único archivo en disco del servidor, en modo WAL, fuera de toda carpeta pública
- ASP.NET Core Identity para autenticación
- Sin servicios administrados, sin APIs de IA y sin OCR (RNF-45 a RNF-49)

## Puesta en marcha

Requisitos: SDK de .NET 8 (el repo lo fija en `global.json`) y `dotnet-ef`
(`dotnet tool install --global dotnet-ef`).

```bash
dotnet restore
```

### Configuración

Ningún secreto vive en `appsettings.json` ni en el código. Todo se resuelve por user-secrets o variables de
entorno:

| Clave | Para qué |
|---|---|
| `ConnectionStrings:ArchivoMedico` | Base SQLite. **Sin ella la aplicación falla al arrancar.** |
| `CuentasIniciales:<n>:NombreDeUsuario` · `:Email` · `:Contrasena` | Alta administrativa de cuentas, máximo 5. Se aplica en cada arranque y omite las que ya existen. |
| `Almacenamiento:Ruta` | Carpeta de archivos cifrados, fuera de toda carpeta pública. |
| `Almacenamiento:ClaveBase64` | Clave AES-256 (32 bytes en base64). **Sin ella la aplicación falla al arrancar.** |
| `Almacenamiento:CupoTotalEnBytes` | Cupo compartido de almacenamiento; 20 GB por omisión. |

```bash
cd src/MiArchivoMedico.Web
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:ArchivoMedico" "Data Source=/ruta/privada/archivo-medico.db"
dotnet user-secrets set "CuentasIniciales:0:NombreDeUsuario" "paciente.uno"
dotnet user-secrets set "CuentasIniciales:0:Email"           "paciente.uno@ejemplo.invalid"
dotnet user-secrets set "CuentasIniciales:0:Contrasena"      "<contraseña de al menos 12 caracteres>"
dotnet user-secrets set "Almacenamiento:Ruta"        "/ruta/privada/archivos"
dotnet user-secrets set "Almacenamiento:ClaveBase64" "$(openssl rand -base64 32)"
```

La ruta de la base debe quedar **fuera del repositorio y fuera de toda carpeta servida por el servidor web**,
junto con sus archivos auxiliares `-wal` y `-shm`.

### Correr

```bash
dotnet run --project src/MiArchivoMedico.Web   # https://localhost:7028
dotnet test                                    # suite completa
dotnet build                                   # debe compilar sin warnings nuevos
```

Las migraciones se aplican solas al arrancar. Para crear una nueva:

```bash
dotnet ef migrations add <Nombre> --project src/MiArchivoMedico.Web --output-dir Data/Migraciones
```

## Estructura

```
src/MiArchivoMedico.Web/
  Controllers/          Cuenta (login), Estudios (CRUD), Archivos (ver, descargar)
  Data/                 DbContext, entidades, migraciones, inicialización y siembra
  Archivos/             validación, sanitización de nombres, almacenamiento cifrado
  Dominio/              normalización de texto para búsqueda
  Security/             sesión, hashing, usuario actual, alta de cuentas
  Models/ · Views/      formularios y vistas
tests/MiArchivoMedico.Tests/
  Infraestructura/      aplicación en memoria con SQLite descartable y reloj controlable
  *Tests.cs             un archivo por área, con los AC del PRD en el nombre de cada test
```

## Decisiones de seguridad que conviene conocer

Cada una responde a un requerimiento del PRD, y cada una tiene test:

- **Aislamiento por propietario (RNF-53).** Toda entidad que implemente `IPropiedadDeUsuario` recibe
  automáticamente un filtro global de EF Core por `OwnerId`, para que olvidar el `Where` no sea posible.
  Un `Find`/`FindAsync` por clave primaria **no** aplica filtros globales: nunca usarlos para datos médicos.
- **Autorización por omisión.** La política global exige sesión: una pantalla nueva nace protegida salvo que
  se la marque explícitamente con `[AllowAnonymous]`.
- **Sesiones (RNF-04, RNF-05).** Cookie `Secure` + `HttpOnly` + `SameSite=Strict`, 30 minutos deslizantes y
  un tope absoluto de 24 horas guardado en las `AuthenticationProperties`, que sobreviven al refresco del
  sello de seguridad.
- **Bloqueo por fuerza bruta (RNF-60).** 5 intentos fallidos dentro de una ventana real de 15 minutos.
  El mensaje es único e indistinguible para usuario inexistente, contraseña incorrecta y cuenta bloqueada.
- **Hashing (RNF-03).** El `PasswordHasher<T>` de Identity deriva con PBKDF2-**HMAC-SHA512**, que no es
  ninguna de las tres combinaciones que admite el PRD. Está reemplazado por `HasherPbkdf2Sha256`
  (PBKDF2-HMAC-SHA256, formato Identity V3), que falla al construirse con menos de 100.000 iteraciones.
- **Sin registro ni recuperación de contraseña (RNF-54).** No existe ninguna ruta que dé de alta una cuenta.
  El alta y el restablecimiento son administrativos, por configuración externa.
- **Cifrado en reposo (RNF-02).** Cada archivo se guarda cifrado con AES-256-CBC bajo un nombre físico que
  es solo un GUID, sin ninguna porción del nombre original. La clave se custodia fuera del entorno y fuera
  de los respaldos: un respaldo sin ella no permite descifrar nada.
- **Validación de lo que se sube (RNF-15 a RNF-17).** Extensión, tipo MIME y firma binaria deben coincidir,
  y el formato se valida estructuralmente: los PDF por su marca `%%EOF`, las imágenes decodificándolas por
  completo. Todo se valida en un área de tránsito; al almacenamiento definitivo solo llega lo aceptado.
- **Entrega de archivos (RNF-06, RNF-20).** No existe ninguna URL pública ni permanente: cada solicitud pasa
  por autenticación y por el filtro de propietario. El contenido se sirve con una CSP `sandbox` y se incrusta
  en un iframe con `sandbox`, de modo que un PDF con JavaScript embebido no lo ejecute.
- **Nada de datos médicos en logs (RNF-09, RNF-43).** Ni títulos, descripciones, profesionales,
  instituciones, etiquetas ni nombres originales de archivo. Solo identificadores técnicos.

## Datos de prueba

Nunca se usan estudios, nombres, instituciones ni archivos reales en desarrollo, tests o fixtures: solo datos
ficticios (RNF-10). Los archivos de prueba se generan; no se copian de un caso real.

## Fuera de alcance

El MVP no incluye OCR, búsqueda dentro del contenido de los archivos, exportar/importar/restaurar desde la
interfaz, enlaces públicos, envío por mail o WhatsApp, roles configurables, versionado, notificaciones, ni
nada que cruce datos entre cuentas. La lista completa está en la sección
[Fuera de Alcance](PRD2.md#fuera-de-alcance) del PRD, que es la fuente única.
