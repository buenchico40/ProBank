# Portafolio

**Propósito.** Seguir proyectos, mejoras y bugs de TI con tareas, cronograma y aprobaciones.

## Reglas

| ID | Regla |
|---|---|
| PRY-01 | Gerencia de TI es el líder de proyecto. |
| PRY-02 | Cada ítem tiene uno o varios responsables. |
| PRY-03 | Desde el detalle del ítem se crea una tarea y se asigna a un técnico de cualquier área. |
| PRY-04 | Aprobadores por tarea: ninguno, uno o varios; si son varios, aprueban todos, en cualquier orden. Quien crea la tarea no la aprueba. |
| PRY-05 | Rechazar exige comentario y devuelve la tarea al técnico; al reenviarla empieza una ronda nueva. |
| PRY-06 | Avance del ítem = tareas completadas ÷ total de tareas. |
| PRY-07 | Cronograma Gantt por ítem, de todas las tareas, y filtrado al ítem desde su detalle. |
| PRY-08 | Acuerdos semanales por ítem con responsable y fecha de cumplimiento; vencen solos. |
| PRY-09 | Parametrización: qué roles ejecutan cada acción (crear ítem, editar tarea, exigir aprobación, registrar y cumplir acuerdos, eliminar borradores). |

## Reglas de operación

- El técnico asignado envía a aprobar y completa su tarea; otros usuarios necesitan poder editar tareas.
- Sin aprobadores, la tarea pasa directo a En curso. Solo se completa una tarea en curso.
- Los aprobadores solo cambian mientras la tarea está Pendiente. Cada cambio queda en el historial de la tarea.
- El responsable de un acuerdo puede marcarlo cumplido aunque su rol no tenga la función.
- Solo se eliminan borradores sin tareas ni revisiones; sus documentos adjuntos se quitan con ellos.
- Un ítem en Borrador no admite tareas ni acuerdos: la pantalla explica por qué el botón está deshabilitado.
- Ítems y tareas aceptan documentos adjuntos (hasta 20 cada uno) en Blob Storage, cada uno con su descripción obligatoria
  (máximo 300 caracteres, editable después). Los tipos y el tamaño máximo de cada uno
  se configuran en Parametrización (de inicio: ítems PDF, Word y Excel hasta 10 MB; tareas zip, sql, PDF y Word hasta 5 MB).
  El contenido de cada archivo se verifica contra su extensión. En una tarea adjunta su técnico o quien edita tareas.
- Los informes de seguimiento (semanales o mensuales) se generan con DevExpress Reporting: primero indicadores y gráficos
  (avance por ítem, acuerdos por estado, movimientos por día y eventos por tipo) y luego el detalle (resumen, bitácora,
  acuerdos y temas). El PDF se guarda como copia en Blob Storage y se ve o descarga desde la pantalla.

## Datos

pry.Item, pry.ItemResponsable, pry.Hito, pry.Tarea, pry.TareaAprobador, pry.Aprobacion, pry.TareaEvento, pry.Revision, pry.Acuerdo,
pry.Informe, pry.Accion, pry.AccionRol, pry.Adjunto, pry.ParametroAdjunto. Solo procedimientos almacenados (`<esquema>.usp_<Entidad>_<Accion>`).

## Pruebas

- Unitarias: `test/ProBank.UnitTests/Portafolio`.
- `test/e2e/specs/portafolio.spec.ts` con una etiqueta por requisito (por ejemplo `@PRY-01`).
