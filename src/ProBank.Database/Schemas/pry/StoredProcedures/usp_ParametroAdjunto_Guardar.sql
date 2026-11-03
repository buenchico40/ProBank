-- Cambia los tipos de archivo y el tamaño máximo de los adjuntos de un ámbito (Item o Tarea). La API valida las extensiones.
CREATE PROCEDURE pry.usp_ParametroAdjunto_Guardar
    @Ambito VARCHAR(10), @Extensiones VARCHAR(200), @MaximoMB INT, @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'paramproy', 'editar') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para editar la parametrización.', 1;
    BEGIN TRAN;
    UPDATE pry.ParametroAdjunto SET Extensiones = @Extensiones, MaximoMB = @MaximoMB WHERE Ambito = @Ambito;
    IF @@ROWCOUNT = 0 THROW 50404, N'El ámbito de adjuntos no existe.', 1;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Portafolio', N'Parametrización de adjuntos · ' + @Ambito, CONCAT(@Extensiones, N' · máximo ', @MaximoMB, N' MB'), @Ip);
    COMMIT;
END;
