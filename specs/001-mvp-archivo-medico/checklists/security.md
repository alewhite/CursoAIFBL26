# Checklist de Seguridad: Calidad de los Requisitos

**Propósito**: validar que los requisitos de seguridad y privacidad del spec están completos, son
inequívocos, consistentes entre sí y verificables objetivamente, antes de pasar a `/speckit-plan`
**Creado**: 2026-08-19
**Feature**: [spec.md](../spec.md)
**Profundidad**: puerta formal · **Dominio**: seguridad y privacidad · **Audiencia**: autor del spec

> Este checklist evalúa **cómo están escritos los requisitos**, no si el sistema funciona. Un ítem que no
> pasa se resuelve corrigiendo el spec —y, si toca alcance, `PRD2.md` primero (Principio II)—, no
> escribiendo código.

## Completitud de los Requisitos

- [ ] CHK001 - ¿Está enumerada de forma cerrada la lista de rutas que pueden atenderse sin sesión, de modo
      que la excepción a la autenticación por omisión sea auditable? [Hueco, Spec §FR-001]
- [ ] CHK002 - ¿Está especificado el requisito de protección contra falsificación de solicitudes entre
      sitios para los formularios que mutan datos, más allá del atributo SameSite de la cookie?
      [Hueco, Spec §FR-008]
- [ ] CHK003 - ¿Está definido si el cierre de sesión invalida únicamente la sesión en curso o todas las
      sesiones activas de la cuenta? [Hueco, Spec §FR-004]
- [ ] CHK004 - ¿Está especificado que el identificador de sesión debe renovarse al autenticarse, para
      cerrar la fijación de sesión? [Hueco, Spec §FR-002]
- [ ] CHK005 - ¿Están definidos requisitos mínimos de fortaleza para las contraseñas que el procedimiento
      administrativo asigna? [Hueco, Spec §FR-009, §FR-010]
- [ ] CHK006 - ¿Está especificado el comportamiento esperado cuando una credencial almacenada quedó con
      parámetros de derivación por debajo del mínimo exigido? [Hueco, Spec §FR-009]
- [ ] CHK007 - ¿Está definido el tratamiento del área de tránsito donde el archivo existe sin cifrar
      durante la validación: su ubicación, su protección y su limpieza? [Hueco, Spec §FR-030, §FR-035]
- [ ] CHK008 - ¿Está especificado qué protección tienen los metadatos médicos en reposo, dado que el
      requisito de cifrado alcanza solo a los archivos? [Hueco, Spec §FR-035]
- [ ] CHK009 - ¿Está definido un requisito de rotación o reemplazo de la clave de cifrado ante sospecha de
      compromiso? [Hueco, Spec §FR-036, §FR-068]
- [ ] CHK010 - ¿Está especificado el requisito de forzar el uso de HTTPS en visitas posteriores, y no solo
      la redirección de la solicitud sin cifrar? [Hueco, Spec §FR-060]
- [ ] CHK011 - ¿Está definido qué sí puede registrarse en logs —identificadores técnicos, códigos de
      error— y no únicamente lo que está prohibido? [Completitud, Spec §FR-061]
- [x] CHK012 - ¿Están cubiertos por el requisito de no registrar datos médicos los logs que produce la
      infraestructura por fuera de la aplicación, en particular los que registran la URL completa con el
      término de búsqueda? [Hueco, Spec §FR-061]
- [ ] CHK013 - ¿Está especificada la política positiva de la caché del navegador —qué puede guardarse—, y
      no solo la prohibición de mostrar datos médicos sin sesión? [Completitud, Spec §FR-057]
- [ ] CHK014 - ¿Está definida la sanitización del nombre de archivo en el momento de la descarga, y no
      solo al almacenarlo y mostrarlo? [Completitud, Spec §FR-034]
- [ ] CHK015 - ¿Están documentados los requisitos de retención y de control de acceso de los propios logs
      técnicos? [Hueco, Spec §FR-061, §FR-062]

## Claridad y Ausencia de Ambigüedad

- [x] CHK016 - ¿Está definido si la expiración por inactividad es deslizante —se renueva con cada
      solicitud— o cuenta desde el inicio de la sesión? [Ambigüedad, Spec §FR-005]
- [x] CHK017 - ¿Está definido qué solicitudes cuentan como "actividad" a efectos de esa expiración, en
      particular si una solicitud emitida por la aplicación instalada mantiene viva la sesión?
      [Ambigüedad, Spec §FR-005]
- [x] CHK018 - ¿Está resuelto si un recurso ajeno debe responder 403 o 404, dado que 403 confirma la
      existencia del recurso y 404 no? [Ambigüedad, Spec §FR-012]
- [x] CHK019 - ¿Está definido si el contador de intentos fallidos se acumula también para nombres de
      usuario inexistentes, de modo que la diferencia de comportamiento no permita enumerar cuentas?
      [Ambigüedad, Spec §FR-007]
- [ ] CHK020 - ¿Está especificado si un intento realizado durante el bloqueo extiende la ventana de 15
      minutos o la deja correr? [Ambigüedad, Spec §FR-007]
- [ ] CHK021 - ¿Está definido qué identificador usa el inicio de sesión —nombre de usuario o correo—, dado
      que la elección cambia qué se puede enumerar desde afuera? [Ambigüedad, Spec §FR-002]
- [ ] CHK022 - ¿Está definido qué significa "cuenta activa" a efectos del límite de cinco, y si una cuenta
      deshabilitada libera un lugar? [Ambigüedad, Spec §FR-011]
- [ ] CHK023 - ¿Está especificado el modo de operación del cifrado y el tratamiento del vector de
      inicialización, o "AES-256" admite modos que no protegen el contenido? [Claridad, Spec §FR-035]
- [ ] CHK024 - ¿Está definida en un único lugar la expresión "dato médico", en lugar de reenumerarse campo
      por campo en cada requisito que la usa? [Claridad, Spec §FR-061, §FR-062, §FR-063]
- [ ] CHK025 - ¿Está especificado si la cookie de autenticación es de sesión del navegador o persistente,
      dado que la aplicación instalada puede cerrarse y reabrirse? [Ambigüedad, Spec §FR-008]

## Consistencia entre Requisitos

- [x] CHK026 - ¿Son consistentes el requisito de URLs temporales con expiración de 5 minutos y el que
      exige que ninguna URL entregue un archivo sin sesión válida, o el primero describe un mecanismo que
      el segundo vuelve innecesario? [Conflicto, Spec §FR-045, §FR-046]
- [ ] CHK027 - ¿Es consistente el borrado físico e inmediato de un estudio con la retención de 30 días de
      los respaldos, y está documentado que el contenido eliminado sobrevive en ellos?
      [Conflicto, Spec §FR-044, §FR-064]
- [ ] CHK028 - ¿Es consistente la prohibición de datos médicos en logs con el requisito de mostrar errores
      de validación junto al archivo que los produjo, respecto de qué información puede viajar en un
      mensaje de error? [Consistencia, Spec §FR-025, §FR-061]
- [ ] CHK029 - ¿Se aplica el mismo criterio de aislamiento por propietario a todos los canales que
      exponen datos derivados —listado, contador de resultados, lista de instituciones del filtro, aviso
      de cupo—, o alguno quedó fuera de la enumeración? [Consistencia, Spec §FR-013, §FR-037, §FR-050]
- [ ] CHK030 - ¿Es consistente el requisito de no ejecutar contenido activo al visualizar con el de
      mostrar el archivo dentro de la aplicación, respecto de qué se le exige al mecanismo de
      incrustación? [Consistencia, Spec §FR-039, §FR-040]

## Calidad de los Criterios de Aceptación

- [ ] CHK031 - ¿Es objetivamente medible el criterio que verifica el cifrado, dado que comprobar que los
      bytes "no corresponden al original en claro" lo satisface también un cifrado débil?
      [Medibilidad, Spec §AC-57]
- [ ] CHK032 - ¿Es objetivamente verificable el requisito de no usar contenido médico para entrenar
      modelos, o depende enteramente de una declaración? [Medibilidad, Spec §FR-063]
- [ ] CHK033 - ¿Está definido el universo sobre el que se busca la ausencia de datos médicos en logs y
      métricas —qué canales se recolectan y durante qué operaciones—, de modo que el criterio pueda darse
      por cumplido? [Medibilidad, Spec §AC-43, §AC-85]
- [x] CHK034 - ¿Está especificado que el mensaje ante credenciales inválidas debe ser indistinguible
      también en tiempo de respuesta, o solo en su texto? [Medibilidad, Spec §FR-003]
- [ ] CHK035 - ¿Puede verificarse el criterio de que ninguna ruta acepta una creación de cuenta sin
      depender de una enumeración manual y por lo tanto incompleta? [Medibilidad, Spec §AC-50, §AC-63]

## Cobertura de Escenarios y Casos Límite

- [ ] CHK036 - ¿Está definido qué ocurre cuando la duración máxima de la sesión vence en medio de una
      carga en curso? [Cobertura, Excepción, Spec §FR-006]
- [ ] CHK037 - ¿Están especificados requisitos para el escenario de recuperación tras pérdida de la clave
      de cifrado, aunque sea para declarar que los archivos son irrecuperables?
      [Cobertura, Recuperación, Spec §FR-068]
- [ ] CHK038 - ¿Está definido el comportamiento esperado ante solicitudes simultáneas de la misma cuenta
      desde varios dispositivos, y si existe un límite de sesiones concurrentes?
      [Cobertura, Escenario alterno, Spec §FR-005]
- [x] CHK039 - ¿Están cubiertos por un requisito los identificadores inexistentes y los mal formados con
      el mismo tratamiento que los ajenos, para que las tres respuestas sean indistinguibles?
      [Cobertura, Caso límite, Spec §FR-012]
- [ ] CHK040 - ¿Está especificado qué debe ocurrir si el borrado del contenido cifrado falla después de
      haberse eliminado los metadatos del estudio? [Cobertura, Excepción, Spec §FR-044]

## Dependencias y Supuestos

- [ ] CHK041 - ¿Está validado el supuesto de que existe un canal de métricas técnicas, o el requisito que
      lo restringe carece de objeto? [Supuesto, Spec §FR-062]
- [ ] CHK042 - ¿Está documentado quién custodia la clave de cifrado y bajo qué procedimiento, dado que el
      requisito solo exige que esté fuera del entorno y de los respaldos? [Supuesto, Spec §FR-068]
- [x] CHK043 - ¿Está registrado como decisión explícita, con su justificación, que el bloqueo por
      intentos fallidos opera por cuenta y habilita una denegación de servicio dirigida?
      [Trazabilidad, Spec §FR-007, Casos Límite]
- [ ] CHK044 - ¿Trazan los requisitos de seguridad del spec a los riesgos enumerados en `PRD2.md`, de modo
      que pueda detectarse un riesgo sin requisito que lo mitigue? [Trazabilidad]

## Notas

- Marcar un ítem como cumplido exige poder señalar el texto del spec que lo satisface; si hay que
  deducirlo, el ítem no pasa.
- Los ítems marcados `[Hueco]` esperan que el spec **no** tenga hoy ese requisito: cumplirlos implica
  agregarlo, o dejar asentado por qué queda deliberadamente fuera.
- Un ítem que exija ampliar el alcance del producto requiere modificar antes `PRD2.md` (Principio II de
  la constitución). Precisar la redacción de un requisito ya existente no lo requiere.
