-- Registra (@Token NULL) o edita un servicio a renovar. COM-02: presupuestado con su partida (en borrador o activa) o no presupuestado.
CREATE PROCEDURE com.usp_Renovacion_Guardar
    @Token UNIQUEIDENTIFIER = NULL, @Servicio NVARCHAR(150), @Proveedor NVARCHAR(150), @Vence DATE, @MontoEstimado DECIMAL(18,2),
    @Presupuestado BIT, @PartidaToken UNIQUEIDENTIFIER = NULL, @DiasAviso INT,
    @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45), @TokenGuardado UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'renovaciones', CASE WHEN @Token IS NULL THEN 'crear' ELSE 'editar' END) = 0
        THROW 50403, N'Tu rol activo no tiene permiso para guardar renovaciones.', 1;
    DECLARE @PartidaId INT = CASE WHEN @Presupuestado = 1 THEN (SELECT PartidaId FROM com.Partida WHERE Token = @PartidaToken AND Estado <> 'Cerrado') END;
    IF @Presupuestado = 1 AND @PartidaId IS NULL THROW 50422, N'Elija una partida en borrador o activa (o márquela como no presupuestada).', 1;
    BEGIN TRAN;
    IF @Token IS NULL
    BEGIN
        SET @TokenGuardado = NEWID();
        INSERT com.Renovacion (Token, Servicio, Proveedor, Vence, MontoEstimado, Presupuestado, PartidaId, DiasAviso, CreadoPorId)
        VALUES (@TokenGuardado, @Servicio, @Proveedor, @Vence, @MontoEstimado, @Presupuestado, @PartidaId, @DiasAviso, @ActorId);
    END
    ELSE
    BEGIN
        DECLARE @Estado VARCHAR(30) = (SELECT Estado FROM com.Renovacion WITH (UPDLOCK) WHERE Token = @Token);
        IF @Estado IS NULL THROW 50404, N'La renovación no existe.', 1;
        IF @Estado = 'Retirado' THROW 50422, N'La renovación está retirada.', 1;
        UPDATE com.Renovacion SET Servicio = @Servicio, Proveedor = @Proveedor, Vence = @Vence, MontoEstimado = @MontoEstimado,
               Presupuestado = @Presupuestado, PartidaId = @PartidaId, DiasAviso = @DiasAviso
        WHERE Token = @Token;
        SET @TokenGuardado = @Token;
    END;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    SELECT NULLIF(@ActorId, 0), @RolActivo, 'Presupuesto', CASE WHEN @Token IS NULL THEN N'Nueva renovación ' ELSE N'Edición de la renovación ' END + Codigo,
           CONCAT(Servicio, N' · vence ', CONVERT(CHAR(10), Vence, 103), N' · aviso ', DiasAviso, N' días'), @Ip
    FROM com.Renovacion WHERE Token = @TokenGuardado;
    COMMIT;
END;
