# Marketing

**Propósito.** Imágenes promocionales y comunicados para el Panel y la pantalla de inicio de sesión.

## Reglas

| ID | Regla |
|---|---|
| MKT-01 | Promociones con vigencia, audiencia por rol y orden. |
| MKT-02 | Comunicados estructurados (resumen, secciones, contacto) con tipo, vigencia y destacado. |

## Reglas de operación

- Ciclo: **Borrador** → *Publicar* → **Publicado** → *Retirar* → **Retirado** (se puede corregir y volver a publicar).
  Lo publicado se muestra como **Programado** antes de su vigencia, **Publicado** durante ella y **Vencido** después.
- Publicar exige la vigencia completa (desde y hasta) que no haya terminado; la imagen promocional exige además su imagen.
- Solo se eliminan borradores; lo publicado se retira (queda para auditoría).
- Audiencia: «Todos» o un rol activo. Administrador y Marketing ven todo lo vigente para revisar lo publicado.
- Imagen promocional: JPG o PNG hasta 2 MB (Panel: 1200 × 300 px, 4:1; inicio de sesión: 1920 × 1080 px, 16:9); se verifica que el contenido sea
  realmente una imagen. Se guarda en Blob Storage, contenedor `marketing`. Texto alternativo y enlace https opcionales.
- Ubicación **Panel principal**: carrusel del Panel (440 px) en el orden indicado; la imagen se ve completa arriba y el título,
  la vigencia y el enlace van en su propia franja, sin taparla.
  Ubicación **Inicio de sesión**: fondo a pantalla completa con el ingreso centrado y el título en una sección debajo; solo si está dirigida a «Todos».
- Comunicado: resumen (tarjeta del Panel, máx. 300), secciones con el editor enriquecido y contacto. Los destacados van
  primero; la lectura muestra resumen, vigencia, audiencia, secciones y contacto.
- Permisos: ver, crear, editar, eliminar (borradores) y **aprobar** para publicar y retirar.

## Datos

mkt.Promocion, mkt.Comunicado; funciones mkt.fn_EstadoVigente y mkt.fn_VisiblePara. Solo procedimientos almacenados
(`<esquema>.usp_<Entidad>_<Accion>`).

## Pruebas

- Unitarias: `test/ProBank.UnitTests/Marketing`.
- `test/e2e/specs/marketing.spec.ts` con una etiqueta por requisito (`@MKT-01`, `@MKT-02`).
