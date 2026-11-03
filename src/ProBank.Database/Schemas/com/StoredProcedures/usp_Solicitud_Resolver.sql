-- Aprueba o rechaza una solicitud pendiente. Aprobada y presupuestada, queda como comprometido de su partida: la partida
-- debe estar activa y tener disponible.
CREATE PROCEDURE com.usp_Solicitud_Resolver
    @Token UNIQUEIDENTIFIER, @Aprobar BIT, @Comentario NVARCHAR(500) = NULL, @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'solicitudes', 'aprobar') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para aprobar solicitudes de compra.', 1;
    BEGIN TRAN;
    DECLARE @Codigo VARCHAR(20), @Aprobacion VARCHAR(20), @Anulada BIT, @PartidaId INT, @RenovacionId INT;
    SELECT @Codigo = Codigo, @Aprobacion = Aprobacion, @Anulada = Anulada, @PartidaId = PartidaId, @RenovacionId = RenovacionId
    FROM com.Solicitud WITH (UPDLOCK) WHERE Token = @Token;
    IF @Codigo IS NULL THROW 50404, N'La solicitud no existe.', 1;
    IF @Anulada = 1 OR @Aprobacion <> 'Pendiente' THROW 50422, N'Solo se resuelven solicitudes pendientes.', 1;
    IF @Aprobar = 1 AND @PartidaId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM com.Partida WHERE PartidaId = @PartidaId AND Estado = 'Activo')
        THROW 50422, N'La partida de la solicitud debe estar activa.', 1;
    UPDATE com.Solicitud SET Aprobacion = CASE WHEN @Aprobar = 1 THEN 'Aprobada' ELSE 'Rechazada' END, AprobadoPorId = @ActorId,
           FechaAprobacion = SYSDATETIME(), Comentario = NULLIF(@Comentario, N'')
    WHERE Token = @Token;
    IF @Aprobar = 1 AND @PartidaId IS NOT NULL
    BEGIN
        DECLARE @p VARCHAR(20) = CAST(@PartidaId AS VARCHAR(20));
        EXEC com.usp_Partida_ExigirDisponible @p;
    END;
    -- Rechazado el requerimiento de una renovación, la renovación vuelve a estar disponible para pedirlo otra vez
    IF @Aprobar = 0 AND @RenovacionId IS NOT NULL
        UPDATE com.Renovacion SET Estado = 'Activo' WHERE RenovacionId = @RenovacionId AND Estado = 'Requerimiento enviado';
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Presupuesto', CASE WHEN @Aprobar = 1 THEN N'Aprobación de la solicitud ' ELSE N'Rechazo de la solicitud ' END + @Codigo,
            NULLIF(@Comentario, N''), @Ip);
    COMMIT;
END;
