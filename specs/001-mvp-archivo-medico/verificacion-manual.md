# Verificación Manual

**Feature**: MVP Mi Archivo Médico · **Creado**: 2026-08-19 · **Plan**: [plan.md](./plan.md)

Diecisiete de los 99 criterios de aceptación vigentes **no son alcanzables por un test de integración**.
La clasificación completa está en [research.md](./research.md) §6. Que no se puedan automatizar no los
vuelve opcionales: se ejecutan con este procedimiento antes de entregar, y el resultado se anota en la
planilla del final.

Los otros 82 los cubre `dotnet test`, que corre en segundos y no necesita a nadie mirando.

## Antes de empezar

- Una instalación desplegada con HTTPS real y certificado válido, no `localhost`.
- Dos cuentas sembradas, con al menos un estudio cada una y algún archivo cargado.
- Un teléfono o el navegador en modo dispositivo, a **360 píxeles** de ancho.
- Las herramientas de desarrollo del navegador abiertas en la pestaña de red.

---

## A · Ocho comprobaciones con navegador real

### AC-38 — Instalación como aplicación

1. Abrir la aplicación en un navegador compatible y aceptar la instalación que ofrezca.
2. Cerrar el navegador y abrir la aplicación desde el ícono que quedó en el sistema.

**Pasa si**: el sistema operativo registra un ícono propio y la aplicación abre en una ventana **sin la
barra de direcciones** del navegador.

### AC-39 — Diez acciones sin desplazamiento horizontal, a 360 píxeles

Con la ventana a 360 píxeles de ancho, ejecutar las diez acciones principales de RNF-30 y comprobar en
cada una que **no aparece barra de desplazamiento horizontal en el cuerpo de la página**:

| # | Acción | Sin scroll horizontal |
|---|---|---|
| 1 | Iniciar sesión | ☐ |
| 2 | Crear un estudio | ☐ |
| 3 | Cargar archivos | ☐ |
| 4 | Buscar | ☐ |
| 5 | Aplicar filtros | ☐ |
| 6 | Limpiar filtros | ☐ |
| 7 | Abrir el detalle de un estudio | ☐ |
| 8 | Visualizar un archivo | ☐ |
| 9 | Descargar un archivo | ☐ |
| 10 | Eliminar un estudio | ☐ |

La tabla del listado puede desplazarse **dentro de su propio contenedor** —tiene `overflow-x: auto`—;
lo que no debe desplazarse es la página.

### AC-40 — Pantalla sin conexión

1. Con sesión iniciada, cortar la conexión (modo avión, o «Offline» en las herramientas del navegador).
2. Navegar a cualquier pantalla de la aplicación.

**Pasa si**: aparece la pantalla que dice explícitamente que no hay conexión, y **no contiene ningún
estudio, metadato ni archivo**.

### AC-41 — Sin sesión y sin conexión, no se ve nada guardado antes

1. Con sesión iniciada, recorrer el listado y abrir un par de estudios, para que el navegador tenga
   oportunidad de guardar algo.
2. Cerrar sesión.
3. Cortar la conexión.
4. Abrir la aplicación instalada.

**Pasa si**: no se muestra ningún estudio ni metadato médico visto antes. Solo la pantalla sin conexión.

> Este es el criterio que más fácil se rompe al «mejorar» la experiencia sin conexión. La suite cubre
> la mitad automatizable: que cada entrada de la lista `ESTATICOS` responda sin sesión.

### AC-42 — Aviso de carga interrumpida

1. Empezar a cargar un estudio con un archivo grande.
2. Cortar la conexión **mientras la carga está en curso**.

**Pasa si**: la aplicación informa que el archivo **no** fue cargado.

> El aviso es del navegador, no del servidor: con la conexión caída el servidor no llega a responder
> nada. Implementarlo del lado del servidor no funciona.

### AC-26 — Un PDF con JavaScript no lo ejecuta

1. Cargar el PDF con JavaScript embebido que genera `ArchivosFicticios.PdfConJavaScript()`.
2. Visualizarlo desde la aplicación.

**Pasa si**: no aparece ningún diálogo ni se ejecuta nada, y la consola del navegador no muestra
actividad del script.

> La suite ya verifica la mitad observable desde el servidor: que la respuesta lleve `nosniff` y una
> política que bloquea todo contenido activo, y que la vista use un marco restringido. Lo que necesita
> un navegador es comprobar que el script efectivamente no corre.

### AC-78 — No se transfiere contenido hasta pedirlo

1. Abrir las herramientas del navegador en la pestaña de red y limpiarla.
2. Abrir el listado y después el detalle de un estudio con archivos, **sin** tocar «Visualizar» ni
   «Descargar».

**Pasa si**: ninguna solicitud de la pestaña de red descarga el contenido de un archivo médico.

> La suite ya verifica que ni el listado ni el detalle incluyan un elemento que descargue contenido por
> su cuenta. Lo que se comprueba acá es que el navegador tampoco lo pida por otra vía.

### AC-79 — La creación en tres pasos como máximo

Crear un estudio con título, fecha y un archivo, contando las pantallas.

**Un paso es cada pantalla distinta que el usuario ve** entre que elige crear y el estudio queda
guardado. Un diálogo de confirmación suma; volver a ver el mismo formulario tras un error de validación
no suma, porque es la misma pantalla otra vez.

**Pasa si**: son tres o menos.

---

## B · Seis comprobaciones sobre la instalación desplegada

### AC-56 — HTTPS y TLS 1.2 o superior

```bash
curl -I http://tu-dominio/Estudios          # debe responder una redirección a https
openssl s_client -connect tu-dominio:443 -tls1_2 </dev/null 2>&1 | grep Protocol
```

**Pasa si**: la solicitud sin cifrar redirige a HTTPS y la conexión negocia TLS 1.2 o superior.

### AC-59 — Un respaldo por día, 30 días de retención

Tras 30 días de operación, listar el destino de respaldos.

**Pasa si**: hay al menos un par base + archivos por cada día, y los de los últimos 30 días siguen
disponibles.

### AC-60 — Los respaldos no se pueden alterar desde el entorno principal

Con las credenciales del servidor, intentar borrar o modificar un respaldo.

**Pasa si**: la operación es rechazada.

### AC-61, AC-67 — Restauración en un entorno limpio

Procedimiento completo en [`docs/operacion.md`](../../docs/operacion.md) §4.

**Pasa si**: un estudio que tenía tres archivos conserva sus metadatos y sus tres archivos vinculados,
ningún estudio referencia un archivo inexistente, y no hay archivos sin estudio.

### AC-68 — Sin la clave, los respaldos no se descifran

Restaurar base y archivos en un entorno limpio **sin** configurar la clave de cifrado.

**Pasa si**: la aplicación no arranca. Y si se configura una clave distinta, los archivos no se abren.

> Es la mitad del respaldo que nadie prueba nunca, y es justo la que sostiene RNF-58.

---

## C · Tres mediciones de rendimiento

### AC-51, AC-52 — Búsqueda y listado sobre 2.000 estudios

Automatizadas, pero fuera de la suite habitual porque sembrar 2.000 estudios la volvería lenta:

```bash
dotnet test --filter "Category=Rendimiento"
```

Imprimen el p95 medido y su límite. Última corrida sobre el servidor de pruebas: **7 ms** para la
búsqueda (límite 1.000) y **2 ms** para el listado (límite 2.000).

> Ese margen es de la consulta, no del sistema completo: el servidor de pruebas no atraviesa red real.
> Sobre la instalación desplegada conviene repetir la medición con el navegador.

### AC-53 — Carga de 10 MB en menos de 15 segundos a 10 Mbps

No se automatiza: exige limitar el ancho de banda del enlace.

1. Limitar la conexión a 10 Mbps (en las herramientas del navegador, «Network throttling», o con
   `tc`/Network Link Conditioner).
2. Cargar un estudio con archivos que sumen 10 MB.
3. Cronometrar desde que se confirma hasta que la operación termina.

**Pasa si**: menos de 15 segundos.

---

## D · Procedimiento de medición de los tiempos de tarea

Cubre SC-001 y SC-002, que son indicadores de éxito del producto y no criterios de aceptación.

**Quién**: una persona que ya usó la aplicación al menos una vez, para no medir el descubrimiento
inicial. En una instalación familiar, cualquiera de los integrantes salvo quien la construyó.

**Sobre qué colección**: la instalación real, con sus estudios cargados; y si todavía no los tiene, la
colección de 2.000 que genera `SembradorDeVolumen`.

**Cuántas repeticiones**: cinco por tarea, descartando la primera. Se toma la **mediana** de las cuatro
restantes.

| Indicador | Tarea | Límite | Mediana medida |
|---|---|---|---|
| SC-001 | Crear un estudio con tres archivos, desde el listado hasta verlo guardado | 60 s | ☐ |
| SC-002 | Encontrar un estudio que se sabe que existe, partiendo del listado | 10 s | ☐ |

**Se considera cumplido** si la mediana está por debajo del límite. Si una repetición se dispara por un
motivo ajeno —el teléfono se trabó, sonó el timbre— se descarta y se repite.

---

## Planilla de resultados

Se completa en cada ejecución, antes de entregar. Un criterio verificado por inspección solo se puede
dar por cumplido si queda constancia de quién lo verificó y cuándo.

| Fecha | Responsable | Bloque | Criterios | Resultado | Observaciones |
|---|---|---|---|---|---|
| | | A · navegador | AC-26, AC-38 a AC-42, AC-78, AC-79 | | |
| | | B · desplegada | AC-56, AC-59 a AC-61, AC-67, AC-68 | | |
| | | C · rendimiento | AC-51, AC-52, AC-53 | | |
| | | D · tiempos de tarea | SC-001, SC-002 | | |
