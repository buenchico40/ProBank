# Versiones de aplicaciones y componentes

**Propósito.** Saber qué versión de cada componente está en cada ambiente y qué incluye.

## Reglas

| ID | Regla |
|---|---|
| VER-01 | Catálogo de aplicaciones y componentes con tipo, responsable y repositorio. |
| VER-02 | Versión mayor.menor.parche con notas, ligada a proyectos, mejoras y bugs del Portafolio. |
| VER-03 | Despliegues por ambiente; paso a Producción con aprobadores (si son varios, todos); reversión con motivo. |
| VER-04 | Dependencias con versión mínima; aviso si un despliegue rompe una dependencia. |

## Datos

ver.Aplicacion, ver.Componente, ver.Version, ver.VersionItem, ver.Despliegue, ver.Dependencia. Solo procedimientos almacenados (`<esquema>.usp_<Entidad>_<Accion>`).

## Pruebas

`test/e2e/specs/versiones.spec.ts` con una etiqueta por requisito (por ejemplo `@VER-01`).
