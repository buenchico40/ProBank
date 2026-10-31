-- COA-04: carga masiva. La Web lee el Excel y la API valida el formato; aquí se validan las reglas que dependen de la base.
-- @Filas: JSON [{fila, oficio, recibido, remitidoPor, tipo, proceso, ruc, tipoIdentificacion, identificacion, cliente, monto,
--                productos, montoOrdenado, observacion}]. Si una fila tiene errores, su oficio completo no se carga.
-- Las tablas temporales usan la intercalación de la base (tempdb puede tener otra).
-- Resultado 1: errores (Fila, Oficio, Motivo). Resultado 2: oficios y órdenes cargados.
CREATE PROCEDURE coa.usp_Carga_Procesar
    @Filas NVARCHAR(MAX), @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'oficios', 'carga') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para la carga masiva.', 1;

    CREATE TABLE #f (Fila INT PRIMARY KEY, Oficio VARCHAR(30) COLLATE DATABASE_DEFAULT, Recibido DATE, RemitidoPor NVARCHAR(150) COLLATE DATABASE_DEFAULT, Tipo VARCHAR(20) COLLATE DATABASE_DEFAULT, Proceso VARCHAR(50) COLLATE DATABASE_DEFAULT,
                     Ruc VARCHAR(13) COLLATE DATABASE_DEFAULT, TipoIdentificacion VARCHAR(10) COLLATE DATABASE_DEFAULT, Identificacion VARCHAR(20) COLLATE DATABASE_DEFAULT, Cliente NVARCHAR(150) COLLATE DATABASE_DEFAULT, Monto DECIMAL(18,2),
                     Productos VARCHAR(200) COLLATE DATABASE_DEFAULT, MontoOrdenado DECIMAL(18,2), Observacion NVARCHAR(500) COLLATE DATABASE_DEFAULT,
                     EntidadId INT NULL, CoactivaId INT NULL, Bloqueado DECIMAL(18,2) NULL);
    INSERT #f (Fila, Oficio, Recibido, RemitidoPor, Tipo, Proceso, Ruc, TipoIdentificacion, Identificacion, Cliente, Monto, Productos, MontoOrdenado, Observacion)
    SELECT * FROM OPENJSON(@Filas) WITH (Fila INT '$.fila', Oficio VARCHAR(30) '$.oficio', Recibido DATE '$.recibido', RemitidoPor NVARCHAR(150) '$.remitidoPor',
        Tipo VARCHAR(20) '$.tipo', Proceso VARCHAR(50) '$.proceso', Ruc VARCHAR(13) '$.ruc', TipoIdentificacion VARCHAR(10) '$.tipoIdentificacion',
        Identificacion VARCHAR(20) '$.identificacion', Cliente NVARCHAR(150) '$.cliente', Monto DECIMAL(18,2) '$.monto', Productos VARCHAR(200) '$.productos',
        MontoOrdenado DECIMAL(18,2) '$.montoOrdenado', Observacion NVARCHAR(500) '$.observacion');

    UPDATE f SET EntidadId = e.EntidadId FROM #f f JOIN coa.Entidad e ON e.Ruc = f.Ruc AND e.Estado = 'Activo';
    UPDATE f SET CoactivaId = c.CoactivaId, Bloqueado = c.Bloqueado
    FROM #f f JOIN coa.vw_Coactiva c ON c.EntidadId = f.EntidadId AND c.NumeroProceso = f.Proceso AND c.Anulada = 0;

    CREATE TABLE #e (Fila INT, Oficio VARCHAR(30) COLLATE DATABASE_DEFAULT, Motivo NVARCHAR(300) COLLATE DATABASE_DEFAULT);
    INSERT #e SELECT Fila, Oficio, N'El oficio ya existe en ProBank.' FROM #f WHERE Oficio IN (SELECT Numero FROM coa.Oficio);
    INSERT #e SELECT Fila, Oficio, N'El RUC de la entidad no está en el catálogo de entidades.' FROM #f WHERE EntidadId IS NULL;
    INSERT #e SELECT Fila, Oficio, N'El N.º de proceso ya existe para esa entidad.' FROM #f WHERE Tipo = 'Bloqueo' AND CoactivaId IS NOT NULL;
    INSERT #e SELECT f.Fila, f.Oficio, N'El N.º de proceso está repetido para la misma entidad en el archivo.'
    FROM #f f WHERE f.Tipo = 'Bloqueo' AND EXISTS (SELECT 1 FROM #f g WHERE g.Tipo = 'Bloqueo' AND g.Ruc = f.Ruc AND g.Proceso = f.Proceso AND g.Fila <> f.Fila);
    INSERT #e SELECT Fila, Oficio, N'El N.º de proceso no existe en ProBank con fondos bloqueados.'
    FROM #f WHERE Tipo <> 'Bloqueo' AND EntidadId IS NOT NULL AND (CoactivaId IS NULL OR Bloqueado <= 0);
    INSERT #e SELECT Fila, Oficio, N'El monto ordenado supera lo bloqueado vigente.'
    FROM #f WHERE Tipo <> 'Bloqueo' AND CoactivaId IS NOT NULL AND Bloqueado > 0 AND MontoOrdenado > Bloqueado;
    INSERT #e SELECT f.Fila, f.Oficio, N'La identificación ya existe con otro tipo de identificación.'
    FROM #f f JOIN coa.Cliente c ON c.Identificacion = f.Identificacion WHERE f.Tipo = 'Bloqueo' AND c.TipoIdentificacion <> f.TipoIdentificacion;

    -- Solo se cargan los oficios sin errores
    DELETE FROM #f WHERE Oficio IN (SELECT Oficio FROM #e);
    DECLARE @Oficios INT = (SELECT COUNT(DISTINCT Oficio) FROM #f), @Ordenes INT = (SELECT COUNT(*) FROM #f);

    BEGIN TRAN;
    INSERT coa.Oficio (Numero, Recibido, RemitidoPor, RegistradoPorId)
    SELECT Oficio, MIN(Recibido), MIN(RemitidoPor), @ActorId FROM #f GROUP BY Oficio;

    INSERT coa.Cliente (TipoIdentificacion, Identificacion, Nombre)
    SELECT TipoIdentificacion, Identificacion, MIN(Cliente) FROM #f
    WHERE Tipo = 'Bloqueo' AND Identificacion NOT IN (SELECT Identificacion FROM coa.Cliente)
    GROUP BY TipoIdentificacion, Identificacion;

    INSERT coa.Coactiva (OficioId, EntidadId, ClienteId, NumeroProceso, Monto, ProductosPermitidos, Observacion)
    SELECT o.OficioId, f.EntidadId, c.ClienteId, f.Proceso, f.Monto, NULLIF(f.Productos, ''), f.Observacion
    FROM #f f JOIN coa.Oficio o ON o.Numero = f.Oficio JOIN coa.Cliente c ON c.Identificacion = f.Identificacion
    WHERE f.Tipo = 'Bloqueo';

    INSERT coa.OrdenCoactiva (OficioId, CoactivaId, Tipo, MontoOrdenado, Observacion)
    SELECT o.OficioId, ISNULL(f.CoactivaId, c.CoactivaId), f.Tipo, CASE WHEN f.Tipo = 'Bloqueo' THEN NULL ELSE f.MontoOrdenado END, f.Observacion
    FROM #f f JOIN coa.Oficio o ON o.Numero = f.Oficio
    LEFT JOIN coa.Coactiva c ON f.Tipo = 'Bloqueo' AND c.EntidadId = f.EntidadId AND c.NumeroProceso = f.Proceso;

    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    SELECT NULLIF(@ActorId, 0), @RolActivo, 'Coactivas', N'Carga masiva del oficio ' + Oficio, CONCAT(COUNT(*), N' coactiva(s)'), @Ip
    FROM #f GROUP BY Oficio;
    COMMIT;

    SELECT Fila, Oficio, Motivo FROM #e ORDER BY Fila;
    SELECT @Oficios AS Oficios, @Ordenes AS Ordenes;
END;
