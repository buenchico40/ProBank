# Marketing

**Propósito.** Imágenes promocionales y comunicados para el panel.

## Reglas

| ID | Regla |
|---|---|
| MKT-01 | Promociones con vigencia, audiencia por rol y orden. |
| MKT-02 | Comunicados estructurados (resumen, secciones, contacto) con tipo, vigencia y destacado. |

## Datos

mkt.Promocion, mkt.Comunicado. Solo procedimientos almacenados (`<esquema>.usp_<Entidad>_<Accion>`).

## Pruebas

`test/e2e/specs/marketing.spec.ts` con una etiqueta por requisito (por ejemplo `@MKT-01`).
