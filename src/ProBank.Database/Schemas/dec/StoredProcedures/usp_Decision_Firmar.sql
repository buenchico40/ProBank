-- DEC-03: firma de un aprobador (la API ya verificó su clave). Solo firma un aprobador pendiente; en secuencia, cuando llega su turno.
-- El contenido debe seguir íntegro (huella). Guarda el resultado con su sello; un rechazo (con comentario) cierra la decisión como
-- Rechazada y, cuando todos aprueban, queda Aprobada.
CREATE PROCEDURE dec.usp_Decision_Firmar
    @Token UNIQUEIDENTIFIER, @Aprobar BIT, @Comentario NVARCHAR(500) = NULL,
    @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45), @EstadoFinal NVARCHAR(20) OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'pendientes', 'aprobar') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para firmar aprobaciones.', 1;
    IF @Aprobar = 0 AND NULLIF(LTRIM(RTRIM(@Comentario)), N'') IS NULL THROW 50422, N'Indique el motivo del rechazo.', 1;

    BEGIN TRAN;
    DECLARE @Id INT, @Codigo VARCHAR(12), @Estado NVARCHAR(20), @Secuencial BIT, @Huella VARBINARY(32), @Orden INT, @Resultado NVARCHAR(10);
    SELECT @Id = DecisionId, @Codigo = Codigo, @Estado = Estado, @Secuencial = Secuencial, @Huella = Huella FROM dec.Decision WITH (UPDLOCK) WHERE Token = @Token;
    IF @Id IS NULL THROW 50404, N'La decisión no existe.', 1;
    SELECT @Orden = Orden, @Resultado = Resultado FROM dec.DecisionAprobador WITH (UPDLOCK) WHERE DecisionId = @Id AND UsuarioId = @ActorId;
    IF @Orden IS NULL THROW 50403, N'No es aprobador de esta decisión.', 1;
    IF @Estado <> N'En aprobación' THROW 50409, N'La decisión no está en aprobación.', 1;
    IF @Resultado <> N'Pendiente' THROW 50409, N'Ya firmó esta decisión.', 1;
    IF @Secuencial = 1 AND EXISTS (SELECT 1 FROM dec.DecisionAprobador WHERE DecisionId = @Id AND Resultado = N'Pendiente' AND Orden < @Orden)
        THROW 50409, N'Aún no es su turno: la decisión se aprueba en secuencia.', 1;
    IF @Huella IS NULL OR @Huella <> dec.fn_Huella(@Id)
        THROW 50409, N'El contenido de la decisión no coincide con su huella: no se puede firmar. Avise a Seguridad de la información.', 1;

    DECLARE @Fecha DATETIME2(0) = SYSDATETIME(), @Usuario VARCHAR(60) = (SELECT Usuario FROM seg.Usuario WHERE UsuarioId = @ActorId);
    DECLARE @Res NVARCHAR(10) = CASE WHEN @Aprobar = 1 THEN N'Aprobada' ELSE N'Rechazada' END;
    SET @Comentario = NULLIF(LTRIM(RTRIM(@Comentario)), N'');
    UPDATE dec.DecisionAprobador SET Resultado = @Res, Comentario = @Comentario, Fecha = @Fecha, Ip = @Ip,
           Sello = dec.fn_Sello(@Huella, @Usuario, @Res, @Fecha, @Comentario, @Ip)
    WHERE DecisionId = @Id AND UsuarioId = @ActorId;

    SET @EstadoFinal = CASE WHEN @Aprobar = 0 THEN N'Rechazada'
                            WHEN NOT EXISTS (SELECT 1 FROM dec.DecisionAprobador WHERE DecisionId = @Id AND Resultado <> N'Aprobada') THEN N'Aprobada'
                            ELSE N'En aprobación' END;
    IF @EstadoFinal <> N'En aprobación' UPDATE dec.Decision SET Estado = @EstadoFinal, FechaCierre = @Fecha WHERE DecisionId = @Id;

    INSERT dec.DecisionEvento (DecisionId, UsuarioId, Evento, Detalle, Ip)
    SELECT @Id, @ActorId, CASE WHEN @Aprobar = 1 THEN N'Firma: aprobada' ELSE N'Firma: rechazada' END,
           LEFT(CONCAT(N'Sello ', CONVERT(VARCHAR(64), Sello, 2), N' · ' + @Comentario), 500), @Ip
    FROM dec.DecisionAprobador WHERE DecisionId = @Id AND UsuarioId = @ActorId;
    IF @EstadoFinal <> N'En aprobación'
        INSERT dec.DecisionEvento (DecisionId, UsuarioId, Evento, Detalle, Ip)
        VALUES (@Id, @ActorId, N'Cierre', CASE @EstadoFinal WHEN N'Aprobada' THEN N'Aprobada por todos los aprobadores' ELSE N'Rechazada' END, @Ip);

    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    SELECT NULLIF(@ActorId, 0), @RolActivo, N'Decisiones', CONCAT(N'Firma de la decisión ', @Codigo, N': ', @Res),
           CONCAT(N'Sello ', CONVERT(VARCHAR(64), Sello, 2), N' · decisión ', @EstadoFinal, N' · ' + @Comentario), @Ip
    FROM dec.DecisionAprobador WHERE DecisionId = @Id AND UsuarioId = @ActorId;
    COMMIT;
END;
