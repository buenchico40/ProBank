# Trazabilidad

Cada requisito apunta a su módulo, a la tabla principal y a la prueba que lo cubre (etiqueta Playwright).

| ID | Requisito | Módulo | Datos | Prueba |
|---|---|---|---|---|
| SEG-01 | Un usuario tiene varios roles y un rol predeterminado; cambia el rol activo desde su perfil. | Seguridad | seg.Usuario | `seguridad.spec.ts` @SEG-01 |
| SEG-02 | Autenticación con Microsoft Entra ID (OpenID Connect); ProBank nunca recibe la contraseña. | Seguridad | seg.Usuario | `seguridad.spec.ts` @SEG-02 |
| SEG-03 | Permisos por opción × acción y por función propia (mostrar contraseña, revocar sesiones). | Seguridad | seg.Usuario | `seguridad.spec.ts` @SEG-03 |
| SEG-04 | Cambio de IP en la sesión revoca la sesión. | Seguridad | seg.Usuario | `seguridad.spec.ts` @SEG-04 |
| SEG-05 | Auditoría general de toda acción, no editable ni eliminable. | Seguridad | seg.Usuario | `seguridad.spec.ts` @SEG-05 |
| COA-01 | Cada coactiva se ejecuta por orden judicial: bloqueo, levantamiento o transferencia. | Coactivas | coa.Oficio | `coactivas.spec.ts` @COA-01 |
| COA-02 | Un oficio trae varias coactivas de varios clientes y entidades, con órdenes distintas mezcladas. | Coactivas | coa.Oficio | `coactivas.spec.ts` @COA-02 |
| COA-03 | Se eligen los tipos de producto que se pueden bloquear (ahorros, corriente, plazo fijo, inversión). | Coactivas | coa.Oficio | `coactivas.spec.ts` @COA-03 |
| COA-04 | Carga masiva desde el formato Excel de `spec/formatos/`. | Coactivas | coa.Oficio | `coactivas.spec.ts` @COA-04 |
| COA-05 | Cobertura calculada: Sin bloqueo, Parcial o Total; montos siempre en moneda ($ 856,00). | Coactivas | coa.Oficio | `coactivas.spec.ts` @COA-05 |
| PRY-01 | Gerencia de TI es el líder de proyecto. | Portafolio | pry.Item | `portafolio.spec.ts` @PRY-01 |
| PRY-02 | Cada ítem tiene uno o varios responsables. | Portafolio | pry.Item | `portafolio.spec.ts` @PRY-02 |
| PRY-03 | Desde el detalle del ítem se crea una tarea y se asigna a un técnico de cualquier área. | Portafolio | pry.Item | `portafolio.spec.ts` @PRY-03 |
| PRY-04 | Aprobadores por tarea: ninguno, uno o varios; si son varios, aprueban todos, en cualquier orden. | Portafolio | pry.Item | `portafolio.spec.ts` @PRY-04 |
| PRY-05 | Rechazar exige comentario y devuelve la tarea al técnico. | Portafolio | pry.Item | `portafolio.spec.ts` @PRY-05 |
| PRY-06 | Avance del ítem = tareas completadas ÷ total de tareas. | Portafolio | pry.Item | `portafolio.spec.ts` @PRY-06 |
| PRY-07 | Cronograma Gantt por ítem, de todas las tareas, y filtrado al ítem desde su detalle. | Portafolio | pry.Item | `portafolio.spec.ts` @PRY-07 |
| PRY-08 | Acuerdos semanales por ítem con responsable y fecha de cumplimiento; vencen solos. | Portafolio | pry.Item | `portafolio.spec.ts` @PRY-08 |
| PRY-09 | Parametrización: qué roles ejecutan cada acción. | Portafolio | pry.Item | `portafolio.spec.ts` @PRY-09 |
| COM-01 | Borrador de presupuesto del año siguiente, copiado del vigente y aprobado después. | Compras y pagos TI | com.Partida | `compras.spec.ts` @COM-01 |
| COM-02 | Solicitudes de compra y renovaciones marcadas como Presupuestado o No presupuestado. | Compras y pagos TI | com.Partida | `compras.spec.ts` @COM-02 |
| COM-03 | Alertas de renovación por días de aviso. | Compras y pagos TI | com.Partida | `compras.spec.ts` @COM-03 |
| COM-04 | Pagos descuentan de la partida. | Compras y pagos TI | com.Partida | `compras.spec.ts` @COM-04 |
| CUM-01 | Controles con fecha de evaluación y estado calculado. | Cumplimiento y auditoría | cum.Control | `cumplimiento.spec.ts` @CUM-01 |
| CUM-02 | Hallazgos con plan de acción; vencen solos. | Cumplimiento y auditoría | cum.Control | `cumplimiento.spec.ts` @CUM-02 |
| CRE-01 | Certificados, cuentas técnicas y secretos con vencimiento y rotación. | Certificados y secretos | cred.GrupoAcceso | `credenciales.spec.ts` @CRE-01 |
| CRE-02 | Mostrar o copiar un valor es un permiso propio, solo para el grupo de acceso, y queda auditado. | Certificados y secretos | cred.GrupoAcceso | `credenciales.spec.ts` @CRE-02 |
| CRE-03 | Valores cifrados con Always Encrypted; CMK en Key Vault. | Certificados y secretos | cred.GrupoAcceso | `credenciales.spec.ts` @CRE-03 |
| MKT-01 | Promociones con vigencia, audiencia por rol y orden. | Marketing | mkt.Promocion | `marketing.spec.ts` @MKT-01 |
| MKT-02 | Comunicados estructurados (resumen, secciones, contacto) con tipo, vigencia y destacado. | Marketing | mkt.Promocion | `marketing.spec.ts` @MKT-02 |
| SQL-01 | Solo procedimientos almacenados; no se aceptan queries libres. | Consultas dinámicas | rpt.Conexion | `consultas.spec.ts` @SQL-01 |
| SQL-02 | Bases de datos configurables; la cadena se guarda en Key Vault y nunca se muestra; se elige una al ejecutar. | Consultas dinámicas | rpt.Conexion | `consultas.spec.ts` @SQL-02 |
| SQL-03 | Aprobadores por ejecución, opcionales; si son varios, aprueban todos; quien solicita no aprueba. | Consultas dinámicas | rpt.Conexion | `consultas.spec.ts` @SQL-03 |
| SQL-04 | Parámetros con valor fijo o variable: Hoy, Ayer, inicio y fin de semana, mes, mes anterior, trimestre y año; Año actual, Año anterior, Mes actual. | Consultas dinámicas | rpt.Conexion | `consultas.spec.ts` @SQL-04 |
| SQL-05 | Exportación a Excel, CSV, PDF, JSON y texto delimitado. | Consultas dinámicas | rpt.Conexion | `consultas.spec.ts` @SQL-05 |
| SQL-06 | Auditoría de cada solicitud, aprobación, rechazo, ejecución (exitosa o fallida) y exportación, con parámetros resueltos, filas, duración e IP. | Consultas dinámicas | rpt.Conexion | `consultas.spec.ts` @SQL-06 |
| SQL-07 | Programación diaria, semanal o mensual; las variables se resuelven en cada corrida. | Consultas dinámicas | rpt.Conexion | `consultas.spec.ts` @SQL-07 |
| SQL-08 | Envío por correo con Microsoft Graph desde un buzón de servicio a usuarios y grupos de Entra ID; archivos grandes van como enlace que vence. | Consultas dinámicas | rpt.Conexion | `consultas.spec.ts` @SQL-08 |
| VER-01 | Catálogo de aplicaciones y componentes con tipo, responsable y repositorio. | Versiones de aplicaciones y componentes | ver.Aplicacion | `versiones.spec.ts` @VER-01 |
| VER-02 | Versión mayor.menor.parche con notas, ligada a proyectos, mejoras y bugs del Portafolio. | Versiones de aplicaciones y componentes | ver.Aplicacion | `versiones.spec.ts` @VER-02 |
| VER-03 | Despliegues por ambiente; paso a Producción con aprobadores (si son varios, todos); reversión con motivo. | Versiones de aplicaciones y componentes | ver.Aplicacion | `versiones.spec.ts` @VER-03 |
| VER-04 | Dependencias con versión mínima; aviso si un despliegue rompe una dependencia. | Versiones de aplicaciones y componentes | ver.Aplicacion | `versiones.spec.ts` @VER-04 |
| REP-01 | Catálogo con ruta, parámetros y roles; publicar un reporte no requiere desplegar ProBank. | Reportes | rpt.Reporte | `reportes.spec.ts` @REP-01 |
