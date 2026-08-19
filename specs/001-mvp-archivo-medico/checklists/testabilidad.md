# Checklist de Testabilidad y Trazabilidad: Calidad de los Requisitos

**Propósito**: validar que cada requisito y cada criterio de aceptación del spec declara cómo se verifica,
que la verificación es alcanzable, y que la cadena requisito → criterio → test no tiene eslabones sueltos,
antes de pasar a `/speckit-plan` y `/speckit-tasks`
**Creado**: 2026-08-19
**Feature**: [spec.md](../spec.md)
**Profundidad**: puerta formal · **Dominio**: testabilidad y trazabilidad · **Audiencia**: autor del spec

> Este checklist evalúa **cómo están escritos los requisitos**, no si los tests pasan. Su objetivo es que
> `/speckit-tasks` no genere tareas de "escribir el test de X" cuando X no se puede automatizar, y que
> ningún requisito quede sin forma de darse por cumplido.

## Ejecutabilidad de los Criterios de Aceptación

- [ ] CHK001 - ¿Está declarado cuáles criterios de aceptación son alcanzables mediante un test de
      integración sobre la aplicación y cuáles exigen otro método? [Hueco, Trazabilidad]
- [ ] CHK002 - ¿Está registrado que instalar la aplicación y comprobar el ícono y la ventana propios
      excede lo que un test de integración puede ejercitar? [Hueco, Spec §AC-38]
- [ ] CHK003 - ¿Está registrado el método con que se evalúa la ausencia de desplazamiento horizontal en
      las diez acciones principales, que requiere una pantalla real renderizada? [Hueco, Spec §AC-39]
- [ ] CHK004 - ¿Está declarado cómo se evalúan los escenarios sin conexión, dado que dependen del
      navegador y no del servidor? [Hueco, Spec §AC-40, §AC-41]
- [ ] CHK005 - ¿Está registrado que el aviso de carga interrumpida se origina en el navegador y por lo
      tanto no puede provocarse desde una prueba del lado del servidor? [Hueco, Spec §AC-42, §FR-058]
- [ ] CHK006 - ¿Está definido el método y el entorno con que se miden los percentiles de respuesta sobre
      una colección de 2.000 estudios, y si esa medición forma parte de la suite habitual?
      [Hueco, Spec §AC-51, §AC-52]
- [ ] CHK007 - ¿Está definido cómo se reproduce una conexión de 10 Mbps para dar por cumplido el criterio
      de carga en menos de 15 segundos? [Hueco, Spec §AC-53]
- [ ] CHK008 - ¿Está declarado que la negociación de TLS y la redirección desde HTTP se evalúan sobre un
      entorno desplegado y no sobre la aplicación en pruebas? [Hueco, Spec §AC-56]
- [ ] CHK009 - ¿Está definido el método de evaluación de los criterios de respaldo, que dependen de 30
      días de calendario y de credenciales de infraestructura?
      [Hueco, Spec §AC-59, §AC-60, §AC-61, §AC-67, §AC-68]
- [ ] CHK010 - ¿Está declarado cómo se evalúa que un archivo con contenido activo no lo ejecuta, dado que
      la ejecución ocurre en el visor del navegador y no en el servidor? [Hueco, Spec §AC-26]
- [ ] CHK011 - ¿Está definido cómo se evalúa que abrir el listado y el detalle no transfiere el contenido
      de ningún archivo, lo que exige observar las solicitudes que emite el navegador?
      [Hueco, Spec §AC-78]
- [ ] CHK012 - ¿Está definido cómo se ejercita el rechazo del arranque ante la falta de la clave de
      cifrado, que ocurre antes de que la aplicación quede disponible? [Hueco, Spec §AC-83, §FR-036]

## Método de Verificación Declarado

- [ ] CHK013 - ¿Está registrado en el spec el listado de requisitos que `PRD2.md` declara verificables por
      inspección documental, de configuración o de dependencias, de modo que no se los confunda con
      huecos de cobertura? [Trazabilidad, Spec §Supuestos]
- [ ] CHK014 - ¿Está definido qué constituye evidencia suficiente para dar por cumplido un requisito
      verificado por inspección, y quién la registra? [Hueco, Spec §FR-069, §FR-070]
- [ ] CHK015 - ¿Está definido el alcance de la búsqueda de datos médicos sobre logs y métricas: qué
      canales se recolectan, durante qué operaciones y con qué nivel de detalle habilitado?
      [Medibilidad, Spec §AC-43, §AC-85]
- [ ] CHK016 - ¿Está definido cómo se enumeran "todas las rutas expuestas" para dar por cumplidos los
      criterios que dependen de esa enumeración, sin recurrir a una lista escrita a mano que envejece?
      [Medibilidad, Spec §AC-50, §AC-63]
- [ ] CHK017 - ¿Está declarado cómo se inspecciona el almacenamiento por fuera de la aplicación para
      evaluar el cifrado y el nombre físico? [Medibilidad, Spec §AC-57, §AC-65]

## Trazabilidad entre Requisitos, Criterios y Tests

- [x] CHK018 - ¿Está definido con qué identificador se nombran los tests de los requisitos derivados de
      las sesiones de clarificación, que no tienen un AC de `PRD2.md` al cual referirse?
      [Hueco, Spec §FR-025b, §FR-048b, §FR-053b, §FR-054b]
- [x] CHK019 - ¿Está resuelta la tensión entre la convención de nombrar cada test con un RF, RNF o AC y la
      existencia de requisitos del spec que no derivan de ninguno? [Conflicto, Spec §FR-054b]
- [ ] CHK020 - ¿Está registrado que los requisitos de exclusión —publicidad, seguimiento, entrenamiento de
      modelos— no admiten un test que demuestre una ausencia y se verifican de otro modo?
      [Trazabilidad, Spec §FR-063]
- [x] CHK021 - ¿Existe un requisito del spec para cada AC vigente de `PRD2.md`, sin AC huérfanos que no
      correspondan a ningún FR? [Trazabilidad]
- [x] CHK022 - ¿Existe al menos un criterio de aceptación o un método declarado para cada FR del spec, sin
      requisitos que no puedan darse por cumplidos de ninguna forma? [Trazabilidad]
- [ ] CHK023 - ¿Está definido cómo se mantiene la trazabilidad cuando una sesión de clarificación modifica
      el texto de un FR que ya tenía AC asociados? [Hueco, Spec §Clarificaciones]
- [ ] CHK024 - ¿Está declarado cuáles historias de usuario pueden evaluarse de forma independiente y
      cuáles necesitan que otra esté construida antes? [Consistencia, Spec §Historia de Usuario 3]

## Medibilidad de los Criterios de Éxito

- [ ] CHK025 - ¿Está definido el procedimiento de medición de los tiempos de tarea, incluyendo quién los
      ejecuta, sobre qué colección y cuántas repeticiones? [Medibilidad, Spec §SC-001, §SC-002]
- [ ] CHK026 - ¿Está definido qué se cuenta como "intento de acceso cruzado ensayado" para poder afirmar
      que fueron cero las entregas indebidas? [Medibilidad, Spec §SC-007]
- [ ] CHK027 - ¿Es evaluable el criterio de mantener el costo mensual dentro del límite, y está definido
      quién lo mide y con qué periodicidad? [Medibilidad, Spec §SC-014]
- [ ] CHK028 - ¿Está definido el momento y el responsable de dar por validados los criterios de aceptación
      al 100 %, dado que ese criterio de éxito depende de todos los demás? [Medibilidad, Spec §SC-015]
- [ ] CHK029 - ¿Se distinguen los criterios de éxito que se evalúan una vez antes de entregar de los que
      exigen observación continua en operación? [Claridad, Spec §SC-013, §SC-014]

## Datos, Tiempo y Aislamiento en la Verificación

- [ ] CHK030 - ¿Está especificado que los archivos de prueba se generan y no se copian de un caso real, y
      qué formatos deben poder generarse para cubrir los criterios de validación?
      [Completitud, Spec §FR-073]
- [ ] CHK031 - ¿Está declarado que las ventanas temporales —30 minutos, 24 horas, 15 minutos, 5 minutos—
      exigen poder controlar el tiempo desde la prueba, en lugar de esperarlo?
      [Hueco, Spec §FR-005, §FR-006, §FR-007, §FR-046]
- [ ] CHK032 - ¿Está definido cómo se construye el estado de partida de una prueba que necesita estudios
      de dos propietarios distintos, sin romper el aislamiento que se está evaluando?
      [Hueco, Spec §FR-012, §FR-013]
- [ ] CHK033 - ¿Está definido cómo se alcanza el estado de cupo agotado sin cargar realmente 20 GB?
      [Hueco, Spec §FR-037, §AC-55]
- [ ] CHK034 - ¿Está definido cómo se construye una colección de 2.000 estudios reproducible para las
      mediciones de rendimiento? [Hueco, Spec §AC-51, §AC-52]
- [ ] CHK035 - ¿Está especificado que los datos ficticios deben ser únicos e irrepetibles cuando la
      evaluación consiste en buscarlos dentro de logs y métricas? [Completitud, Spec §AC-43, §AC-85]

## Cobertura por Clase de Escenario

- [ ] CHK036 - ¿Están cubiertos por algún criterio los flujos de recuperación tras un fallo parcial, como
      el borrado que falla a mitad de camino o la carga interrumpida?
      [Cobertura, Recuperación, Spec §FR-044, §FR-058]
- [ ] CHK037 - ¿Están cubiertos los escenarios de excepción de cada límite del sistema —cantidad de
      archivos, tamaño, cupo, cuentas— con un criterio propio y no solo con el camino feliz?
      [Cobertura, Spec §FR-023, §FR-027, §FR-037, §FR-011]
- [ ] CHK038 - ¿Está cubierta por algún criterio la clase de escenario de datos vacíos, ahora que el spec
      distingue dos estados de listado vacío? [Cobertura, Spec §FR-053b]
- [ ] CHK039 - ¿Están cubiertos los escenarios alternos de sesión —expiración deslizante, tope absoluto,
      cierre manual— con criterios distinguibles entre sí? [Cobertura, Spec §FR-004, §FR-005, §FR-006]
- [ ] CHK040 - ¿Está cubierta la clase de escenario de concurrencia, o está declarada como fuera del
      alcance de la verificación? [Cobertura, Hueco]

## Dependencias y Supuestos de la Estrategia de Verificación

- [ ] CHK041 - ¿Está documentado el supuesto de que la aplicación puede levantarse completa en una prueba,
      incluidos un almacenamiento de archivos y una base de datos descartables?
      [Supuesto, Spec §Historia de Usuario 2]
- [ ] CHK042 - ¿Está documentado qué parte de la verificación depende de infraestructura inexistente en el
      entorno de desarrollo —respaldos, custodia de la clave, TLS— y quién la ejecuta?
      [Supuesto, Spec §FR-064, §FR-068, §FR-060]
- [ ] CHK043 - ¿Está declarado si la evaluación de los criterios que requieren un navegador real forma
      parte de la definición de terminado o queda como comprobación manual previa a la entrega?
      [Hueco, Spec §AC-38, §AC-39, §AC-40]

## Notas

- Marcar un ítem como cumplido exige poder señalar el texto del spec que lo satisface; si hay que
  deducirlo, el ítem no pasa.
- Muchos ítems se cumplen **declarando** el método de verificación, no automatizando nada: la respuesta
  correcta a "¿cómo se evalúa AC-38?" puede ser perfectamente "a mano, una vez, antes de entregar",
  siempre que quede escrito.
- CHK018 y CHK019 son el par crítico: cuatro requisitos nacidos de las sesiones de clarificación no citan
  ningún AC de `PRD2.md`, y la convención del proyecto pide que cada test nombre un RF, RNF o AC.
  Resolverlo puede requerir incorporar esos requisitos a `PRD2.md` o definir un esquema propio de
  identificadores para el spec.
