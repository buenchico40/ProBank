-- Aprueba o rechaza un movimiento pendiente. Al aprobarse cambia el presupuesto: la partida de la que sale debe tener disponible.
CREATE PROCEDURE com.usp_Movimiento_Resolver
    @Token UNIQUEIDENTIFIER, @Aprobar BIT, @Comentario NVARCHAR(500) = NULL, @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'movimientos', 'aprobar') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para aprobar movimientos.', 1;
    BEGIN TRAN;
    DECLARE @Codigo VARCHAR(20), @Estado VARCHAR(20), @Origen INT, @Destino INT;
    SELECT @Codigo = Codigo, @Estado = Estado, @Origen = PartidaOrigenId, @Destino = PartidaDestinoId FROM com.Movimiento WITH (UPDLOCK) WHERE Token = @Token;
    IF @Codigo IS NULL THROW 50404, N'El movimiento no existe.', 1;
    IF @Estado <> 'Pendiente' THROW 50422, N'Solo se resuelven movimientos pendientes.', 1;
    IF @Aprobar = 1 AND EXISTS (SELECT 1 FROM com.Partida WHERE PartidaId IN (@Origen, @Destino) AND Estado <> 'Activo')
        THROW 50422, N'Las partidas del movimiento deben estar activas.', 1;
    UPDATE com.Movimiento SET Estado = CASE WHEN @Aprobar = 1 THEN 'Aprobada' ELSE 'Rechazada' END, ResueltoPorId = @ActorId,
           FechaResolucion = SYSDATETIME(), Comentario = NULLIF(@Comentario, N'')
    WHERE Token = @Token;
    IF @Aprobar = 1 AND @Origen IS NOT NULL
    BEGIN
        DECLARE @p VARCHAR(20) = CAST(@Origen AS VARCHAR(20));
        EXEC com.usp_Partida_ExigirDisponible @p;
    END;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Presupuesto', CASE WHEN @Aprobar = 1 THEN N'Aprobación del movimiento ' ELSE N'Rechazo del movimiento ' END + @Codigo,
            NULLIF(@Comentario, N''), @Ip);
    COMMIT;
END;
