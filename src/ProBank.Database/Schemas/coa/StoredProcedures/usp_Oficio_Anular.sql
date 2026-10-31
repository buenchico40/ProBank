-- Anular un oficio anula las coactivas que trajo (sus bloqueos vigentes se liberan). El oficio se conserva para auditoría.
CREATE PROCEDURE coa.usp_Oficio_Anular
    @Token UNIQUEIDENTIFIER, @Hoy DATE, @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45), @Liberados INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'oficios', 'eliminar') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para anular oficios.', 1;
    DECLARE @OficioId INT, @Numero VARCHAR(30), @Anulado BIT;
    SELECT @OficioId = OficioId, @Numero = Numero, @Anulado = Anulado FROM coa.Oficio WHERE Token = @Token;
    IF @OficioId IS NULL THROW 50404, N'El oficio no existe.', 1;
    IF @Anulado = 1 THROW 50422, N'El oficio ya está anulado.', 1;

    DECLARE @Coactivas VARCHAR(MAX) = (SELECT STRING_AGG(CAST(CoactivaId AS VARCHAR(MAX)), ',') FROM coa.Coactiva WHERE OficioId = @OficioId AND Anulada = 0);
    BEGIN TRAN;
    UPDATE coa.Oficio SET Anulado = 1 WHERE OficioId = @OficioId;
    UPDATE coa.Coactiva SET Anulada = 1 WHERE OficioId = @OficioId;
    DECLARE @Motivo NVARCHAR(500) = N'Levantado por anulación del oficio ' + @Numero;
    EXEC coa.usp_Bloqueo_LiberarVigentes @Coactivas = @Coactivas, @Motivo = @Motivo, @Hoy = @Hoy, @ActorId = @ActorId, @Liberados = @Liberados OUTPUT;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Coactivas', N'Anulación de oficio ' + @Numero, CONCAT(@Liberados, N' bloqueo(s) liberado(s)'), @Ip);
    COMMIT;
END;
