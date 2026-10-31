# Especificación viva de ProBank

La especificación manda: el código y las pruebas se escriben a partir de estos archivos.

## Cómo se escribe un spec

Cada módulo en `modulos/` sigue el mismo orden:

1. **Propósito**: una frase.
2. **Pantallas**: qué ve el usuario (la referencia visual está en `prototype/`).
3. **Reglas**: una por línea, con su ID (por ejemplo `PRY-04`).
4. **Datos**: tablas del esquema y procedimientos almacenados.
5. **Pruebas**: archivo Playwright y etiqueta del requisito.

Un cambio de regla entra primero aquí, luego en `trazabilidad.md`, luego en el código y la prueba, en el mismo PR.

## Índice

| Archivo | Contenido |
|---|---|
| [00-vision.md](00-vision.md) | Problema, valor y alcance |
| [01-roles-permisos.md](01-roles-permisos.md) | Roles, rol activo y permisos |
| [modulos/](modulos/) | Un archivo por módulo |
| [trazabilidad.md](trazabilidad.md) | Requisito → pantalla → datos → prueba |
| [no-funcionales.md](no-funcionales.md) | Seguridad, auditoría, rendimiento |
| [modelo-datos/](modelo-datos/) | Diagramas ER por esquema (Mermaid) |
| [formatos/](formatos/) | Formato Excel de carga masiva de coactivas |
