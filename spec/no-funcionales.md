# Requisitos no funcionales

| Área | Requisito |
|---|---|
| Identidad | Entra ID con OIDC (Microsoft.Identity.Web); MFA según política de Entra |
| Autorización | Permiso por opción y acción validado en la API y en los SP |
| Datos sensibles | Always Encrypted; CMK en Azure Key Vault |
| Conexiones de consultas | Cadena solo en Key Vault; usuario de solo lectura con EXECUTE en esquemas autorizados |
| Acceso a datos | Dapper con procedimientos almacenados; nunca SQL concatenado |
| Auditoría | Usuario, fecha y hora, IP y resultado de cada acción |
| Segregación | Quien solicita una ejecución o un paso a Producción no lo aprueba |
| Límites | Tiempo máximo y límite de filas por consulta; resultados grandes en la cola |
| Resiliencia | Polly (reintentos, cortacircuitos) hacia el core y servicios externos |
| Observabilidad | Azure Monitor y Application Insights |
| Usabilidad | DevExtreme, tema Fluent compacto, escritorio y teléfono |
| Rendimiento | Listados paginados en SP; el pool de conexiones no se satura con la carga esperada |
