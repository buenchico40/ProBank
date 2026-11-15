# Firma electrónica de decisiones y PDF sellado por el banco

- **Estado:** Aceptada
- **Fecha:** 2026-11

## Decisión

Las aprobaciones del módulo Decisiones (DEC-03, DEC-04 y DEC-07) usan **firma electrónica en ProBank** y el PDF exportado lo firma
el **certificado del banco**; no se pide un certificado de firma a cada aprobador.

- **Firma del aprobador:** vuelve a ingresar su clave (la API la verifica; una clave incorrecta no firma y queda en la auditoría) y
  la base guarda un sello SHA-256 que une la huella del contenido de la versión, su usuario, el resultado, la fecha y hora, el
  comentario y la IP (`dec.fn_Sello`).
- **Contenido sellado:** al enviar la decisión se fija la huella SHA-256 de la información registrada, sin los archivos (`dec.fn_Huella`). Se puede verificar en
  cualquier momento: si alguien la cambia por fuera de ProBank, la huella y los sellos dejan de coincidir y la decisión se muestra
  «Alterada». Solo un aprobador la modifica dentro de ProBank, y eso crea una versión nueva: las firmas anteriores quedan en el
  historial y todos vuelven a firmar.
- **PDF:** DevExpress Reporting lo firma con el certificado `firma-documentos` de Key Vault (PAdES con la firma del lector de PDF);
  cualquier cambio al archivo invalida la firma.

## Motivo

Da integridad y no repudio razonables sin que cada aprobador gestione un certificado o un token, y deja la verificación dentro de
ProBank (huella, sellos, trazabilidad y auditoría).

## Consecuencias

- El valor probatorio descansa en la autenticación de ProBank y en la auditoría; si una decisión exige firma electrónica
  calificada de cada persona, se registra un ADR nuevo (firma con el certificado de cada aprobador).
- Terraform crea `firma-documentos` autofirmado para que el ambiente funcione desde el primer despliegue; para validez ante
  terceros, operaciones importa con ese nombre el certificado emitido por una entidad acreditada.
- En local, sin Key Vault, la API usa un certificado autofirmado de desarrollo (o el PKCS#12 en base64 de `Secretos:firma-documentos`).
