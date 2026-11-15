-- DEC-02 y DEC-03: envía a aprobación una decisión en borrador con al menos un aprobador y fija la huella del contenido.
CREATE PROCEDURE dec.usp_Decision_Enviar
    @Token UNIQUEIDENTIFIER, @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRAN;
    DECLARE @Id INT, @Codigo VARCHAR(12), @Estado NVARCHAR(20), @Creador INT, @Secuencial BIT, @TipoActivo BIT;
    SELECT @Id = d.DecisionId, @Codigo = d.Codigo, @Estado = d.Estado, @Creador = d.CreadoPorId, @Secuencial = t.Secuencial, @TipoActivo = t.Activo
    FROM dec.Decision d WITH (UPDLOCK) JOIN dec.TipoDecision t ON t.TipoId = d.TipoId WHERE d.Token = @Token;
    IF @Id IS NULL THROW 50404, N'La decisión no existe.', 1;
    IF @Creador <> @ActorId OR seg.fn_TienePermiso(@ActorId, @RolActivo, 'decisiones', 'editar') = 0
        THROW 50403, N'Solo quien registró la decisión la envía a aprobación (con el permiso de editar decisiones).', 1;
    IF @Estado <> N'Borrador' THROW 50409, N'La decisión ya fue enviada.', 1;
    IF @TipoActivo = 0 THROW 50422, N'El tipo de decisión está inactivo: elija otro tipo.', 1;
    IF NOT EXISTS (SELECT 1 FROM dec.DecisionAprobador WHERE DecisionId = @Id)
        THROW 50422, N'La decisión no tiene aprobadores: agregue al menos uno antes de enviarla.', 1;

    UPDATE dec.Decision SET Estado = N'En aprobación', Secuencial = @Secuencial, FechaEnvio = SYSDATETIME(), Version = 1, Huella = NULL WHERE DecisionId = @Id;
    UPDATE dec.Decision SET Huella = dec.fn_Huella(@Id) WHERE DecisionId = @Id;
    INSERT dec.DecisionVersion (DecisionId, Version, Titulo, Descripcion, Area, Fecha, Huella, Motivo, UsuarioId)
    SELECT DecisionId, 1, Titulo, Descripcion, Area, Fecha, Huella, N'Envío a aprobación', @ActorId FROM dec.Decision WHERE DecisionId = @Id;
    DECLARE @Detalle NVARCHAR(500) = (SELECT CONCAT(CASE WHEN @Secuencial = 1 THEN N'En secuencia: ' ELSE N'Todos a la vez: ' END,
        (SELECT STRING_AGG(u.Usuario, ', ') WITHIN GROUP (ORDER BY a.Orden) FROM dec.DecisionAprobador a JOIN seg.Usuario u ON u.UsuarioId = a.UsuarioId WHERE a.DecisionId = @Id),
        N' · huella ', CONVERT(VARCHAR(64), Huella, 2)) FROM dec.Decision WHERE DecisionId = @Id);
    INSERT dec.DecisionEvento (DecisionId, UsuarioId, Evento, Detalle, Ip) VALUES (@Id, @ActorId, N'Envío a aprobación', @Detalle, @Ip);
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, N'Decisiones', N'Envío a aprobación de la decisión ' + @Codigo, @Detalle, @Ip);
    COMMIT;
END;
