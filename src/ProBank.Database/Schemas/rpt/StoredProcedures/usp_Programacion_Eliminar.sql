-- Elimina un envío programado (sus corridas ya ejecutadas quedan en la auditoría de ejecuciones).
CREATE PROCEDURE rpt.usp_Programacion_Eliminar @Codigo VARCHAR(10), @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'consultas', 'editar') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para eliminar envíos programados.', 1;
    BEGIN TRAN;
    DECLARE @Id INT, @Consulta VARCHAR(20);
    SELECT @Id = p.ProgramacionId, @Consulta = q.Codigo FROM rpt.Programacion p WITH (UPDLOCK) JOIN rpt.Consulta q ON q.ConsultaId = p.ConsultaId WHERE p.Codigo = @Codigo;
    IF @Id IS NULL THROW 50404, N'El envío programado no existe.', 1;
    DELETE rpt.ProgramacionDestinatario WHERE ProgramacionId = @Id;
    DELETE rpt.Programacion WHERE ProgramacionId = @Id;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Consultas dinámicas', N'Eliminación del envío programado ' + @Codigo + N' de ' + @Consulta, NULL, @Ip);
    COMMIT;
END;
