-- Crea (@InformeId NULL) o edita el periodo de un informe de seguimiento. Editar el periodo lo devuelve a Borrador.
CREATE PROCEDURE pry.usp_Informe_Guardar
    @InformeId INT = NULL, @Tipo VARCHAR(10), @Desde DATE, @Hasta DATE, @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45), @Id INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'informes', CASE WHEN @InformeId IS NULL THEN 'crear' ELSE 'editar' END) = 0
        THROW 50403, N'Tu rol activo no tiene permiso para registrar informes.', 1;
    IF @Hasta < @Desde THROW 50422, N'El periodo no es válido.', 1;
    BEGIN TRAN;
    IF @InformeId IS NULL
    BEGIN
        INSERT pry.Informe (Tipo, Desde, Hasta, CreadoPorId) VALUES (@Tipo, @Desde, @Hasta, @ActorId);
        SET @Id = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE pry.Informe SET Tipo = @Tipo, Desde = @Desde, Hasta = @Hasta, Estado = 'Borrador', ContenidoHtml = NULL WHERE InformeId = @InformeId;
        IF @@ROWCOUNT = 0 THROW 50404, N'El informe no existe.', 1;
        SET @Id = @InformeId;
    END;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    SELECT NULLIF(@ActorId, 0), @RolActivo, 'Portafolio', CASE WHEN @InformeId IS NULL THEN N'Nuevo informe ' ELSE N'Edición del informe ' END + Codigo,
           CONCAT(@Tipo, N' · ', CONVERT(CHAR(10), @Desde, 103), N' – ', CONVERT(CHAR(10), @Hasta, 103)), @Ip
    FROM pry.Informe WHERE InformeId = @Id;
    COMMIT;
END;
