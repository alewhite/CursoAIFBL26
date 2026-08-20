# ADR-003: Alta de cuentas en el arranque desde configuración externa

| Campo | Valor |
|-------|-------|
| Fecha | 2026-08-20 |
| Ticket | FEAT-001a |
| Estado | Aceptado |

## Contexto

RNF-54 del PRD maestro prohíbe el registro abierto de cuentas: el alta y el restablecimiento de
contraseña son procedimientos administrativos fuera de la interfaz. FR-01 de
`docs/daw/prd/prd-FEAT-001a.md` traslada esa prohibición a este ticket, y AC-02 exige que las rutas
de registro respondan 404. Algún mecanismo tiene que crear las hasta 5 cuentas del grupo familiar.

## Opciones consideradas

### Opción 1: Siembra en el arranque leyendo configuración externa

- **Pros:** sin superficie HTTP nueva, así que no hay nada que atacar ni que proteger. El
  administrador técnico ya opera el despliegue. Se apoya en el mecanismo de configuración que el
  proyecto necesita igual para la cadena de conexión y, más adelante, para la clave de cifrado.
- **Contras:** las contraseñas viven en claro en el entorno del proceso, y dar de alta a alguien
  exige un despliegue.

### Opción 2: Un comando de administración fuera de la aplicación web

- **Pros:** las contraseñas no quedan en el entorno del servicio; el alta es un acto explícito y no
  un efecto del arranque.
- **Contras:** un segundo ejecutable y un segundo camino de acceso a la base para dar de alta a lo
  sumo 5 cuentas, probablemente una sola vez. Desproporcionado para el tamaño del producto.

### Opción 3: Una pantalla de alta protegida por rol de administrador

- **Pros:** la más cómoda de operar.
- **Contras:** prohibida por RNF-54 y por RNF-57 del maestro, que excluye roles y cuentas con
  visibilidad sobre datos de terceros. No es una opción viable.

## Decisión

Opción 1: siembra en el arranque, leyendo las altas de la configuración externa (variables de entorno
o user-secrets), nunca de un archivo versionado.

La siembra es **estrictamente aditiva**: si la cuenta ya existe, no se toca — ni su contraseña ni
ninguna otra propiedad. Un arranque no puede cambiar credenciales: eso convertiría reiniciar el
proceso en un vector de secuestro de cuenta si la configuración cambió.

Como corolario, **la configuración de altas puede retirarse del entorno una vez creada la cuenta**:
los arranques siguientes levantan igual, con las cuentas intactas. Es lo que evita que las
contraseñas en claro vivan indefinidamente junto al proceso.

Un alta rechazada —por superar el límite de 5 o por no alcanzar los 12 caracteres— **no aborta el
arranque**: la aplicación levanta con las cuentas válidas e informa el rechazo, como exigen AC-03 y
AC-04.

## Consecuencias

- Dar de alta o restablecer una contraseña exige acceso al entorno y un reinicio; queda registrado
  como el procedimiento administrativo del producto.
- El límite de 5 se evalúa contra las cuentas persistidas, no contra la lista declarada, para que dos
  arranques sucesivos no puedan superarlo.
- Ningún log, mensaje de arranque ni excepción puede contener la contraseña declarada.
- `appsettings.json`, que sí se versiona, no contiene ninguna contraseña; un test lo verifica.
- No existe camino en la interfaz para crear, cambiar o recuperar una contraseña, y AC-02 lo prueba.
