# Configuración

**Propósito.** Administrar en un solo lugar la configuración global de la solución (correo, consultas dinámicas…) sin desplegar.

## Reglas

| ID | Regla |
|---|---|
| CFG-01 | Parámetros globales por categoría; los edita solo quien tiene el permiso; cada cambio queda en la auditoría con su valor anterior y el nuevo. |
| CFG-02 | Ningún secreto se escribe en la configuración: solo el nombre del secreto de Key Vault, con el prefijo de su uso (`smtp-`, `cnx-`). |
| CFG-03 | Correo por Microsoft 365 (Graph), SMTP del banco o carpeta local (solo desarrollo), con prueba de envío. |
| CFG-04 | Consultas dinámicas: días de conservación de los resultados guardados y tamaño máximo del adjunto en el correo. |

## Reglas de operación

- **Configuración › Parámetros generales**: una tarjeta por categoría. Cada parámetro tiene tipo (texto, número con rango, lista,
  correo o secreto), descripción y último cambio. Se guardan solo los valores que cambian; se valida el conjunto completo y los
  cambios se aplican de inmediato (la API los lee con una caché de un minuto, que la instancia que guarda invalida al momento).
  Si un parámetro no tiene valor, se usa el de la configuración del servidor (por ejemplo `Correo:BuzonServicio`).
- La definición de los parámetros (nombre, tipo, rango, opciones) viene del despliegue; el valor inicial solo se pone al crearlos, así
  un despliegue nuevo no pisa lo que cambió el administrador.
- **Correo** (ADR 0011): con Microsoft 365, el buzón de servicio es obligatorio. Con SMTP: servidor y remitente obligatorios; con
  usuario, el secreto de su clave (`smtp-…`) y cifrado StartTls o TlsDirecto; sin usuario, relay autorizado por IP. *Carpeta* solo
  si el servidor tiene `Correo:CarpetaLocal` (desarrollo). *Probar envío de correo* envía uno de prueba con lo guardado y queda
  en la auditoría, exitoso o con el motivo de la falla.
- **Consultas dinámicas**: los resultados guardados de las ejecuciones se borran pasados los días configurados (0 = no se borran);
  el programador revisa una vez por hora y deja «Resultado eliminado» en la bitácora de la ejecución (la auditoría se conserva).
  Si el archivo de un envío programado supera el máximo, el correo sale sin adjunto e indica la ejecución para descargarlo.
- Permiso `config` (ver y editar); al instalar solo lo tiene el Administrador.

## Datos

cfg.Parametro; procedimientos `cfg.usp_Parametro_Listar` y `cfg.usp_Parametro_Guardar`. Auditoría en aud.Auditoria (módulo Configuración).

## Pruebas

- Unitarias: `test/ProBank.UnitTests/Configuracion` (reglas de validación).
