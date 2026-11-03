-- Elimina una base de datos que ninguna consulta, ejecución ni programación usa; si está en uso, se desactiva.
CREATE PROCEDURE rpt.usp_Conexion_Eliminar @Codigo VARCHAR(20), @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'conexiones', 'eliminar') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para eliminar bases de datos.', 1;
    BEGIN TRAN;
    DECLARE @Id INT, @Nombre NVARCHAR(150);
    SELECT @Id = ConexionId, @Nombre = Nombre FROM rpt.Conexion WITH (UPDLOCK) WHERE Codigo = @Codigo;
    IF @Id IS NULL THROW 50404, N'La base de datos no existe.', 1;
    IF EXISTS (SELECT 1 FROM rpt.ConsultaConexion WHERE ConexionId = @Id) OR EXISTS (SELECT 1 FROM rpt.Ejecucion WHERE ConexionId = @Id)
       OR EXISTS (SELECT 1 FROM rpt.Programacion WHERE ConexionId = @Id)
        THROW 50422, N'La base de datos está en uso por consultas o ejecuciones: desactívela en lugar de eliminarla.', 1;
    DELETE rpt.Conexion WHERE ConexionId = @Id;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Consultas dinámicas', N'Eliminación de la base de datos ' + @Codigo, @Nombre, @Ip);
    COMMIT;
END;
