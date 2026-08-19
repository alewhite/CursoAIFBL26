# Fase 1 — Guía de Puesta en Marcha y Validación

**Feature**: MVP Mi Archivo Médico · **Fecha**: 2026-08-19 · **Plan**: [plan.md](./plan.md)

Cómo levantar la aplicación, configurarla y comprobar de punta a punta que hace lo que el spec dice.
Los detalles de modelo están en [data-model.md](./data-model.md) y los de rutas en
[contracts/rutas.md](./contracts/rutas.md).

## Requisitos previos

- SDK de .NET 8.
- Opcional, solo para inspeccionar la base a mano: `dotnet tool install --global dotnet-ef`.
- No hace falta ningún servicio externo, contenedor ni motor de base de datos.

## Configuración obligatoria antes del primer arranque

Ninguno de estos valores va en un archivo versionado. Se cargan por `dotnet user-secrets` en desarrollo
o por variables de entorno en el servidor.

| Clave | Para qué | Si falta |
|---|---|---|
| `ConnectionStrings:ArchivoMedico` | Base SQLite: `Data Source=<ruta>/archivo-medico.db`, fuera de toda carpeta pública. | La aplicación no arranca. |
| `Almacenamiento:Ruta` | Carpeta de archivos cifrados, fuera de toda carpeta pública. | La aplicación no arranca. |
| `Almacenamiento:ClaveBase64` | Clave AES-256: 32 bytes en base64. | **La aplicación no arranca** (RNF-62, AC-83). Nunca guarda archivos en claro. |
| `Almacenamiento:CupoTotalEnBytes` | Cupo compartido. Por omisión 20 GB. | Usa el valor por omisión. |
| `CuentasIniciales:<n>:NombreDeUsuario` / `:Email` / `:Contrasena` | Alta administrativa de cuentas, máximo 5. Se aplica en cada arranque y omite las que ya existen. | No se crea ninguna cuenta y no se puede entrar. |

Generar una clave de prueba:

```bash
dotnet user-secrets set "Almacenamiento:ClaveBase64" "$(head -c 32 /dev/urandom | base64)" \
  --project src/MiArchivoMedico.Web
```

## Levantar

```bash
dotnet restore
dotnet build                                    # sin errores ni warnings nuevos
dotnet run --project src/MiArchivoMedico.Web
```

Las migraciones se aplican solas al arrancar, junto con el modo WAL de la base y la siembra de las
cuentas configuradas. `dotnet ef database update` a mano solo hace falta para inspeccionar la base.

## Correr la suite

```bash
dotnet test                                              # todo
dotnet test --filter "FullyQualifiedName~AislamientoPorPropietarioTests"
dotnet test --filter "DisplayName~RNF-53"                # por requerimiento
dotnet test --filter "DisplayName~AC-47"                 # por criterio de aceptación
```

Cada test lleva en su `DisplayName` el identificador que verifica, así que el filtro por requerimiento
funciona para cualquier RF, RNF o AC de `PRD2.md`.

## Validación de punta a punta

Cada bloque es una historia de usuario del spec y se puede validar por separado.

### 1. Acceso privado y aislado (P1)

1. Sin haber iniciado sesión, pedir `/Estudios`: redirige al ingreso y no muestra nada médico.
2. Entrar con una cuenta sembrada: llega al listado.
3. Probar credenciales inválidas y un usuario inexistente: el mensaje y la demora son iguales.
4. Fallar cinco veces seguidas y probar con la contraseña correcta: rechaza.
5. Con sesión de una cuenta, pedir el detalle de un estudio de la otra: **404**, igual que un
   identificador inventado.

**Queda validado**: AC-01 a AC-05, AC-47 a AC-49, AC-98.

### 2. Guardar un estudio con sus archivos (P1)

1. Crear un estudio con título, fecha de hoy y tres archivos: un PDF válido, un JPG válido y un
   ejecutable renombrado a `.pdf`.
2. Resultado esperado: el estudio queda creado con los dos archivos válidos, el tercero se rechaza con
   su motivo junto a ese archivo, y el rechazado no existe en la carpeta de almacenamiento.
3. Probar una fecha de mañana: se rechaza.
4. Mirar la carpeta de almacenamiento: los nombres son GUID y los bytes no son los del original.

**Queda validado**: AC-09 a AC-12, AC-20 a AC-22, AC-27, AC-57, AC-65, AC-90, AC-91.

### 3. Consultar, descargar y eliminar (P2)

1. Abrir el detalle: se ven los archivos listados, sin que se transfiera su contenido.
2. Visualizar un archivo: se muestra incrustado, sin descargarlo.
3. Descargarlo y comparar su huella con la registrada: idénticas.
4. Eliminar el estudio: pide confirmación, cancelar no hace nada, confirmar lo borra junto con sus
   archivos y libera el cupo.

**Queda validado**: AC-15 a AC-19, AC-77, AC-102.

### 4. Encontrar un estudio en segundos (P2)

1. Con un estudio cuya institución es "Hospital Central", buscar `" hospital central "` en minúsculas
   y con espacios sobrantes: aparece.
2. Con uno etiquetado "cardiología", buscar `"cardiologia"` sin acento: aparece.
3. **Mirar la dirección del navegador**: no contiene el término buscado.
4. Filtrar por institución: la lista solo ofrece las instituciones de los estudios propios.
5. Avanzar de página, abrir un estudio y volver: la búsqueda y los filtros siguen aplicados.
6. Buscar algo que no existe: informa cero resultados y ofrece limpiar los filtros.

**Queda validado**: AC-29 a AC-31, AC-34 a AC-37, AC-45, AC-46, AC-92 a AC-96, AC-101.

### 5. Corregir los datos de un estudio (P3)

Editar la institución y comprobar que el metadato cambia y la huella de los archivos no.
**Queda validado**: AC-14.

### 6. Tenerlo a mano en el teléfono (P3)

Instalar la aplicación desde el navegador, cortar la conexión y navegar: aparece la pantalla sin
conexión, sin datos médicos. Ejecutar las diez acciones principales en una ventana de 360 píxeles de
ancho sin desplazamiento horizontal.
**Queda validado**: AC-38 a AC-42, todos por comprobación manual.

### 7. Operar sin filtrar ni perder datos (P3)

Crear, visualizar y eliminar un estudio cuyos metadatos sean cadenas únicas e irrepetibles, y buscar
esas cadenas en la totalidad de los registros y métricas emitidos: cero coincidencias.
**Queda validado**: AC-43, AC-85.

## Comprobaciones que no cubre la suite

17 criterios no son alcanzables por un test de integración y se comprueban de otra forma. La
clasificación completa está en [research.md](./research.md) §6. En resumen:

- **8 con navegador real**: AC-26, AC-38, AC-39, AC-40, AC-41, AC-42, AC-78, AC-79.
- **6 sobre la instalación desplegada**: AC-56, AC-59, AC-60, AC-61, AC-67, AC-68.
- **3 de medición de rendimiento**: AC-51, AC-52, AC-53, sobre una colección sembrada de 2.000
  estudios y, para AC-53, con el ancho de banda limitado a 10 Mbps.

Ninguno es opcional: se ejecutan con procedimiento escrito antes de la entrega.

## Definición de terminado

Una tarea no está terminada hasta que `dotnet build` compila sin errores ni warnings nuevos,
`dotnet test` pasa completo, los criterios afectados están cubiertos, se verificó el aislamiento para
todo acceso nuevo a datos médicos, no se introdujeron datos médicos en registros ni mensajes de error,
y el cambio cita el RF/RNF/AC de `PRD2.md` que satisface.
