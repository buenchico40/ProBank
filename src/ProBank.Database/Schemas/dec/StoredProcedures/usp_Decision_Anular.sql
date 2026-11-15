-- DEC-01: anula una decisión en borrador o en aprobación, con motivo. La anula quien la registró o quien tiene el permiso de
-- eliminar decisiones. Las firmas ya hechas se conservan.
CREATE PROCEDURE dec.usp_Decision_Anular
    @Token UNIQUEIDENTIFIER, @Motivo NVARCHAR(500), @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NULLIF(LTRIM(RTRIM(@Motivo)), N'') IS NULL THROW 50422, N'Indique el motivo de la anulación.', 1;
    BEGIN TRAN;
    DECLARE @Id INT, @Codigo VARCHAR(12), @Estado NVARCHAR(20), @Creador INT;
    SELECT @Id = DecisionId, @Codigo = Codigo, @Estado = Estado, @Creador = CreadoPorId FROM dec.Decision WITH (UPDLOCK) WHERE Token = @Token;
    IF @Id IS NULL THROW 50404, N'La decisión no existe.', 1;
    IF NOT ((@Creador = @ActorId AND seg.fn_TienePermiso(@ActorId, @RolActivo, 'decisiones', 'editar') = 1)
            OR seg.fn_TienePermiso(@ActorId, @RolActivo, 'decisiones', 'eliminar') = 1)
        THROW 50403, N'Solo anula la decisión quien la registró o quien tiene el permiso de anular decisiones.', 1;
    IF @Estado NOT IN (N'Borrador', N'En aprobación') THROW 50409, N'Solo se anula una decisión en borrador o en aprobación.', 1;
    UPDATE dec.Decision SET Estado = N'Anulada', FechaCierre = SYSDATETIME(), MotivoAnulacion = LTRIM(RTRIM(@Motivo)) WHERE DecisionId = @Id;
    INSERT dec.DecisionEvento (DecisionId, UsuarioId, Evento, Detalle, Ip) VALUES (@Id, @ActorId, N'Anulación', LTRIM(RTRIM(@Motivo)), @Ip);
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, N'Decisiones', N'Anulación de la decisión ' + @Codigo, CONCAT(N'Estaba ', @Estado, N' · ', @Motivo), @Ip);
    COMMIT;
END;
