-- Anular una coactiva libera sus bloqueos vigentes (levantamiento sin orden, queda en el historial).
CREATE PROCEDURE coa.usp_Coactiva_Anular
    @Token UNIQUEIDENTIFIER, @Hoy DATE, @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45), @Liberados INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'coactivas', 'eliminar') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para anular coactivas.', 1;
    BEGIN TRAN;
    DECLARE @CoactivaId INT, @Proceso VARCHAR(50), @Anulada BIT;
    SELECT @CoactivaId = CoactivaId, @Proceso = NumeroProceso, @Anulada = Anulada FROM coa.Coactiva WITH (UPDLOCK) WHERE Token = @Token;
    IF @CoactivaId IS NULL THROW 50404, N'La coactiva no existe.', 1;
    IF @Anulada = 1 THROW 50422, N'La coactiva ya está anulada.', 1;
    UPDATE coa.Coactiva SET Anulada = 1 WHERE CoactivaId = @CoactivaId;
    DECLARE @Lista VARCHAR(20) = CAST(@CoactivaId AS VARCHAR(20)), @Motivo NVARCHAR(500) = N'Levantado por anulación de la coactiva ' + @Proceso;
    EXEC coa.usp_Bloqueo_LiberarVigentes @Coactivas = @Lista, @Motivo = @Motivo, @Hoy = @Hoy, @ActorId = @ActorId, @Liberados = @Liberados OUTPUT;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Coactivas', N'Anulación de coactiva ' + @Proceso, CONCAT(@Liberados, N' bloqueo(s) liberado(s)'), @Ip);
    COMMIT;
END;
