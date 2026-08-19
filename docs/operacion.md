# Operación de Mi Archivo Médico

Procedimientos del administrador técnico, que en el MVP es uno de los integrantes del grupo familiar.
Nada de lo que está acá se hace desde la aplicación: son todos procedimientos externos, y esa es una
decisión de alcance del PRD, no una omisión (RNF-38, RNF-39, RNF-54).

## 1. Alta de cuentas y restablecimiento de contraseñas (RNF-54, RNF-56, RNF-69)

La aplicación **no expone ninguna ruta** de registro, cambio ni recuperación de contraseña. Las cuentas
se dan de alta por configuración externa y se aplican en cada arranque, omitiendo las que ya existen.

En desarrollo, con user-secrets:

```bash
dotnet user-secrets set "CuentasIniciales:0:NombreDeUsuario" "nombre.integrante" --project src/MiArchivoMedico.Web
dotnet user-secrets set "CuentasIniciales:0:Email" "integrante@ejemplo.invalido" --project src/MiArchivoMedico.Web
dotnet user-secrets set "CuentasIniciales:0:Contrasena" "una-contrasena-de-doce-o-mas" --project src/MiArchivoMedico.Web
```

En el servidor, con variables de entorno:

```bash
export CuentasIniciales__0__NombreDeUsuario="nombre.integrante"
export CuentasIniciales__0__Email="integrante@ejemplo.invalido"
export CuentasIniciales__0__Contrasena="una-contrasena-de-doce-o-mas"
```

**Reglas que el arranque hace cumplir por sí solo:**

- Máximo **5 cuentas activas**. El alta de una sexta detiene el arranque con un error explícito.
- La contraseña tiene **12 caracteres como mínimo**, sin reglas de composición. Una más corta detiene
  el arranque.
- Una cuenta que ya existe se omite: volver a arrancar no la pisa ni la duplica.

**Para restablecer una contraseña olvidada** no hay flujo en la aplicación. El procedimiento es borrar
la fila de esa cuenta en la base y volver a sembrarla con la contraseña nueva, o actualizar el hash a
mano con la aplicación detenida. Perder la contraseña **no** implica perder los estudios: siguen
asociados al identificador de la cuenta.

## 2. Custodia de la clave de cifrado (RNF-58, RNF-62)

La clave AES-256 se pasa por `Almacenamiento:ClaveBase64`, en base64, y decodifica a exactamente 32
bytes. **Sin ella la aplicación no arranca**, en lugar de guardar archivos en claro.

Generarla una sola vez:

```bash
head -c 32 /dev/urandom | base64
```

**Dónde vive y dónde no:**

- **Sí**: en un gestor de contraseñas personal, o en una anotación en papel guardada fuera del lugar
  donde vive el servidor.
- **No**: en el repositorio, en `appsettings.json`, en el mismo disco que la base, ni **dentro de los
  respaldos**. Que un respaldo por sí solo no permita descifrar los archivos es justamente el punto.

**Si se pierde la clave, los archivos son irrecuperables.** Los metadatos sobreviven —títulos, fechas,
profesionales— pero ningún documento vuelve a abrirse. No hay puerta trasera y no debe haberla.

**La rotación de la clave está fuera del alcance del MVP** (PRD2.md, Fuera de Alcance). Cambiarla exige
descifrar y volver a cifrar todos los archivos existentes con la aplicación detenida. Ante sospecha de
compromiso, ese es el procedimiento manual, y hasta ejecutarlo la clave anterior sigue siendo la única
que abre lo ya guardado.

## 3. Respaldo diario (RNF-34, RNF-35, RNF-59)

Hay que respaldar **dos cosas al mismo punto en el tiempo**: la base de datos y la carpeta de archivos
cifrados. Si se desincronizan, una restauración deja estudios sin archivo o archivos huérfanos.

**La base nunca se copia en caliente con `cp`.** SQLite en modo WAL tiene escrituras en vuelo y la copia
saldría inconsistente. Se usa `VACUUM INTO`, que produce un archivo íntegro sin detener la aplicación:

```bash
FECHA=$(date +%Y-%m-%d)
sqlite3 /ruta/a/archivo-medico.db "VACUUM INTO '/respaldos/base-$FECHA.db'"
tar -czf "/respaldos/archivos-$FECHA.tar.gz" -C /ruta/al/almacenamiento .
```

Los dos comandos van **seguidos y en ese orden**, sin operaciones de por medio: primero la base, después
los archivos. Un archivo que se cargue entre ambos queda en el respaldo de archivos sin fila en la base
—se detecta y se descarta al restaurar—, mientras que el orden inverso produciría una fila apuntando a
un archivo que el respaldo no tiene.

**Retención y destino:**

- Un respaldo **por día**, conservado **30 días como mínimo**.
- En una **cuenta o suscripción distinta** de la del entorno principal, de modo que una credencial
  comprometida del servidor no permita alterarlos ni borrarlos.
- **Nunca** en el mismo disco ni en la misma carpeta que la base o el almacenamiento.
- **Sin la clave de cifrado adentro.**

## 4. Prueba de recuperación trimestral (RNF-37)

**Al menos una vez cada tres meses**, y siempre sobre un entorno limpio, no sobre el que está en uso.

1. Levantar una instalación nueva, vacía, con su propia carpeta de almacenamiento.
2. Restaurar el par base + archivos del **mismo día**.
3. Configurar la clave de cifrado desde su custodia.
4. Entrar con una cuenta y comprobar, en este orden:
   - El listado muestra los estudios esperados.
   - Un estudio que tenía tres archivos sigue teniendo los tres vinculados (AC-61).
   - Ningún estudio referencia un archivo inexistente y no hay archivos sin estudio (AC-67).
   - Un archivo se abre y se descarga, y su huella coincide con la registrada.
5. Comprobar que **sin la clave** los archivos no se descifran (AC-68): es la mitad del respaldo que
   suele no probarse nunca.

**Registro de evidencia.** Cada prueba se anota acá abajo, con fecha, quién la hizo, qué respaldo se
restauró y el resultado. Un requisito verificado por inspección solo se puede dar por cumplido si queda
constancia.

| Fecha | Responsable | Respaldo restaurado | Resultado |
|---|---|---|---|
| _(pendiente: la primera prueba se hace al desplegar)_ | | | |

## 5. Constancia de ausencia de servicios externos (RNF-40 a RNF-44, RNF-50)

Verificaciones por inspección, a repetir cada vez que se agregue una dependencia:

| Qué se comprueba | Cómo | Estado |
|---|---|---|
| Sin publicidad ni seguimiento de comportamiento | `dotnet list package` y revisión de las vistas: no hay etiquetas de terceros ni scripts externos | ✔ al 2026-08-19 |
| Sin envío de datos a terceros | La aplicación no hace ninguna solicitud saliente; el único servicio externo es el propio hosting | ✔ al 2026-08-19 |
| Sin uso de contenido médico para entrenar modelos | No hay integración con ninguna API de inteligencia artificial | ✔ al 2026-08-19 |
| Sin dependencia de IA, OCR ni motores de búsqueda administrados | La búsqueda se resuelve con SQLite vía EF Core, sobre columnas normalizadas | ✔ al 2026-08-19 |
| Todo servicio externo pago se puede deshabilitar por configuración | El MVP **no integra ninguno**, así que la condición se cumple por vacío | ✔ al 2026-08-19 |

Dependencias del proyecto al día de hoy: EF Core con proveedor SQLite, ASP.NET Core Identity e
ImageSharp 3.1.12 (Apache-2.0). Ninguna implica un servicio de terceros en tiempo de ejecución.

## 6. Qué mirar cuando algo falla

Los registros técnicos **no contienen ningún dato médico**, y eso es deliberado (RNF-09, RNF-43): no hay
títulos, profesionales, instituciones, descripciones, etiquetas ni nombres de archivo. Solo
identificadores técnicos.

Eso significa que para diagnosticar un problema **no se puede buscar por el título de un estudio**. Se
busca por el identificador de la cuenta, que sí aparece, o se reproduce con datos ficticios. Es más
incómodo, y es el precio correcto.

**Si la aplicación no arranca**, el mensaje dice cuál de estas tres falta:

| Clave | Síntoma |
|---|---|
| `ConnectionStrings:ArchivoMedico` | «Falta ConnectionStrings:ArchivoMedico» |
| `Almacenamiento:Ruta` | «Falta Almacenamiento:Ruta» |
| `Almacenamiento:ClaveBase64` | «Falta Almacenamiento:ClaveBase64» o «decodifica a N bytes y AES-256 exige exactamente 32» |

Que la aplicación se niegue a arrancar es el comportamiento correcto: arrancar sin clave significaría
guardar archivos médicos en claro.
