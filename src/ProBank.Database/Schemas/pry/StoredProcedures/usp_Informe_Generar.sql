-- Registra la copia generada del informe (PDF de DevExpress Reporting guardado por la API en Blob Storage) y lo marca Completado.
CREATE PROCEDURE pry.usp_Informe_Generar
    @InformeId INT, @RutaPdf VARCHAR(300), @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'informes', 'generar') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para generar informes.', 1;
    BEGIN TRAN;
    UPDATE pry.Informe SET RutaPdf = @RutaPdf, ContenidoHtml = NULL, Estado = 'Completado', GeneradoPorId = @ActorId, FechaGenerado = SYSDATETIME()
    WHERE InformeId = @InformeId;
    IF @@ROWCOUNT = 0 THROW 50404, N'El informe no existe.', 1;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    SELECT NULLIF(@ActorId, 0), @RolActivo, 'Portafolio', N'Generación del informe ' + Codigo, CONCAT(Tipo, N' · ', CONVERT(CHAR(10), Desde, 103), N' – ', CONVERT(CHAR(10), Hasta, 103)), @Ip
    FROM pry.Informe WHERE InformeId = @InformeId;
    COMMIT;
END;
