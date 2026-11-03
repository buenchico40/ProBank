-- Crea (@Codigo NULL, código QRY-NNN, en Borrador) o edita una consulta con sus bases, roles y aprobadores (listas JSON).
-- Cambiar el procedimiento borra la definición leída (parámetros y columnas): hay que volver a leerla. Cambiar el
-- procedimiento o las bases de una consulta publicada la devuelve a Borrador (@VolvioBorrador).
CREATE PROCEDURE rpt.usp_Consulta_Guardar
    @Codigo VARCHAR(20) = NULL, @Nombre NVARCHAR(150), @Descripcion NVARCHAR(500) = NULL, @Procedimiento NVARCHAR(257),
    @TiempoMaximoSeg INT, @LimiteFilas INT,
    @Conexiones NVARCHAR(MAX), @Roles NVARCHAR(MAX), @Aprobadores NVARCHAR(MAX),
    @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45), @CodigoGuardado VARCHAR(20) OUTPUT, @VolvioBorrador BIT OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'consultas', CASE WHEN @Codigo IS NULL THEN 'crear' ELSE 'editar' END) = 0
        THROW 50403, N'Tu rol activo no tiene permiso para guardar consultas.', 1;

    DECLARE @Cx TABLE (ConexionId INT PRIMARY KEY);
    DECLARE @Ro TABLE (RolId INT PRIMARY KEY);
    DECLARE @Ap TABLE (UsuarioId INT PRIMARY KEY);
    INSERT @Cx SELECT DISTINCT c.ConexionId FROM OPENJSON(@Conexiones) j JOIN rpt.Conexion c ON c.Codigo = j.value;
    IF (SELECT COUNT(*) FROM @Cx) <> (SELECT COUNT(DISTINCT value) FROM OPENJSON(@Conexiones)) THROW 50422, N'Una de las bases de datos no existe.', 1;
    INSERT @Ro SELECT DISTINCT r.RolId FROM OPENJSON(@Roles) j JOIN seg.Rol r ON r.Nombre = j.value;
    IF (SELECT COUNT(*) FROM @Ro) <> (SELECT COUNT(DISTINCT value) FROM OPENJSON(@Roles)) THROW 50422, N'Uno de los roles no existe.', 1;
    INSERT @Ap SELECT DISTINCT u.UsuarioId FROM OPENJSON(@Aprobadores) j JOIN seg.Usuario u ON u.Token = TRY_CAST(j.value AS UNIQUEIDENTIFIER) AND u.Estado = 'Activo';
    IF (SELECT COUNT(*) FROM @Ap) <> (SELECT COUNT(DISTINCT value) FROM OPENJSON(@Aprobadores)) THROW 50422, N'Uno de los aprobadores no existe o está inactivo.', 1;

    SET @VolvioBorrador = 0;
    BEGIN TRAN;
    DECLARE @Id INT;
    IF @Codigo IS NULL
    BEGIN
        DECLARE @n INT = 1 + ISNULL((SELECT MAX(CAST(SUBSTRING(Codigo, 5, 10) AS INT)) FROM rpt.Consulta WITH (UPDLOCK, HOLDLOCK) WHERE Codigo LIKE 'QRY-%'), 0);
        SET @CodigoGuardado = 'QRY-' + RIGHT('00' + CAST(@n AS VARCHAR(10)), 3);
        INSERT rpt.Consulta (Codigo, Nombre, Descripcion, Procedimiento, TiempoMaximoSeg, LimiteFilas, Estado, CreadoPorId)
        VALUES (@CodigoGuardado, @Nombre, @Descripcion, @Procedimiento, @TiempoMaximoSeg, @LimiteFilas, 'Borrador', @ActorId);
        SET @Id = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        DECLARE @ProcActual NVARCHAR(257), @Estado VARCHAR(20);
        SELECT @Id = ConsultaId, @ProcActual = Procedimiento, @Estado = Estado FROM rpt.Consulta WITH (UPDLOCK) WHERE Codigo = @Codigo;
        IF @Id IS NULL THROW 50404, N'La consulta no existe.', 1;
        DECLARE @CambiaProc BIT = CASE WHEN @ProcActual <> @Procedimiento THEN 1 ELSE 0 END;
        DECLARE @CambiaBases BIT = CASE WHEN EXISTS (SELECT ConexionId FROM rpt.ConsultaConexion WHERE ConsultaId = @Id EXCEPT SELECT ConexionId FROM @Cx)
                                          OR EXISTS (SELECT ConexionId FROM @Cx EXCEPT SELECT ConexionId FROM rpt.ConsultaConexion WHERE ConsultaId = @Id) THEN 1 ELSE 0 END;
        IF @Estado = 'Publicada' AND (@CambiaProc = 1 OR @CambiaBases = 1) SET @VolvioBorrador = 1;
        IF @CambiaProc = 1 DELETE rpt.Parametro WHERE ConsultaId = @Id;
        UPDATE rpt.Consulta SET Nombre = @Nombre, Descripcion = @Descripcion, Procedimiento = @Procedimiento, TiempoMaximoSeg = @TiempoMaximoSeg,
               LimiteFilas = @LimiteFilas,
               Estado = CASE WHEN @VolvioBorrador = 1 THEN 'Borrador' ELSE Estado END,
               ColumnasJson = CASE WHEN @CambiaProc = 1 THEN NULL ELSE ColumnasJson END,
               DefinicionLeida = CASE WHEN @CambiaProc = 1 THEN NULL ELSE DefinicionLeida END
        WHERE ConsultaId = @Id;
        SET @CodigoGuardado = @Codigo;
        DELETE rpt.ConsultaConexion WHERE ConsultaId = @Id;
        DELETE rpt.ConsultaRol WHERE ConsultaId = @Id;
        DELETE rpt.ConsultaAprobador WHERE ConsultaId = @Id;
    END;
    INSERT rpt.ConsultaConexion (ConsultaId, ConexionId) SELECT @Id, ConexionId FROM @Cx;
    INSERT rpt.ConsultaRol (ConsultaId, RolId) SELECT @Id, RolId FROM @Ro;
    INSERT rpt.ConsultaAprobador (ConsultaId, UsuarioId) SELECT @Id, UsuarioId FROM @Ap;
    -- Una consulta con aprobadores no puede enviarse programada: sus programaciones quedan en pausa
    IF EXISTS (SELECT 1 FROM @Ap) UPDATE rpt.Programacion SET Estado = 'Pausada' WHERE ConsultaId = @Id AND Estado = 'Activa';

    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Consultas dinámicas',
            CASE WHEN @Codigo IS NULL THEN N'Nueva consulta ' ELSE N'Edición de la consulta ' END + @CodigoGuardado,
            CONCAT(@Nombre, N' · ', @Procedimiento, CASE WHEN @VolvioBorrador = 1 THEN N' · vuelve a Borrador' ELSE N'' END), @Ip);
    COMMIT;
END;
