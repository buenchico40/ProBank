# Uso de IA y evidencia de revisión

## Usos esperados

| Uso | Cómo se aplicó |
|---|---|
| Generación de código | Maqueta DevExtreme, formato Excel y esqueleto de esta solución |
| Refactorización | Grupo aprobador único → aprobadores por tarea |
| Depuración | Formato de montos, buscador sobre la grilla, campos superpuestos |
| Documentación | Arquitectura, especificación y presentación |
| Pruebas | Recorridos Playwright con capturas antes de publicar |
| Diseño arquitectónico | Arquitectura física y lógica, solución .NET, modelo de datos |

## Evidencias

- `prompts/`: pedidos en orden (exportados de la conversación del proyecto).
- `decisiones.md`: propuestas aceptadas, corregidas y descartadas.
- `capturas/`: capturas de cada flujo probado.
- Los diffs viven en el historial de git; cada PR declara qué se aceptó y qué se corrigió.

## Criterio humano

| Propuesta de la IA | Decisión | Corrección y motivo |
|---|---|---|
| Active Directory | Corregida | Entra ID: identidad central en la nube |
| Bloqueo por «cuartilla» | Corregida | Era «coactiva»: error de dictado |
| Gerencia de TI y Líder de proyecto separados | Corregida | Es el mismo rol |
| Técnico limitado a su área | Corregida | Tareas de cualquier área |
| Un grupo aprobador único | Corregida | Aprobadores por tarea; si son varios, todos |
| Queries libres o procedimientos | Restringida | Solo procedimientos almacenados |
| Aprobar la publicación de consultas | Corregida | Aprobación por ejecución, opcional |
| Fechas fijas en parámetros | Ampliada | Variables resueltas al ejecutar |
| Cronograma general desde el ítem | Corregida | Solo el ítem seleccionado |
