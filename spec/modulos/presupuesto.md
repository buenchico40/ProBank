# Presupuesto

**Propósito.** Cruzar el presupuesto de cada área del banco con sus compras, renovaciones y pagos. Las áreas se configuran
en *Presupuesto › Áreas del presupuesto*; inicialmente solo existe **TI** (Tecnología de la información).

## Reglas

| ID | Regla |
|---|---|
| COM-01 | Borrador de presupuesto del año siguiente, copiado del vigente y aprobado después. |
| COM-02 | Solicitudes de compra y renovaciones marcadas como Presupuestado o No presupuestado. |
| COM-03 | Alertas de renovación por días de aviso. |
| COM-04 | Pagos descuentan de la partida. |

## Reglas de operación

- **Áreas del presupuesto** (com.Area): código de 2 a 30 letras mayúsculas o números (TI, MKT), que no cambia, y nombre único.
  Un área **inactiva** conserva sus partidas y su ejecución, pero no recibe partidas nuevas ni prepara borrador; las áreas
  activas aparecen en el selector de *Presupuesto por área* (deshabilitado mientras haya una sola).
- **Partidas** (código `PRE-<año>-NN`, un rubro por año y área; el área y el año no cambian): **Borrador** → *Aprobar* → **Activo** → *Cerrar* → **Cerrado**.
  Solo el borrador se edita o elimina (si no tiene uso). *Preparar año siguiente* copia las partidas activas del último año
  del área como borrador del año siguiente, con el mismo asignado (COM-01).
- **Cifras de la partida**: Ajustes = movimientos aprobados que entran − los que salen; Comprometido = lo que falta pagar de
  las solicitudes presupuestadas aprobadas; Pagado = distribución de los pagos marcados como Pagado;
  Disponible = Asignado + Ajustes − Comprometido − Pagado; Ejecución = (Pagado + Comprometido) ÷ (Asignado + Ajustes).
- **Movimientos** (`MOV-NNNN`): Reasignación (origen y destino distintos), Incremento (solo destino) o Reducción (solo origen),
  con motivo y fecha no futura. Quedan **Pendiente** → *Aprobar* / *Rechazar* (con motivo); solo lo pendiente se corrige o anula.
  Ningún movimiento, solicitud ni pago puede dejar una partida con disponible negativo.
- **Renovaciones** (`REN-NNNN`): servicio, proveedor, vencimiento, monto estimado y días de aviso por servicio (1 a 365).
  Estado calculado: **Vigente**, **Por vencer** (dentro de los días de aviso), **Vencido**, **Requerimiento enviado** o **Retirado** (COM-03).
  *Enviar requerimiento* crea la solicitud de compra; *Registrar renovación* fija el nuevo vencimiento y vuelve a avisar.
- **Solicitudes de compra** (`SC-NNNN`): presupuestada con partida activa o no presupuestada sin partida (COM-02).
  **Pendiente** → *Aprobar* / *Rechazar*; al aprobarse queda como comprometido. Estado: Pendiente, Rechazada, En curso, Completado o Anulado.
  El contrato (PDF o Word) se adjunta a la solicitud.
- **Pagos** (`PAG-NNNN`) de una solicitud aprobada, sin superar lo que falta pagar. La distribución por partida no repite
  partidas; para marcarlo **Pagado** debe cuadrar con el monto (si es presupuestada) y la fecha no puede ser futura (un pago
  futuro queda programado). Al pagarse descuenta de las partidas (COM-04). Facturas (PDF o XML) y documento de
  distribución (Excel, CSV o PDF) hasta 10 MB, verificados por su contenido.
- **Reporte PDF** (*Presupuesto por área › Reporte PDF*, permiso `presupuesto`/`exportar`, DevExpress Reporting) del área y el año
  elegidos: indicadores (presupuestado, comprometido, pagado, disponible y pagos no presupuestados), ejecución por partida y pagos
  por mes; luego el detalle de **partidas** (asignado, ajustes, presupuestado, comprometido, pagado, disponible), **servicios**
  (estimado frente a pagado, por pagar y estado) y **pagos** del año, con sus totales. Los servicios y pagos no presupuestados no
  tienen partida ni área: se incluyen los del año. Cada generación queda en la auditoría (`com.usp_Presupuesto_Reporte`).
- Permisos por opción (`presupuesto`, `movimientos`, `renovaciones`, `solicitudes`, `pagos`, `areaspres`): ver, crear, editar,
  eliminar y **aprobar**. `areaspres` se concede al instalarse a los roles que aprueban el presupuesto (y al Administrador).

## Datos

com.Area, com.Partida, com.Movimiento, com.Renovacion, com.Solicitud, com.Pago, com.PagoDistribucion, com.Documento; vistas
com.vw_Partida y com.vw_Solicitud. Documentos en Blob Storage, contenedor `adjuntos`. Solo procedimientos almacenados
(`<esquema>.usp_<Entidad>_<Accion>`).

## Pruebas

- Unitarias: `test/ProBank.UnitTests/Presupuesto`.
- `test/e2e/specs/presupuesto.spec.ts` con una etiqueta por requisito (por ejemplo `@COM-01`).
