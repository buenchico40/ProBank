# Consultas dinámicas

**Propósito.** Ejecutar procedimientos almacenados parametrizados y exportar o enviar sus resultados.

## Reglas

| ID | Regla |
|---|---|
| SQL-01 | Solo procedimientos almacenados; no se aceptan queries libres. |
| SQL-02 | Bases de datos configurables; la cadena se guarda en Key Vault y nunca se muestra; se elige una al ejecutar. |
| SQL-03 | Aprobadores por ejecución, opcionales; si son varios, aprueban todos; quien solicita no aprueba. |
| SQL-04 | Parámetros con valor fijo o variable: Hoy, Ayer, inicio y fin de semana, mes, mes anterior, trimestre y año; Año actual, Año anterior, Mes actual. |
| SQL-05 | Exportación a Excel, CSV, PDF, JSON y texto delimitado. |
| SQL-06 | Auditoría de cada solicitud, aprobación, rechazo, ejecución (exitosa o fallida) y exportación, con parámetros resueltos, filas, duración e IP. |
| SQL-07 | Programación diaria, semanal o mensual; las variables se resuelven en cada corrida. |
| SQL-08 | Envío por correo con Microsoft Graph desde un buzón de servicio a usuarios y grupos de Entra ID; archivos grandes van como enlace que vence. |

## Datos

rpt.Conexion, rpt.Consulta, rpt.Parametro, rpt.Ejecucion, rpt.EjecucionEvento, rpt.Programacion. Solo procedimientos almacenados (`<esquema>.usp_<Entidad>_<Accion>`).

## Pruebas

`test/e2e/specs/consultas.spec.ts` con una etiqueta por requisito (por ejemplo `@SQL-01`).
