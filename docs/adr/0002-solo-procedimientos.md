# Acceso a datos solo con procedimientos almacenados

- **Estado:** Aceptada
- **Fecha:** 2026-10

## Decisión

Dapper con CommandType.StoredProcedure; sin SQL dinámico ni ORM.

## Motivo

Permisos EXECUTE controlados por el DBA y menor superficie de inyección.

## Consecuencias

Cualquier excepción se registra como un ADR nuevo que reemplaza a este.
