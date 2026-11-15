-- DEC-01 y DEC-05: ven una decisión quien tiene el permiso decisiones·ver, quien la registró, sus aprobadores y sus notificados.
CREATE FUNCTION dec.fn_PuedeVer (@ActorId INT, @RolActivo VARCHAR(60), @DecisionId INT)
RETURNS BIT
AS
BEGIN
    RETURN CASE WHEN seg.fn_TienePermiso(@ActorId, @RolActivo, 'decisiones', 'ver') = 1
                  OR EXISTS (SELECT 1 FROM dec.Decision WHERE DecisionId = @DecisionId AND CreadoPorId = @ActorId)
                  OR EXISTS (SELECT 1 FROM dec.DecisionAprobador WHERE DecisionId = @DecisionId AND UsuarioId = @ActorId)
                  OR EXISTS (SELECT 1 FROM dec.DecisionNotificado WHERE DecisionId = @DecisionId AND UsuarioId = @ActorId)
                THEN 1 ELSE 0 END;
END;
