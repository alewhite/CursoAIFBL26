# Checklist de Calidad de la Especificación: MVP Mi Archivo Médico

**Propósito**: validar que la especificación está completa y es de calidad suficiente antes de pasar a la
planificación
**Creado**: 2026-08-19
**Feature**: [spec.md](../spec.md)

## Calidad del Contenido

- [x] Sin detalles de implementación (lenguajes, frameworks, APIs) — ver nota 1
- [x] Centrada en el valor para el usuario y la necesidad de negocio
- [x] Redactada para interlocutores no técnicos
- [x] Todas las secciones obligatorias completas

## Completitud de los Requisitos

- [x] No quedan marcadores [NEEDS CLARIFICATION]
- [x] Los requisitos son verificables y no ambiguos
- [x] Los criterios de éxito son medibles
- [x] Los criterios de éxito son independientes de la tecnología — ver nota 2
- [x] Todos los escenarios de aceptación están definidos
- [x] Los casos límite están identificados
- [x] El alcance está claramente acotado
- [x] Dependencias y supuestos identificados

## Preparación de la Feature

- [x] Todos los requisitos funcionales tienen criterios de aceptación claros — ver nota 3
- [x] Los escenarios de usuario cubren los flujos principales
- [x] La feature cumple los resultados medibles definidos en Criterios de Éxito
- [x] No se filtran detalles de implementación en la especificación

## Trazabilidad al PRD (Principio II de la constitución)

- [x] Los 34 RF vigentes de `PRD2.md` (revisión 3) están cubiertos por al menos un FR de la especificación
- [x] Los 67 RNF de `PRD2.md` (revisión 3) están cubiertos por al menos un FR o criterio de éxito
- [x] Los 99 AC vigentes de `PRD2.md` (revisión 3) aparecen en al menos un escenario de aceptación
- [x] Ningún identificador retirado (RF-06, RF-09, RF-18, RF-19, RF-25, RF-27, AC-32, AC-33, AC-81) se
      cita como vigente
- [x] La especificación no introduce ninguna capacidad ausente de `PRD2.md`
- [x] La sección "Fuera de Alcance" no contradice la de `PRD2.md`

## Notas

1. **Especificidad técnica heredada del PRD**: la especificación nombra AES-256, SHA-256, TLS 1.2,
   Argon2id/bcrypt/PBKDF2-HMAC-SHA256, GUID, cookies Secure/HttpOnly/SameSite y códigos HTTP 401/403/404.
   No son elecciones de implementación tomadas aquí: son el texto normativo de los RNF de `PRD2.md`, que
   fija esos valores porque la garantía de seguridad depende de ellos. Reescribirlos en lenguaje neutro
   rompería la trazabilidad y volvería no verificables los AC. La selección tecnológica que sí es
   decisión de diseño —framework web, acceso a datos, estructura del proyecto, forma del service
   worker— está deliberadamente ausente y se resuelve en `/speckit-plan`.
2. **SC-009** menciona SHA-256 por la misma razón: es el método de verificación que `RNF-18` define, no
   un detalle de implementación.
3. **FR-069 a FR-073** no tienen AC asociado: corresponden a los RNF que `PRD2.md` declara verificables
   por inspección documental, de configuración o de dependencias (RNF-10, RNF-37 a RNF-42, RNF-44 a
   RNF-50). El propio PRD documenta que esa ausencia es deliberada y no un hueco de cobertura.
4. Los ítems marcados incompletos requieren actualizar la especificación antes de `/speckit-clarify` o
   `/speckit-plan`. En esta validación no quedó ninguno.
