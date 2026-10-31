# Reportes

**Propósito.** Visualizar los reportes del servidor de reportes según el rol.

## Reglas

| ID | Regla |
|---|---|
| REP-01 | Catálogo con ruta, parámetros y roles; publicar un reporte no requiere desplegar ProBank. |

## Datos

rpt.Reporte, rpt.ReporteParametro, rpt.ReporteRol. Solo procedimientos almacenados (`<esquema>.usp_<Entidad>_<Accion>`).

## Pruebas

`test/e2e/specs/reportes.spec.ts` con una etiqueta por requisito (por ejemplo `@REP-01`).
