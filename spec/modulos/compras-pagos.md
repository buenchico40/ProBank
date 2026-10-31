# Compras y pagos TI

**Propósito.** Cruzar presupuesto de TI, compras, renovaciones y pagos.

## Reglas

| ID | Regla |
|---|---|
| COM-01 | Borrador de presupuesto del año siguiente, copiado del vigente y aprobado después. |
| COM-02 | Solicitudes de compra y renovaciones marcadas como Presupuestado o No presupuestado. |
| COM-03 | Alertas de renovación por días de aviso. |
| COM-04 | Pagos descuentan de la partida. |

## Datos

com.Partida, com.Movimiento, com.Solicitud, com.Renovacion, com.Pago. Solo procedimientos almacenados (`<esquema>.usp_<Entidad>_<Accion>`).

## Pruebas

`test/e2e/specs/compras.spec.ts` con una etiqueta por requisito (por ejemplo `@COM-01`).
