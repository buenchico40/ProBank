# Cumplimiento y auditoría

**Propósito.** Controles regulatorios, auditorías y planes de acción con vencimiento.

## Reglas

| ID | Regla |
|---|---|
| CUM-01 | Controles con fecha de evaluación y estado calculado. |
| CUM-02 | Hallazgos con plan de acción; vencen solos. |

## Datos

cum.Control, cum.Evaluacion, cum.Auditoria, cum.Hallazgo. Solo procedimientos almacenados (`<esquema>.usp_<Entidad>_<Accion>`).

## Pruebas

`test/e2e/specs/cumplimiento.spec.ts` con una etiqueta por requisito (por ejemplo `@CUM-01`).
