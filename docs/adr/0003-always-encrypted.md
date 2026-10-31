# Always Encrypted con CMK en Key Vault

- **Estado:** Aceptada
- **Fecha:** 2026-10

## Decisión

Columnas sensibles cifradas con RANDOMIZED; la llave maestra vive en Key Vault.

## Motivo

Ni el DBA ve los valores en claro.

## Consecuencias

Cualquier excepción se registra como un ADR nuevo que reemplaza a este.
