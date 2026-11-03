-- SQL-03: un aprobador aprueba o rechaza (con motivo) una ejecución pendiente. Quien la solicitó no la aprueba. Un rechazo la
-- cierra (Rechazada); cuando aprueban todos queda Aprobada y el solicitante la ejecuta. Deja el evento en la bitácora.
CREATE PROCEDURE rpt.usp_Ejecucion_Resolver
    @Codigo VARCHAR(20), @UsuarioId INT, @Aprobar BIT, @Comentario NVARCHAR(1000) = NULL, @Ip VARCHAR(45), @Estado VARCHAR(20) OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRAN;
    DECLARE @Id BIGINT, @Solicitante INT;
    SELECT @Id = EjecucionId, @Solicitante = SolicitanteId, @Estado = Estado FROM rpt.Ejecucion WITH (UPDLOCK) WHERE Codigo = @Codigo;
    IF @Id IS NULL THROW 50404, N'La ejecución no existe.', 1;
    IF @Estado <> 'PendienteAprobacion' THROW 50422, N'Solo se resuelven ejecuciones pendientes de aprobación.', 1;
    IF @Solicitante = @UsuarioId THROW 50422, N'No puede aprobar una ejecución que usted solicitó.', 1;
    IF NOT EXISTS (SELECT 1 FROM rpt.EjecucionAprobador WHERE EjecucionId = @Id AND UsuarioId = @UsuarioId)
        THROW 50403, N'Usted no es aprobador de esta ejecución.', 1;
    IF EXISTS (SELECT 1 FROM rpt.EjecucionAprobador WHERE EjecucionId = @Id AND UsuarioId = @UsuarioId AND Decision IS NOT NULL)
        THROW 50422, N'Usted ya resolvió esta ejecución.', 1;
    IF @Aprobar = 0 AND NULLIF(LTRIM(RTRIM(@Comentario)), N'') IS NULL THROW 50422, N'Indique el motivo del rechazo.', 1;

    UPDATE rpt.EjecucionAprobador SET Decision = CASE WHEN @Aprobar = 1 THEN 'Aprobada' ELSE 'Rechazada' END,
           Comentario = NULLIF(LTRIM(RTRIM(@Comentario)), N''), Fecha = SYSDATETIME()
    WHERE EjecucionId = @Id AND UsuarioId = @UsuarioId;
    SET @Estado = CASE WHEN @Aprobar = 0 THEN 'Rechazada'
                       WHEN NOT EXISTS (SELECT 1 FROM rpt.EjecucionAprobador WHERE EjecucionId = @Id AND ISNULL(Decision, '') <> 'Aprobada') THEN 'Aprobada'
                       ELSE 'PendienteAprobacion' END;
    UPDATE rpt.Ejecucion SET Estado = @Estado WHERE EjecucionId = @Id;
    INSERT rpt.EjecucionEvento (EjecucionId, UsuarioId, Evento, Detalle, Resultado, Ip)
    VALUES (@Id, @UsuarioId, CASE WHEN @Aprobar = 1 THEN 'Aprobación' ELSE 'Rechazo' END,
            CONCAT((SELECT COUNT(*) FROM rpt.EjecucionAprobador WHERE EjecucionId = @Id AND Decision = 'Aprobada'), N' de ',
                   (SELECT COUNT(*) FROM rpt.EjecucionAprobador WHERE EjecucionId = @Id), N' aprobaciones',
                   CASE WHEN NULLIF(LTRIM(RTRIM(@Comentario)), N'') IS NULL THEN N'' ELSE N' · ' + LTRIM(RTRIM(@Comentario)) END),
            CASE @Estado WHEN 'PendienteAprobacion' THEN 'Pendiente de aprobación' ELSE @Estado END, @Ip);
    COMMIT;
END;
