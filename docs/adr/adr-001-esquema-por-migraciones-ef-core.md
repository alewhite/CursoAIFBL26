# ADR-001: Esquema de base de datos gestionado con migraciones de EF Core

| Campo | Valor |
|-------|-------|
| Fecha | 2026-08-20 |
| Ticket | FEAT-001a |
| Estado | Aceptado |

## Contexto

FR-04 de `docs/daw/prd/prd-FEAT-001a.md` exige que el arranque "cree **o actualice**" el esquema de
la base. Esta es la primera feature del producto y la base todavía no existe, de modo que cualquiera
de las dos estrategias disponibles cumple el ticket. La diferencia aparece más adelante: cuando
lleguen la entidad Estudio y los archivos médicos, ya habrá bases con datos reales en producción.

## Opciones consideradas

### Opción 1: `Database.EnsureCreated()`

- **Pros:** una línea, sin archivos de migración, sin herramientas adicionales. Suficiente para el
  primer arranque de una base vacía.
- **Contras:** no actualiza. Si la base existe, no toca nada: no puede agregar la tabla de Estudios
  cuando llegue. Es incompatible con adoptar migraciones después, y la salida sería borrar la base,
  algo inaceptable con archivos médicos ya cargados y contra el objetivo del maestro de reducir el
  riesgo de pérdida de información. Incumple la mitad "o actualizar" de FR-04.

### Opción 2: Migraciones de EF Core aplicadas al arrancar

- **Pros:** cumple FR-04 completo. Cada cambio de esquema queda versionado y revisable en el
  repositorio. El camino de las features siguientes ya está abierto. `AGENTS.md` prohíbe migraciones
  destructivas, no migraciones.
- **Contras:** agrega archivos de migración al repositorio y un paso de generación al cambiar el
  modelo. Aplicar migraciones automáticamente al arrancar es cómodo para una instalación familiar y
  discutible en sistemas con varias instancias — que este producto no tiene ni tendrá.

## Decisión

Migraciones de EF Core, generadas en el repositorio y aplicadas durante el arranque.

La decisión no se toma por lo que necesita FEAT-001a —donde ambas opciones empatan— sino por lo que
`EnsureCreated` haría imposible después. Es una puerta que se cierra en el primer commit y se abre
solo borrando datos.

Aplicarlas automáticamente al arrancar es aceptable acá porque el despliegue es de instancia única
(`AGENTS.md` § Stack: sin microservicios, hasta 5 cuentas): no hay carrera entre instancias.

## Consecuencias

- Se agrega la migración inicial al repositorio, con las tablas de ASP.NET Core Identity.
- `Program.cs` aplica las migraciones pendientes antes de ejecutar la siembra de cuentas.
- Cambiar el modelo pasa a exigir generar una migración; un cambio sin migración no llega a la base.
- Ninguna migración puede ser destructiva, conforme a `AGENTS.md` § Qué NO hacer.
- Las features siguientes (Estudio, archivos) heredan el mecanismo sin decisión adicional.
