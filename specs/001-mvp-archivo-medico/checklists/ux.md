# Checklist de UX y Usabilidad: Calidad de los Requisitos

**Propósito**: validar que los requisitos de interfaz, mensajes y estados del spec están completos, son
inequívocos, consistentes entre sí y verificables objetivamente, antes de pasar a `/speckit-plan`
**Creado**: 2026-08-19
**Feature**: [spec.md](../spec.md)
**Profundidad**: puerta formal · **Dominio**: UX y usabilidad · **Audiencia**: autor del spec

> Este checklist evalúa **cómo están escritos los requisitos**, no si la interfaz se ve bien. Un ítem que
> no pasa se resuelve corrigiendo el spec —y, si toca alcance, `PRD2.md` primero (Principio II)—, no
> maquetando una pantalla.

## Completitud de los Requisitos

- [x] CHK001 - ¿Está definido qué se muestra a una cuenta recién creada, sin ningún estudio cargado
      todavía? [Hueco, Spec §FR-038]
- [x] CHK002 - ¿Está definido qué se muestra cuando una búsqueda o un filtro no devuelve resultados, y
      cómo vuelve el usuario al listado completo desde ahí? [Hueco, Spec §FR-053, §FR-052]
- [x] CHK003 - ¿Están especificados requisitos de indicación de progreso durante una carga, dado que el
      propio spec admite hasta 15 segundos para 10 MB? [Hueco, Spec §SC-005]
- [x] CHK004 - ¿Está definido el control para retroceder de página, dado que el requisito de paginación
      solo exige un control para avanzar? [Hueco, Spec §FR-054]
- [x] CHK005 - ¿Está especificado si la búsqueda y los filtros aplicados se conservan al pasar de página
      y al volver desde el detalle de un estudio? [Hueco, Spec §FR-048b, §FR-054]
- [x] CHK006 - ¿Está definida la información que muestra la confirmación de borrado —en particular que la
      operación es irreversible y cuántos archivos alcanza—? [Hueco, Spec §FR-043, §FR-044]
- [x] CHK007 - ¿Están especificados los avisos de resultado tras una operación exitosa: estudio creado,
      metadatos guardados, estudio eliminado? [Hueco, Spec §FR-015, §FR-022, §FR-044]
- [x] CHK008 - ¿Está definido qué ve el usuario y qué ocurre con los datos que estaba escribiendo cuando
      la sesión expira en medio de una operación? [Hueco, Spec §FR-005, §FR-006]
- [x] CHK009 - ¿Está especificado si la aplicación ofrece o promueve su instalación, o si el requisito se
      limita a que el navegador pueda instalarla? [Hueco, Spec §FR-055]
- [x] CHK010 - ¿Está definido qué ofrece la pantalla sin conexión más allá del aviso, por ejemplo cómo
      reintentar o cómo volver una vez recuperada la señal? [Hueco, Spec §FR-056]
- [x] CHK011 - ¿Está especificado con qué nombre recibe el usuario un archivo al descargarlo?
      [Hueco, Spec §FR-041, §FR-034]
- [x] CHK012 - ¿Están definidas las longitudes máximas de los campos de texto y cómo se comunican al
      usuario antes de que las exceda? [Hueco, Spec §FR-015, §FR-020]
- [x] CHK013 - ¿Está especificado el orden en que se presentan los archivos y las etiquetas dentro del
      detalle de un estudio? [Hueco, Spec §FR-023, §FR-021]
- [x] CHK014 - ¿Están definidos requisitos de presentación para un archivo que el visor no logra mostrar
      pese a haber pasado la validación de carga? [Hueco, Spec §FR-039]

## Claridad y Ausencia de Ambigüedad

- [x] CHK015 - ¿Está definido qué cuenta como "pantalla o paso" —si un diálogo de confirmación o una
      validación fallida suman—, de modo que el máximo de tres sea inequívoco?
      [Ambigüedad, Spec §FR-024]
- [x] CHK016 - ¿Está definido "utilizable" a 360 píxeles más allá de la ausencia de desplazamiento
      horizontal, por ejemplo respecto de qué debe quedar visible sin desplazarse?
      [Ambigüedad, Spec §FR-059]
- [x] CHK017 - ¿Está definida la relación espacial que satisface "junto al campo o al archivo", de modo
      que pueda decidirse sin criterio personal si un mensaje la cumple? [Ambigüedad, Spec §FR-025]
- [x] CHK018 - ¿Está definido qué distingue "visualizar dentro de la aplicación" de abrir el archivo en
      otra pestaña o en el visor del sistema operativo? [Ambigüedad, Spec §FR-039]
- [x] CHK019 - ¿Está definido si "limpiar todos los filtros" alcanza también al término de búsqueda o
      solo a los filtros por fecha e institución? [Ambigüedad, Spec §FR-052]
- [ ] CHK020 - ¿Está especificado qué debe comunicar la pantalla sin conexión, o el requisito de indicarlo
      "explícitamente" admite cualquier redacción? [Claridad, Spec §FR-056]
- [x] CHK021 - ¿Está definido el formato en que se presentan las fechas y si es uniforme en listado,
      detalle y filtros? [Hueco, Spec §FR-017, §FR-038]
- [x] CHK022 - ¿Está definido dónde aparece el contador de resultados y qué informa cuando hay más de una
      página? [Claridad, Spec §FR-053, §FR-054]

## Consistencia entre Requisitos

- [ ] CHK023 - ¿Es consistente el criterio de ubicación de los mensajes entre el error de validación de un
      campo, el aviso de carga interrumpida y el aviso de cupo alcanzado, o cada uno queda librado a su
      propio requisito? [Consistencia, Spec §FR-025, §FR-058, §FR-037]
- [x] CHK024 - ¿Se aplica un mismo criterio de redacción y ubicación a los tres límites que el sistema
      informa —20 archivos por estudio, 50 MB por archivo, 20 GB de cupo—?
      [Consistencia, Spec §FR-023, §FR-027, §FR-037]
- [x] CHK025 - ¿Es consistente el requisito de no transferir contenido de archivos hasta que se lo pida
      con la presentación del detalle de un estudio, o falta declarar que el detalle no muestra vistas
      previas? [Consistencia, Spec §FR-042]
- [x] CHK026 - ¿Es consistente el máximo de tres pasos para crear un estudio con la exigencia de mostrar
      los errores de validación en su lugar, cuando un intento fallido obliga a repetir un paso?
      [Consistencia, Spec §FR-024, §FR-025]
- [x] CHK027 - ¿Es consistente la creación de un estudio sin archivos válidos con el flujo de tres pasos,
      respecto de qué se le informa al usuario al final de la operación?
      [Consistencia, Spec §FR-024, §FR-030b]

## Calidad y Medibilidad de los Criterios de Aceptación

- [x] CHK028 - ¿Están definidas las condiciones de medición de la ausencia de desplazamiento horizontal
      —alto de la ventana, orientación, tamaño de fuente base—, de modo que el criterio dé el mismo
      resultado en dos evaluaciones? [Medibilidad, Spec §AC-39]
- [x] CHK029 - ¿Puede evaluarse objetivamente el criterio de los tres pasos sin haber definido antes qué
      es un paso? [Medibilidad, Spec §AC-79, §FR-024]
- [x] CHK030 - ¿Es objetivamente verificable que el contenido "queda visible sin que el usuario deba
      descargarlo"? [Medibilidad, Spec §AC-15]
- [x] CHK031 - ¿Está definido el procedimiento de medición de los tiempos de tarea —quién los ejecuta, con
      qué colección, cuántas repeticiones—, o los criterios de 60 y 10 segundos quedan sin forma de
      darse por cumplidos? [Medibilidad, Spec §SC-001, §SC-002]
- [x] CHK032 - ¿Está definido cómo se comprueba que un mensaje aparece "junto" a su origen y no en un
      aviso general? [Medibilidad, Spec §AC-80]

## Cobertura de Escenarios y Casos Límite

- [x] CHK033 - ¿Está especificado si los metadatos ya escritos se conservan cuando el envío del formulario
      falla por validación? [Cobertura, Recuperación, Spec §FR-025]
- [x] CHK034 - ¿Están definidos requisitos para el listado con una sola página, donde los controles de
      paginación no tienen destino? [Cobertura, Caso límite, Spec §FR-054]
- [x] CHK035 - ¿Está especificado el comportamiento esperado cuando el mismo usuario tiene la aplicación
      abierta en dos dispositivos y edita el mismo estudio? [Cobertura, Escenario alterno, Spec §FR-022]
- [ ] CHK036 - ¿Están definidos requisitos para una conexión lenta pero no caída, distinta del corte que
      contempla el aviso de carga interrumpida? [Cobertura, Excepción, Spec §FR-058]
- [x] CHK037 - ¿Está especificado qué ve el usuario mientras el listado tarda en responder, dentro del
      margen de 2 segundos que el propio spec admite? [Cobertura, Spec §SC-004]
- [x] CHK038 - ¿Están definidos requisitos de presentación para un estudio con muchos archivos o con
      etiquetas numerosas, cerca de los límites que el sistema admite?
      [Cobertura, Caso límite, Spec §FR-023]

## Accesibilidad, Idioma y Presentación

- [x] CHK039 - ¿Está declarado si los requisitos de accesibilidad —navegación por teclado, foco visible,
      contraste, compatibilidad con lectores de pantalla— quedan deliberadamente fuera del alcance o
      simplemente faltan? [Hueco, Spec §FR-059]
- [x] CHK040 - ¿Está especificado el idioma de la interfaz y de los mensajes, y si se contempla más de
      uno? [Hueco]
- [x] CHK041 - ¿Están definidos requisitos sobre el texto de los mensajes de error dirigidos al usuario,
      compatibles con la prohibición de exponer datos de otras cuentas?
      [Completitud, Spec §FR-025, §FR-037]

## Dependencias y Supuestos

- [ ] CHK042 - ¿Está documentado qué se degrada, y cómo, en un navegador que no admite la instalación como
      aplicación? [Supuesto, Spec §FR-055]
- [x] CHK043 - ¿Está validado el supuesto de que no hacen falta enlaces marcables a un listado filtrado,
      ahora que la búsqueda no viaja en la dirección? [Supuesto, Spec §FR-048b]
- [x] CHK044 - ¿Está documentado el supuesto de que el usuario opera con una única cuenta por dispositivo,
      sin necesidad de cambiar de cuenta dentro de la aplicación? [Supuesto, Spec §FR-004]

## Notas

- Marcar un ítem como cumplido exige poder señalar el texto del spec que lo satisface; si hay que
  deducirlo, el ítem no pasa.
- Los ítems marcados `[Hueco]` esperan que el spec **no** tenga hoy ese requisito: cumplirlos implica
  agregarlo, o dejar asentado por qué queda deliberadamente fuera.
- CHK039 y CHK040 son el caso típico en que la respuesta correcta puede ser "fuera de alcance": el PRD no
  menciona accesibilidad ni idioma, y ampliarlo requiere modificar `PRD2.md` primero. Lo que el checklist
  exige no es implementarlos, sino que la decisión quede escrita.
