# Threat Model — FEAT-001a: Arranque de la solución y alta administrativa de cuentas

| Campo | Valor |
|-------|-------|
| Ticket | FEAT-001a |
| Fecha | 2026-08-20 |
| Tier | FEATURE |
| PRD | `docs/daw/prd/prd-FEAT-001a.md` |
| Método | STRIDE por componente, sobre el diseño propuesto en PLAN |

## Alcance del análisis

Esta feature no expone todavía ningún inicio de sesión (FEAT-001b) ni almacena ningún archivo
médico (feature posterior). Lo que sí introduce es la **primera custodia de credenciales del
sistema**: contraseñas que entran por configuración externa y hashes que quedan persistidos en un
archivo SQLite en disco. El análisis se concentra ahí.

## Componentes analizados

| ID | Componente | Introducido por |
|---|---|---|
| C1 | Proceso web ASP.NET Core (host y pipeline mínimo) | Bloque 1 |
| C2 | `AppDbContext` y el archivo SQLite (`.db`, `-wal`, `-shm`) en disco | Bloque 2 |
| C3 | Proveedor de configuración externa (variables de entorno / user-secrets) | Bloque 2 y 3 |
| C4 | `AccountProvisioner` y el password hasher de Identity | Bloque 3 |
| C5 | Cadena de construcción: SDK, paquetes NuGet, proyecto de tests | Bloques 1 a 3 |

## Fronteras de confianza declaradas

| ID | Frontera | Cruce |
|---|---|---|
| TB-1 | Red pública → proceso web (C1) | HTTP entrante. En esta feature la superficie es mínima: no hay rutas privadas ni de registro. |
| TB-2 | Proceso web (C1) → sistema de archivos del servidor (C2) | Lectura y escritura del archivo de base de datos y sus auxiliares. |
| TB-3 | Custodia de secretos del entorno (C3) → proceso web (C1) | Las contraseñas de alta y la ubicación de la base cruzan acá, en claro, al arrancar. |
| TB-4 | Administrador técnico (persona) → despliegue | Quien define las altas y opera el arranque. Es el único camino de alta que existe. |
| TB-5 | Repositorio y registro de paquetes → artefacto desplegado (C5) | SDK y NuGet: cadena de suministro. |

## Clasificación de datos

| Dato | Clasificación | En tránsito | En reposo |
|---|---|---|---|
| Contraseña de alta declarada en configuración externa | **Credenciales** | No viaja por la red: entra por el entorno del proceso | Custodiada por el gestor de secretos o el entorno; nunca versionada, nunca escrita en log |
| Hash de contraseña persistido | **Credenciales (derivado)** | No se transmite en esta feature | Derivación irreversible PBKDF2-HMAC-SHA256 con ≥ 100.000 iteraciones y sal por usuario. No es cifrado y no pretende serlo: no existe operación que devuelva la contraseña |
| Nombre de usuario | **PII de bajo grado** (identifica a un integrante del grupo familiar) | Sin transmisión en esta feature | En claro en la base, protegido por ubicación privada y permisos del sistema operativo |
| Datos médicos | **No existen en esta feature** | — | — |

**Cifrado en reposo de la base (F-TM-07):** el archivo SQLite **no** se cifra. Es un riesgo aceptado
formalmente, registrado abajo como R-09, no una omisión.

## Análisis STRIDE por componente

### C1 — Proceso web

| STRIDE | Amenaza | Tratamiento |
|---|---|---|
| Spoofing | Suplantación de identidad de usuario | No aplica todavía: no hay autenticación expuesta. Llega con FEAT-001b y se analiza en su propio threat model. |
| Tampering | Manipulación de la respuesta en tránsito | TLS 1.2+ es requisito del maestro (RNF-01) y se implementa en FEAT-001b junto con la primera ruta que transporta credenciales. Esta feature no transporta ninguna. |
| Repudiation | No queda constancia de qué hizo el arranque | R-07 |
| Information Disclosure | Página de error de desarrollo expuesta en producción | R-06 |
| Denial of Service | Un alta inválida deja el servicio caído | R-05 |
| Elevation of Privilege | No hay roles ni privilegios diferenciados en el MVP (RNF-57) | Sin superficie: el modelo no tiene rol que escalar. |

### C2 — Base SQLite en disco

| STRIDE | Amenaza | Tratamiento |
|---|---|---|
| Spoofing | Sustitución del archivo de base por otro | R-08 |
| Tampering | Modificación directa del archivo por otro proceso | R-08 |
| Repudiation | — | Cubierto por R-07. |
| Information Disclosure | La base termina versionada, servida por HTTP, o legible por cualquier usuario del sistema | R-01, R-02, R-09 |
| Denial of Service | Bloqueo de escritura por WAL con permisos mal puestos | R-08 |
| Elevation of Privilege | Quien lee el archivo obtiene hashes y puede atacarlos sin límite de intentos | R-09, mitigado por el costo de PBKDF2 (R-04) |

### C3 — Configuración externa

| STRIDE | Amenaza | Tratamiento |
|---|---|---|
| Spoofing | Una variable de entorno inyectada declara una cuenta no prevista | R-03 |
| Tampering | Alteración de la configuración de altas entre despliegues | R-03, R-10 |
| Repudiation | No se sabe qué configuración produjo qué cuentas | R-07 |
| Information Disclosure | Contraseñas en `appsettings.json` versionado, en un volcado de configuración o en un log de arranque | **R-01**, **R-02** |
| Denial of Service | Configuración ausente o ilegible impide arrancar | R-05 |
| Elevation of Privilege | Quien controla el entorno controla las cuentas | Aceptado por diseño: es el procedimiento administrativo que exige RNF-54 del maestro. Documentado en R-10. |

### C4 — Provisionador de cuentas y hasher

| STRIDE | Amenaza | Tratamiento |
|---|---|---|
| Spoofing | Un arranque pisa la contraseña de una cuenta existente y permite tomarla | **R-10** |
| Tampering | Alteración de cuentas existentes como efecto colateral del arranque | **R-10** |
| Repudiation | No queda registro de altas y rechazos | R-07 |
| Information Disclosure | La contraseña aparece en el mensaje de rechazo o en la excepción | **R-01** |
| Denial of Service | Un rechazo aborta el arranque completo | R-05 |
| Elevation of Privilege | Superar el máximo de 5 cuentas activas | R-03 |

### C5 — Cadena de construcción

| STRIDE | Amenaza | Tratamiento |
|---|---|---|
| Spoofing | Paquete NuGet suplantado | R-11 |
| Tampering | Dependencia comprometida en la cadena de suministro | R-11 |
| Repudiation | — | Sin superficie relevante en esta feature. |
| Information Disclosure | Un paquete de telemetría filtra datos | R-11, reforzado por la prohibición de herramientas de seguimiento en `AGENTS.md` |
| Denial of Service | Build irreproducible por SDK equivocado | R-12 |
| Elevation of Privilege | Ejecución de código arbitrario en tiempo de build | R-11 |

## Riesgos y tratamiento

| ID | Riesgo | STRIDE | Probab. | Impacto | Tratamiento |
|---|---|---|---|---|---|
| **R-01** | La contraseña de alta termina en un log, una excepción o un mensaje de arranque | I | Alta | **Alto** | Mitigación M-1 |
| **R-02** | El archivo de base o un archivo con secretos queda versionado o dentro de una carpeta pública | I | Alta | **Alto** | Mitigación M-2, M-3 |
| **R-03** | La configuración declara más cuentas de las permitidas o cuentas no previstas | S/E | Media | **Alto** | Mitigación M-4 |
| **R-04** | Hash débil por heredar el valor por defecto del framework | I | Media | Medio | Mitigación M-5 |
| **R-05** | Un alta inválida aborta el arranque y deja el servicio caído | D | Media | Medio | Mitigación M-6 |
| **R-06** | Páginas de error detalladas expuestas fuera de desarrollo | I | Baja | Medio | Mitigación M-7 |
| **R-07** | No queda constancia de qué altas se aplicaron y cuáles se rechazaron | R | Media | Bajo | Mitigación M-8 |
| **R-08** | Permisos del archivo de base demasiado abiertos en el servidor | I/T/D | Media | **Alto** | Mitigación M-9 |
| **R-09** | Quien obtiene el archivo de base obtiene los hashes y puede atacarlos sin límite de intentos | I/E | Baja | Medio | **Riesgo aceptado A-1** |
| **R-10** | Un arranque posterior modifica o pisa cuentas existentes | S/T | Media | **Alto** | Mitigación M-10 |
| **R-11** | Dependencia de terceros comprometida o innecesaria | T/S | Baja | Alto | Mitigación M-11 |
| **R-12** | Build contra el SDK equivocado (la máquina resuelve 10.0.300 por defecto) | D | **Alta** | Bajo | Mitigación M-12 |
| **R-13** | Las contraseñas de siembra permanecen en claro en el entorno del proceso en todos los arranques | I | Media | **Alto** | Mitigación M-13 |

## Mitigaciones a incorporar en la spec

- **M-1 (R-01):** ningún mensaje de arranque, log, excepción ni `ToString()` puede contener la contraseña declarada ni ningún hash. El rechazo nombra la cuenta y la regla incumplida, nada más. Cubre NFR-05 y AC-08.
- **M-2 (R-02):** el origen de las altas son variables de entorno o user-secrets. `appsettings.json` —que sí se versiona— no contiene ninguna contraseña ni cadena de conexión, y un test lo verifica sobre el archivo real.
- **M-3 (R-02):** `.gitignore` cubre `*.db`, `*.db-wal`, `*.db-shm`, `bin/` y `obj/`; y el arranque **falla** si la ruta resuelta de la base cae dentro del `ContentRoot` de la aplicación o de una carpeta servida públicamente. Cubre NFR-04 y AC-06.
- **M-4 (R-03):** el límite de 5 se evalúa contra el total de cuentas **persistidas**, no contra la lista declarada, de modo que dos arranques sucesivos no puedan superarlo. Cubre AC-03.
- **M-5 (R-04):** las iteraciones de PBKDF2 se fijan explícitamente en ≥ 100.000 **en el código de composición** (`PasswordHasherOptions.IterationCount` en `Program.cs`), no en `appsettings.json` ni en una variable de entorno: un parámetro de seguridad configurable desde afuera del repositorio es degradable sin revisión ni test que lo note, y un despliegue con `IterationCount: 1000` arrancaría sin que nada se queje. AC-05 inspecciona el valor almacenado.
- **M-6 (R-05):** el rechazo de un alta **no** aborta el arranque: la aplicación sigue levantando con las cuentas válidas, según exigen AC-03 y AC-04. Lo único que debe abortar el arranque es la imposibilidad de resolver la ubicación de la base (M-3) — y, cuando llegue la feature de archivos, la clave de cifrado ausente (RNF-62 del maestro).
- **M-7 (R-06):** páginas de error detalladas únicamente en el entorno de desarrollo; fuera de él, respuesta genérica.
- **M-8 (R-07):** cada alta aplicada y cada alta rechazada dejan una entrada de log con el nombre de usuario y la regla evaluada, sin credenciales.
- **M-9 (R-08):** el directorio de la base se crea con permisos restrictivos al usuario propietario (`0700` en plataformas POSIX) y se verifica en test donde la plataforma lo permita.
- **M-10 (R-10):** la siembra es estrictamente aditiva: si la cuenta existe, no se toca — ni la contraseña, ni ninguna otra propiedad. Cambiar una contraseña es un procedimiento administrativo explícito, nunca un efecto colateral de reiniciar el proceso. Cubre NFR-07 y AC-07.
- **M-11 (R-11):** las dependencias se limitan a paquetes del framework (`Microsoft.*`) y al runner de tests. Ninguna dependencia de telemetría ni de seguimiento, conforme a `AGENTS.md`.
- **M-12 (R-12):** `global.json` fija el SDK en 8.0.x, de modo que el build no dependa de qué SDK resuelva la máquina.
- **M-13 (R-13):** la siembra es aditiva y no exige que la configuración de altas siga presente: una vez creada la cuenta, las entradas pueden retirarse del entorno y los arranques siguientes levantan igual, con las cuentas intactas. La spec debe documentarlo, porque es lo que permite que las contraseñas en claro no vivan indefinidamente en el entorno del proceso.

## Riesgos aceptados

### A-1 — La base de datos no se cifra en reposo (R-09)

- **Qué se acepta:** el archivo SQLite, con sus nombres de usuario y sus hashes de contraseña —y más adelante los metadatos de estudios— queda sin cifrar en el disco del servidor. Quien obtenga el archivo puede atacar los hashes fuera de línea, sin el límite de 5 intentos que protege el inicio de sesión.
- **Quién lo acepta:** el propietario del producto, en `docs/daw/prd/PRD2.md` revisión 4, que incorpora explícitamente "el riesgo aceptado de los metadatos sin cifrar en reposo". `AGENTS.md` lo repite: la base no está obligada a estar cifrada, y los metadatos se protegen por aislamiento, permisos del sistema operativo y ubicación privada.
- **Justificación:** cifrar la base exigiría una extensión de SQLite fuera del stack declarado o una clave adicional en línea, con custodia propia, para una instalación familiar de hasta 5 cuentas y un techo de costo de USD 15 mensuales. El costo de esa complejidad supera al riesgo mientras el servidor sea de uso privado. Las contraseñas, además, no quedan expuestas: PBKDF2 con ≥ 100.000 iteraciones y sal por usuario hace costoso el ataque fuera de línea, y el mínimo de 12 caracteres de NFR-03 lo vuelve impracticable para contraseñas generadas.
- **Condiciones de revisión:** se revisa si la instalación deja de ser familiar o supera las 5 cuentas; si pasa a alojarse en infraestructura compartida o de terceros con acceso al disco; si los respaldos salen del control del administrador técnico; o si aparece cualquier requerimiento regulatorio sobre datos médicos en reposo. La llegada de los archivos médicos **no** dispara esta revisión: esos sí se cifran con AES-256 por RNF-02, y este riesgo cubre solo los metadatos.

## Verificación de las reglas del catálogo

- **F-TM-01:** los 5 componentes (C1 a C5) tienen las 6 categorías STRIDE evaluadas. ✅
- **F-TM-02:** 5 fronteras de confianza declaradas (TB-1 a TB-5). ✅
- **F-TM-03:** las 13 amenazas tienen mitigación (M-1 a M-13) o aceptación formal (A-1). ✅
- **F-TM-04:** A-1 declara quién acepta, la justificación y las condiciones de revisión. ✅
- **F-TM-05:** datos clasificados en credenciales, credencial derivada y PII de bajo grado. ✅
- **F-TM-06:** el modelo referencia los componentes reales del diseño (`AppDbContext`, `AccountProvisioner`, `global.json`, `appsettings.json`) y sus flujos concretos. ✅
- **F-TM-07:** el tratamiento en reposo de cada dato sensible está especificado, incluida la ausencia deliberada de cifrado de la base, tratada como riesgo aceptado con los 3 campos. ✅
- **W-TM-01:** la cadena de suministro se analiza en C5 y se trata en M-11. ✅
- **W-TM-02:** DoS analizado en C1, C2, C3 y C4. ✅

## Revisión posterior a la auditoría de arquitectura

`daw-arch-auditor` auditó el diseño y devolvió BLOCKED con 5 hallazgos bloqueantes. Dos afectan
directamente a este modelo y quedan incorporados arriba:

- **M-5 se desambiguó:** "en configuración de Identity" era ambiguo y admitía leer las iteraciones
  desde `appsettings.json` o el entorno, lo que dejaba un parámetro de seguridad degradable desde
  afuera del repositorio. Ahora dice explícitamente que va en el código de composición.
- **R-13 y M-13 son nuevos:** el modelo original no se hacía cargo de que las contraseñas de siembra
  viven en el entorno del proceso en *todos* los arranques, no solo en el primero.

Un tercer hallazgo refuerza M-3 sin cambiarlo: el fail-fast ante configuración ausente no es solo
higiene de esta feature, es el mismo patrón que RNF-62 del maestro exigirá para la clave de cifrado,
resuelto en el mismo archivo. Un default tolerante acá instalaría el antipatrón contrario justo donde
después hay que fallar.
