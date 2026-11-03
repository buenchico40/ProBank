-- Retira un comunicado publicado (programado, vigente o vencido): deja de mostrarse en el Panel.
CREATE PROCEDURE mkt.usp_Comunicado_Retirar
    @Token UNIQUEIDENTIFIER, @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'comunicados', 'aprobar') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para retirar comunicados.', 1;
    BEGIN TRAN;
    DECLARE @Codigo VARCHAR(20), @Estado VARCHAR(20);
    SELECT @Codigo = Codigo, @Estado = Estado FROM mkt.Comunicado WITH (UPDLOCK) WHERE Token = @Token;
    IF @Codigo IS NULL THROW 50404, N'El comunicado no existe.', 1;
    IF @Estado <> 'Publicado' THROW 50422, N'Solo se retira lo publicado.', 1;
    UPDATE mkt.Comunicado SET Estado = 'Retirado' WHERE Token = @Token;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Marketing', N'Retiro de ' + @Codigo, NULL, @Ip);
    COMMIT;
END;
