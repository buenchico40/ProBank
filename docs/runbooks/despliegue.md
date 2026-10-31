# Despliegue

1. `infra.yml` con el ambiente: revisar el `plan` y aprobar el `apply` en el ambiente `<amb>-apply`.
2. `deploy.yml`: publica el .dacpac (SqlPackage), la Api y la Web.
3. `e2e.yml` corre Playwright contra el ambiente y publica el reporte.
4. Registrar la versión desplegada en ProBank › Versiones.

## Reversión

Volver a desplegar la versión anterior desde el historial de `deploy.yml` y registrar la reversión con su motivo.
