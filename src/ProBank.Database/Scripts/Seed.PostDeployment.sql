-- Datos base idempotentes: roles, opciones del menú con sus acciones, parámetros de acceso y permisos iniciales.
MERGE seg.Rol AS t
USING (VALUES ('Administrador', N'Acceso a todos los módulos y a la administración de seguridad'),
              ('Gerencia de TI', N'Portafolio, aprobaciones, compras y reportes de TI'),
              ('Analista de coactivas', N'Oficios, coactivas y bloqueo de fondos'),
              ('Compras TI', N'Solicitudes de compra y pagos'),
              ('Cumplimiento', N'Controles regulatorios, auditorías y hallazgos'),
              ('Seguridad de la información', N'Certificados, cuentas técnicas, secretos y auditoría'),
              ('Marketing', N'Imágenes promocionales y comunicados'),
              ('Consulta', N'Solo lectura de reportes')) AS s (Nombre, Descripcion)
ON t.Nombre = s.Nombre
WHEN MATCHED AND t.Descripcion IS NULL THEN UPDATE SET Descripcion = s.Descripcion
WHEN NOT MATCHED THEN INSERT (Nombre, Descripcion) VALUES (s.Nombre, s.Descripcion);

-- SEG-03: opciones del menú (mismas claves que el prototipo y que el menú de la Web)
MERGE seg.Opcion AS t
USING (VALUES
    ('inicio', N'Inicio', N'Inicio / Panel', 1),
    ('usuarios', N'Seguridad', N'Usuarios', 10), ('roles', N'Seguridad', N'Roles y permisos', 11),
    ('params', N'Seguridad', N'Parámetros de acceso', 12), ('auditoria', N'Seguridad', N'Auditoría', 13),
    ('oficios', N'Coactivas', N'Oficios', 20), ('coactivas', N'Coactivas', N'Coactivas', 21), ('clientes', N'Coactivas', N'Coactivas por cliente', 22),
    ('proyectos', N'Portafolio', N'Proyectos, mejoras y bugs', 30), ('gantt', N'Portafolio', N'Cronograma (Gantt)', 31),
    ('tareas', N'Portafolio', N'Mis tareas', 32), ('aprobaciones', N'Portafolio', N'Aprobaciones', 33),
    ('acuerdos', N'Portafolio', N'Acuerdos semanales', 34), ('informes', N'Portafolio', N'Informes de seguimiento', 35),
    ('paramproy', N'Portafolio', N'Parametrización', 36),
    ('presupuesto', N'Presupuesto', N'Presupuesto por área', 40), ('movimientos', N'Presupuesto', N'Ajustes y movimientos', 41),
    ('renovaciones', N'Presupuesto', N'Renovaciones de servicios', 42), ('solicitudes', N'Presupuesto', N'Solicitudes de compra', 43),
    ('pagos', N'Presupuesto', N'Pagos', 44), ('areaspres', N'Presupuesto', N'Áreas del presupuesto', 45),
    ('controles', N'Cumplimiento y auditoría', N'Controles regulatorios', 50), ('auditorias', N'Cumplimiento y auditoría', N'Auditorías', 51),
    ('hallazgos', N'Cumplimiento y auditoría', N'Hallazgos y planes de acción', 52),
    ('grupos', N'Bóveda', N'Grupos de acceso', 60), ('certificados', N'Bóveda', N'Certificados digitales', 61),
    ('cuentas', N'Bóveda', N'Cuentas técnicas', 62), ('secretos', N'Bóveda', N'Secretos', 63),
    ('promos', N'Marketing', N'Imágenes promocionales', 70), ('comunicados', N'Marketing', N'Comunicados', 71),
    ('consultas', N'Consultas dinámicas', N'Catálogo de consultas', 80), ('ejecuciones', N'Consultas dinámicas', N'Ejecuciones y aprobaciones', 81),
    ('audejec', N'Consultas dinámicas', N'Auditoría de ejecuciones', 82), ('conexiones', N'Consultas dinámicas', N'Bases de datos', 83),
    ('reportes', N'Reportes', N'Reportería por módulo', 90), ('catreportes', N'Reportes', N'Catálogo de reportes', 91),
    ('config', N'Configuración', N'Parámetros generales', 95)
) AS s (Opcion, Modulo, Nombre, Orden)
ON t.Opcion = s.Opcion
WHEN MATCHED THEN UPDATE SET Modulo = s.Modulo, Nombre = s.Nombre, Orden = s.Orden
WHEN NOT MATCHED THEN INSERT (Opcion, Modulo, Nombre, Orden) VALUES (s.Opcion, s.Modulo, s.Nombre, s.Orden);

-- Acciones genéricas para todas las opciones y funciones propias (cada función es un permiso)
DECLARE @Nuevas TABLE (Operacion NVARCHAR(10), Opcion VARCHAR(60), Accion VARCHAR(60), Tipo VARCHAR(10));
MERGE seg.OpcionAccion AS t
USING (
    SELECT o.Opcion, a.Accion, a.Nombre, 'Accion' AS Tipo, a.Orden
    FROM seg.Opcion o
    CROSS JOIN (VALUES ('ver', N'Ver', 1), ('crear', N'Crear', 2), ('editar', N'Editar', 3), ('eliminar', N'Eliminar / Anular', 4),
                       ('exportar', N'Exportar', 5), ('aprobar', N'Aprobar', 6)) AS a (Accion, Nombre, Orden)
    UNION ALL
    SELECT f.Opcion, f.Accion, f.Nombre, 'Funcion', f.Orden
    FROM (VALUES ('usuarios', 'revocar', N'Revocar sesiones', 10), ('usuarios', 'clave', N'Restablecer contraseña', 11),
                 ('usuarios', 'desbloquear', N'Desbloquear usuario', 12),
                 ('oficios', 'carga', N'Carga masiva (Excel)', 10),
                 ('coactivas', 'bloquear', N'Bloquear fondos', 10), ('coactivas', 'levantar', N'Levantar bloqueo', 11),
                 ('coactivas', 'transferir', N'Transferir fondos a la entidad', 12),
                 ('tareas', 'enviar', N'Enviar a aprobar', 10), ('acuerdos', 'cumplir', N'Marcar cumplido', 10),
                 ('informes', 'generar', N'Generar informe', 10),
                 ('cuentas', 'mostrar', N'Mostrar contraseña', 10), ('cuentas', 'copiar', N'Copiar contraseña', 11),
                 ('secretos', 'mostrar', N'Mostrar valor', 10), ('secretos', 'copiar', N'Copiar valor', 11)) AS f (Opcion, Accion, Nombre, Orden)
) AS s
ON t.Opcion = s.Opcion AND t.Accion = s.Accion
WHEN MATCHED THEN UPDATE SET Nombre = s.Nombre, Tipo = s.Tipo, Orden = s.Orden
WHEN NOT MATCHED THEN INSERT (Opcion, Accion, Nombre, Tipo, Orden) VALUES (s.Opcion, s.Accion, s.Nombre, s.Tipo, s.Orden)
OUTPUT $action, inserted.Opcion, inserted.Accion, inserted.Tipo INTO @Nuevas;

-- Funciones que aparecen en una versión nueva: se conceden a los roles que ya pueden editar esa opción
-- (como en el prototipo: la función sigue a la acción base). Las que un administrador quite después no se vuelven a dar.
INSERT seg.Permiso (RolId, Opcion, Accion)
SELECT p.RolId, n.Opcion, n.Accion
FROM @Nuevas n JOIN seg.Permiso p ON p.Opcion = n.Opcion AND p.Accion = 'editar'
WHERE n.Operacion = 'INSERT' AND n.Tipo = 'Funcion'
  AND NOT EXISTS (SELECT 1 FROM seg.Permiso x WHERE x.RolId = p.RolId AND x.Opcion = n.Opcion AND x.Accion = n.Accion);

-- Áreas del presupuesto: la opción nueva se concede (una sola vez) a los roles que aprueban el presupuesto; el Administrador
-- la recibe con todas las demás. Luego se administra en Seguridad › Roles y permisos.
IF NOT EXISTS (SELECT 1 FROM seg.Permiso WHERE Opcion = 'areaspres')
    INSERT seg.Permiso (RolId, Opcion, Accion)
    SELECT DISTINCT p.RolId, a.Opcion, a.Accion
    FROM seg.Permiso p JOIN seg.Rol r ON r.RolId = p.RolId AND r.Nombre <> 'Administrador'
    JOIN seg.OpcionAccion a ON a.Opcion = 'areaspres' AND a.Accion IN ('ver', 'crear', 'editar', 'exportar')
    WHERE p.Opcion = 'presupuesto' AND p.Accion = 'aprobar';

-- Presupuesto: áreas iniciales (por ahora solo TI); solo se insertan, luego se administran en Presupuesto › Áreas del presupuesto
MERGE com.Area AS t
USING (VALUES ('TI', N'Tecnología de la información')) AS s (Codigo, Nombre)
ON t.Codigo = s.Codigo
WHEN NOT MATCHED THEN INSERT (Codigo, Nombre) VALUES (s.Codigo, s.Nombre);

-- CFG-01: definición de los parámetros globales; el valor inicial solo se pone al crearlos, luego se administra en
-- Configuración › Parámetros generales (un despliegue nuevo no pisa lo que cambió el administrador).
MERGE cfg.Parametro AS t
USING (VALUES
    ('Correo.Proveedor', N'Correo', N'Proveedor de correo', N'Microsoft 365 (Graph) desde el buzón de servicio; SMTP del banco; Carpeta solo en desarrollo (deja .eml en una carpeta del equipo).',
        'Lista', N'Graph,Smtp,Carpeta', NULL, NULL, NULL, N'Graph', 1),
    ('Correo.BuzonServicio', N'Correo', N'Buzón de servicio (Microsoft 365)', N'Desde qué buzón envía Graph. La identidad de la API necesita Mail.Send limitado a este buzón.',
        'Correo', NULL, NULL, NULL, NULL, NULL, 2),
    ('Correo.Smtp.Servidor', N'Correo', N'Servidor SMTP', N'Nombre o IP del servidor de correo del banco.', 'Texto', NULL, NULL, NULL, NULL, NULL, 3),
    ('Correo.Smtp.Puerto', N'Correo', N'Puerto SMTP', N'587 con STARTTLS, 465 con TLS directo, 25 para un relay interno.', 'Numero', NULL, 1, 65535, NULL, N'587', 4),
    ('Correo.Smtp.Seguridad', N'Correo', N'Cifrado SMTP', N'Ninguna solo para un relay interno sin usuario.', 'Lista', N'StartTls,TlsDirecto,Ninguna', NULL, NULL, NULL, N'StartTls', 5),
    ('Correo.Smtp.Usuario', N'Correo', N'Usuario SMTP', N'Vacío: relay autorizado por IP, sin usuario ni clave.', 'Texto', NULL, NULL, NULL, NULL, NULL, 6),
    ('Correo.Smtp.SecretoClave', N'Correo', N'Secreto de Key Vault con la clave SMTP', N'Solo el nombre del secreto (empieza con smtp-); la clave la carga operaciones en Key Vault.',
        'Secreto', NULL, NULL, NULL, 'smtp-', NULL, 7),
    ('Correo.Smtp.Remitente', N'Correo', N'Remitente SMTP', N'Correo desde el que salen los envíos por SMTP.', 'Correo', NULL, NULL, NULL, NULL, NULL, 8),
    ('Correo.NombreRemitente', N'Correo', N'Nombre del remitente', N'Nombre que ven los destinatarios.', 'Texto', NULL, NULL, NULL, NULL, N'ProBank', 9),
    ('Consultas.DiasConservacionResultados', N'Consultas dinámicas', N'Días que se conservan los resultados',
        N'Los resultados guardados de las ejecuciones (para verlos y exportarlos) se borran después de estos días; la auditoría se conserva. 0 = no se borran.',
        'Numero', NULL, 0, 3650, NULL, N'90', 20),
    ('Consultas.MaximoMBAdjunto', N'Consultas dinámicas', N'Tamaño máximo del adjunto en el correo (MB)',
        N'Si el archivo de un envío programado lo supera, el correo sale sin adjunto e indica la ejecución para descargarlo desde ProBank.',
        'Numero', NULL, 1, 25, NULL, N'10', 21)
) AS s (Clave, Categoria, Nombre, Descripcion, Tipo, Opciones, Minimo, Maximo, Prefijo, Valor, Orden)
ON t.Clave = s.Clave
WHEN MATCHED THEN UPDATE SET Categoria = s.Categoria, Nombre = s.Nombre, Descripcion = s.Descripcion, Tipo = s.Tipo, Opciones = s.Opciones,
    Minimo = s.Minimo, Maximo = s.Maximo, Prefijo = s.Prefijo, Orden = s.Orden
WHEN NOT MATCHED THEN INSERT (Clave, Categoria, Nombre, Descripcion, Tipo, Opciones, Minimo, Maximo, Prefijo, Valor, Orden)
    VALUES (s.Clave, s.Categoria, s.Nombre, s.Descripcion, s.Tipo, s.Opciones, s.Minimo, s.Maximo, s.Prefijo, s.Valor, s.Orden);

-- Parámetros de acceso: solo se insertan; luego se administran en Seguridad › Parámetros de acceso
MERGE seg.ParametroAcceso AS t
USING (VALUES ('IntentosFallidos', N'5', N'Intentos fallidos antes de bloquear la cuenta', 3, 10, 1),
              ('MinutosBloqueo', N'15', N'Minutos de bloqueo de la cuenta', 5, 1440, 2),
              ('MinutosSesion', N'60', N'Duración de la sesión (minutos)', 15, 480, 3)) AS s (Clave, Valor, Nombre, Minimo, Maximo, Orden)
ON t.Clave = s.Clave
WHEN MATCHED THEN UPDATE SET Nombre = s.Nombre, Minimo = s.Minimo, Maximo = s.Maximo, Orden = s.Orden
WHEN NOT MATCHED THEN INSERT (Clave, Valor, Nombre, Minimo, Maximo, Orden) VALUES (s.Clave, s.Valor, s.Nombre, s.Minimo, s.Maximo, s.Orden);

-- Adjuntos de Portafolio: tipos y tamaño máximo iniciales; solo se insertan, luego se administran en Portafolio › Parametrización
MERGE pry.ParametroAdjunto AS t
USING (VALUES ('Item', '.pdf,.doc,.docx,.xls,.xlsx', 10),
              ('Tarea', '.zip,.sql,.pdf,.doc,.docx', 5)) AS s (Ambito, Extensiones, MaximoMB)
ON t.Ambito = s.Ambito
WHEN NOT MATCHED THEN INSERT (Ambito, Extensiones, MaximoMB) VALUES (s.Ambito, s.Extensiones, s.MaximoMB);

-- PRY-09: acciones de Portafolio y sus roles iniciales (los del prototipo); luego se administran en Portafolio › Parametrización
DECLARE @AccionesNuevas TABLE (Operacion NVARCHAR(10), Clave VARCHAR(40));
MERGE pry.Accion AS t
USING (VALUES ('crear_item', N'Crear proyecto, mejora o bug', 1), ('editar_tarea', N'Editar tarea', 2), ('exigir_aprobacion', N'Exigir aprobación', 3),
              ('registrar_acuerdo', N'Registrar acuerdo semanal', 4), ('cumplir_acuerdo', N'Marcar acuerdo cumplido', 5),
              ('eliminar_borrador', N'Eliminar borradores', 6)) AS s (Clave, Nombre, Orden)
ON t.Clave = s.Clave
WHEN MATCHED THEN UPDATE SET Nombre = s.Nombre, Orden = s.Orden
WHEN NOT MATCHED THEN INSERT (Clave, Nombre, Orden) VALUES (s.Clave, s.Nombre, s.Orden)
OUTPUT $action, inserted.Clave INTO @AccionesNuevas;

INSERT pry.AccionRol (Accion, RolId)
SELECT n.Clave, r.RolId
FROM @AccionesNuevas n
JOIN (VALUES ('crear_item', 'Gerencia de TI'), ('crear_item', 'Administrador'), ('editar_tarea', 'Gerencia de TI'),
             ('exigir_aprobacion', 'Gerencia de TI'), ('registrar_acuerdo', 'Gerencia de TI'), ('cumplir_acuerdo', 'Gerencia de TI'),
             ('eliminar_borrador', 'Gerencia de TI'), ('eliminar_borrador', 'Administrador')) AS d (Clave, Rol) ON d.Clave = n.Clave
JOIN seg.Rol r ON r.Nombre = d.Rol
WHERE n.Operacion = 'INSERT';

-- El Administrador tiene siempre todos los permisos (también los de opciones nuevas)
INSERT seg.Permiso (RolId, Opcion, Accion)
SELECT r.RolId, a.Opcion, a.Accion FROM seg.Rol r CROSS JOIN seg.OpcionAccion a
WHERE r.Nombre = 'Administrador'
  AND NOT EXISTS (SELECT 1 FROM seg.Permiso p WHERE p.RolId = r.RolId AND p.Opcion = a.Opcion AND p.Accion = a.Accion);

-- Permisos iniciales de los demás roles (los del prototipo). Solo para roles que aún no tienen ninguno:
-- después se administran en Seguridad › Roles y permisos y este script no los pisa.
DECLARE @Inicial TABLE (Rol VARCHAR(60), Modulo NVARCHAR(60) NULL, Opcion VARCHAR(60) NULL, Acciones VARCHAR(200));
INSERT @Inicial (Rol, Modulo, Opcion, Acciones) VALUES
    -- Todos: panel, sus tareas y aprobaciones (técnicos y aprobadores no son roles) y solicitar consultas dinámicas
    ('*', NULL, 'inicio', 'ver'), ('*', NULL, 'tareas', 'ver,enviar'), ('*', NULL, 'aprobaciones', 'ver,aprobar'),
    ('*', NULL, 'ejecuciones', 'ver,crear,exportar,aprobar'),
    ('Analista de coactivas', N'Coactivas', NULL, 'ver,crear,editar,eliminar,exportar,carga,bloquear,levantar,transferir'),
    ('Analista de coactivas', NULL, 'reportes', 'ver,exportar'),
    ('Gerencia de TI', N'Portafolio', NULL, 'ver,crear,editar,eliminar,exportar,aprobar,enviar,cumplir,generar'),
    ('Gerencia de TI', N'Presupuesto', NULL, 'ver,crear,editar,eliminar,exportar,aprobar'),
    ('Gerencia de TI', N'Cumplimiento y auditoría', NULL, 'ver,exportar'),
    ('Gerencia de TI', NULL, 'reportes', 'ver,exportar'), ('Gerencia de TI', NULL, 'catreportes', 'ver,exportar'),
    ('Gerencia de TI', NULL, 'consultas', 'ver,exportar'), ('Gerencia de TI', NULL, 'audejec', 'ver,exportar'),
    ('Compras TI', N'Presupuesto', NULL, 'ver,crear,editar,exportar'), ('Compras TI', NULL, 'reportes', 'ver'),
    ('Cumplimiento', N'Cumplimiento y auditoría', NULL, 'ver,crear,editar,eliminar,exportar,aprobar'),
    ('Cumplimiento', NULL, 'auditoria', 'ver,exportar'), ('Cumplimiento', NULL, 'reportes', 'ver,exportar'),
    ('Cumplimiento', NULL, 'audejec', 'ver,exportar'),
    ('Seguridad de la información', N'Bóveda', NULL, 'ver,crear,editar,eliminar,exportar,aprobar,mostrar,copiar'),
    ('Seguridad de la información', NULL, 'auditoria', 'ver,exportar'), ('Seguridad de la información', NULL, 'usuarios', 'ver,exportar'),
    ('Seguridad de la información', NULL, 'params', 'ver,exportar'), ('Seguridad de la información', NULL, 'reportes', 'ver'),
    ('Seguridad de la información', NULL, 'consultas', 'ver,exportar'), ('Seguridad de la información', NULL, 'audejec', 'ver,exportar'),
    ('Marketing', N'Marketing', NULL, 'ver,crear,editar,eliminar,exportar,aprobar'),
    ('Consulta', NULL, 'reportes', 'ver');

INSERT seg.Permiso (RolId, Opcion, Accion)
SELECT DISTINCT r.RolId, a.Opcion, a.Accion
FROM seg.Rol r
JOIN @Inicial i ON i.Rol = r.Nombre OR i.Rol = '*'
JOIN seg.Opcion o ON o.Opcion = i.Opcion OR o.Modulo = i.Modulo
JOIN seg.OpcionAccion a ON a.Opcion = o.Opcion
     AND a.Accion IN (SELECT LTRIM(RTRIM(value)) FROM STRING_SPLIT(i.Acciones, ','))
WHERE r.Nombre IN (SELECT Rol FROM @Inicial WHERE Rol <> '*') -- solo los roles de fábrica, no los creados después
  AND NOT EXISTS (SELECT 1 FROM seg.Permiso p WHERE p.RolId = r.RolId);
