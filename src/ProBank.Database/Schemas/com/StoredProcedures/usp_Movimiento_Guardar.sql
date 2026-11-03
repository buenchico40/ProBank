-- Registra (@Token NULL) o corrige un movimiento pendiente. Las partidas deben estar activas; el presupuesto cambia al aprobarlo.
CREATE PROCEDURE com.usp_Movimiento_Guardar
    @Token UNIQUEIDENTIFIER = NULL, @Fecha DATE, @Tipo NVARCHAR(20), @OrigenToken UNIQUEIDENTIFIER = NULL, @DestinoToken UNIQUEIDENTIFIER = NULL,
    @Monto DECIMAL(18,2), @Motivo NVARCHAR(500), @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45), @TokenGuardado UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'movimientos', CASE WHEN @Token IS NULL THEN 'crear' ELSE 'editar' END) = 0
        THROW 50403, N'Tu rol activo no tiene permiso para registrar movimientos.', 1;
    BEGIN TRAN;
    DECLARE @Origen INT = (SELECT PartidaId FROM com.Partida WHERE Token = @OrigenToken AND Estado = 'Activo'),
            @Destino INT = (SELECT PartidaId FROM com.Partida WHERE Token = @DestinoToken AND Estado = 'Activo');
    IF (@OrigenToken IS NOT NULL AND @Origen IS NULL) OR (@DestinoToken IS NOT NULL AND @Destino IS NULL)
        THROW 50422, N'Las partidas del movimiento deben existir y estar activas.', 1;
    IF @Token IS NULL
    BEGIN
        SET @TokenGuardado = NEWID();
        INSERT com.Movimiento (Token, Fecha, Tipo, PartidaOrigenId, PartidaDestinoId, Monto, Motivo, RegistradoPorId)
        VALUES (@TokenGuardado, @Fecha, @Tipo, @Origen, @Destino, @Monto, @Motivo, @ActorId);
    END
    ELSE
    BEGIN
        DECLARE @Estado VARCHAR(20) = (SELECT Estado FROM com.Movimiento WITH (UPDLOCK) WHERE Token = @Token);
        IF @Estado IS NULL THROW 50404, N'El movimiento no existe.', 1;
        IF @Estado <> 'Pendiente' THROW 50422, N'Solo se corrigen movimientos pendientes.', 1;
        UPDATE com.Movimiento SET Fecha = @Fecha, Tipo = @Tipo, PartidaOrigenId = @Origen, PartidaDestinoId = @Destino, Monto = @Monto, Motivo = @Motivo
        WHERE Token = @Token;
        SET @TokenGuardado = @Token;
    END;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    SELECT NULLIF(@ActorId, 0), @RolActivo, 'Presupuesto', CASE WHEN @Token IS NULL THEN N'Nuevo movimiento ' ELSE N'Edición del movimiento ' END + Codigo,
           CONCAT(Tipo, N' · ', Monto), @Ip
    FROM com.Movimiento WHERE Token = @TokenGuardado;
    COMMIT;
END;
