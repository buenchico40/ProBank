-- Opciones para configurar consultas y envíos: roles, usuarios activos (aprobadores y destinatarios) y bases de datos.
CREATE PROCEDURE rpt.usp_Consulta_Opciones
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Nombre FROM seg.Rol ORDER BY Nombre;
    SELECT Token, Nombre, Correo FROM seg.Usuario WHERE Estado = 'Activo' ORDER BY Nombre;
    SELECT Codigo, Nombre, Estado FROM rpt.Conexion ORDER BY Nombre;
END;
