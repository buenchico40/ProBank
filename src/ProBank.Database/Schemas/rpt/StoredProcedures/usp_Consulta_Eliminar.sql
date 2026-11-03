-- Elimina una consulta en Borrador que nunca se ejecutó, con sus parámetros, relaciones y envíos programados.
CREATE PROCEDURE rpt.usp_Consulta_Eliminar @Codigo VARCHAR(20), @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'consultas', 'eliminar') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para eliminar consultas.', 1;
    BEGIN TRAN;
    DECLARE @Id INT, @Estado VARCHAR(20), @Nombre NVARCHAR(150);
    SELECT @Id = ConsultaId, @Estado = Estado, @Nombre = Nombre FROM rpt.Consulta WITH (UPDLOCK) WHERE Codigo = @Codigo;
    IF @Id IS NULL THROW 50404, N'La consulta no existe.', 1;
    IF @Estado <> 'Borrador' THROW 50422, N'Solo se eliminan consultas en borrador: retírela primero.', 1;
    IF EXISTS (SELECT 1 FROM rpt.Ejecucion WHERE ConsultaId = @Id)
        THROW 50422, N'La consulta ya tiene ejecuciones en la auditoría: no se elimina, queda en Borrador.', 1;
    DELETE d FROM rpt.ProgramacionDestinatario d JOIN rpt.Programacion p ON p.ProgramacionId = d.ProgramacionId WHERE p.ConsultaId = @Id;
    DELETE rpt.Programacion WHERE ConsultaId = @Id;
    DELETE rpt.Parametro WHERE ConsultaId = @Id;
    DELETE rpt.ConsultaConexion WHERE ConsultaId = @Id;
    DELETE rpt.ConsultaRol WHERE ConsultaId = @Id;
    DELETE rpt.ConsultaAprobador WHERE ConsultaId = @Id;
    DELETE rpt.Consulta WHERE ConsultaId = @Id;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Consultas dinámicas', N'Eliminación de la consulta ' + @Codigo, @Nombre, @Ip);
    COMMIT;
END;
