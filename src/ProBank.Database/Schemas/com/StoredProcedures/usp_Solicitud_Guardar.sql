-- Registra (@Token NULL) o corrige una solicitud pendiente. COM-02: presupuestada con una partida activa, o no presupuestada.
CREATE PROCEDURE com.usp_Solicitud_Guardar
    @Token UNIQUEIDENTIFIER = NULL, @Descripcion NVARCHAR(300), @Area NVARCHAR(60), @Proveedor NVARCHAR(150) = NULL, @Presupuestado BIT,
    @PartidaToken UNIQUEIDENTIFIER = NULL, @Monto DECIMAL(18,2),
    @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45), @TokenGuardado UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'solicitudes', CASE WHEN @Token IS NULL THEN 'crear' ELSE 'editar' END) = 0
        THROW 50403, N'Tu rol activo no tiene permiso para guardar solicitudes de compra.', 1;
    DECLARE @PartidaId INT = CASE WHEN @Presupuestado = 1 THEN (SELECT PartidaId FROM com.Partida WHERE Token = @PartidaToken AND Estado = 'Activo') END;
    IF @Presupuestado = 1 AND @PartidaId IS NULL THROW 50422, N'Elija una partida activa (o márquela como no presupuestada).', 1;
    BEGIN TRAN;
    IF @Token IS NULL
    BEGIN
        SET @TokenGuardado = NEWID();
        INSERT com.Solicitud (Token, Descripcion, Area, Proveedor, Presupuestado, PartidaId, Monto, SolicitanteId)
        VALUES (@TokenGuardado, @Descripcion, @Area, @Proveedor, @Presupuestado, @PartidaId, @Monto, @ActorId);
    END
    ELSE
    BEGIN
        DECLARE @Aprobacion VARCHAR(20), @Anulada BIT;
        SELECT @Aprobacion = Aprobacion, @Anulada = Anulada FROM com.Solicitud WITH (UPDLOCK) WHERE Token = @Token;
        IF @Aprobacion IS NULL THROW 50404, N'La solicitud no existe.', 1;
        IF @Anulada = 1 OR @Aprobacion <> 'Pendiente' THROW 50422, N'Solo se corrigen solicitudes pendientes de aprobación.', 1;
        UPDATE com.Solicitud SET Descripcion = @Descripcion, Area = @Area, Proveedor = @Proveedor, Presupuestado = @Presupuestado,
               PartidaId = @PartidaId, Monto = @Monto
        WHERE Token = @Token;
        SET @TokenGuardado = @Token;
    END;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    SELECT NULLIF(@ActorId, 0), @RolActivo, 'Presupuesto', CASE WHEN @Token IS NULL THEN N'Nueva solicitud de compra ' ELSE N'Edición de la solicitud ' END + Codigo,
           CONCAT(Descripcion, N' · ', Monto, CASE WHEN Presupuestado = 0 THEN N' · no presupuestada' END), @Ip
    FROM com.Solicitud WHERE Token = @TokenGuardado;
    COMMIT;
END;
