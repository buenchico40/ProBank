# Coactivas

**Propósito.** Registrar oficios judiciales y ejecutar cada orden por coactiva.

## Reglas

| ID | Regla |
|---|---|
| COA-01 | Cada coactiva se ejecuta por orden judicial: bloqueo, levantamiento o transferencia. Cada movimiento queda con el oficio que lo ordena. |
| COA-02 | Un oficio trae varias coactivas de varios clientes y entidades, con órdenes distintas mezcladas. |
| COA-03 | Se eligen los tipos de producto que se pueden bloquear (ahorros, corriente, plazo fijo, inversión); vacío = todos. |
| COA-04 | Carga masiva desde el formato Excel de `spec/formatos/` (hoja «Coactivas» desde la fila 6). Si una fila tiene errores, su oficio no se carga. |
| COA-05 | Cobertura calculada con lo bloqueado vigente más lo transferido: Sin bloqueo, Parcial o Total; montos siempre en moneda ($ 856,00). |

## Reglas de ejecución

- Un bloqueo no supera lo que falta cubrir de la coactiva ni lo disponible en la cuenta (saldo referencial menos lo bloqueado por todas las coactivas).
- Levantamiento y transferencia: solo sobre bloqueos vigentes y hasta el monto ordenado (vacío = total). La transferencia exige la cuenta de la entidad y descuenta el saldo referencial.
- Anular un oficio anula sus coactivas; anular una coactiva libera sus bloqueos vigentes (queda en el historial).
- El proceso es único por entidad. El RUC de la entidad debe estar en el catálogo (Oficios › Entidades).
- Saldos y bloqueos se registran manualmente: en esta fase no hay conexión al core bancario.

## Permisos

Oficios: ver, crear, editar (agregar coactivas, entidades), eliminar (anular), carga. Coactivas: ver, editar (monto, productos, productos del cliente),
eliminar (anular) y las funciones bloquear, levantar y transferir. Coactivas por cliente: ver.

## Datos

coa.Entidad, coa.Cliente, coa.Producto, coa.Oficio, coa.Coactiva, coa.OrdenCoactiva, coa.Bloqueo, coa.Movimiento; vistas coa.vw_Bloqueo,
coa.vw_Coactiva, coa.vw_Orden y coa.vw_Oficio. Solo procedimientos almacenados (`<esquema>.usp_<Entidad>_<Accion>`).

## Pruebas

- Unitarias: `test/ProBank.UnitTests/Coactivas`.
- `test/e2e/specs/coactivas.spec.ts` con una etiqueta por requisito (por ejemplo `@COA-01`).
