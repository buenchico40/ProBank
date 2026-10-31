-- Solo inserta: la auditoría no se edita ni se elimina (el usuario de la app no tiene UPDATE/DELETE sobre aud).
CREATE PROCEDURE aud.usp_Auditoria_Registrar
    @UsuarioId INT, @RolActivo VARCHAR(60), @Modulo VARCHAR(60), @Accion NVARCHAR(200), @Detalle NVARCHAR(2000), @Ip VARCHAR(45)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip) VALUES (NULLIF(@UsuarioId, 0), @RolActivo, @Modulo, @Accion, @Detalle, @Ip);
END;
