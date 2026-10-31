CREATE PROCEDURE seg.usp_Usuario_ListarActivos
AS
BEGIN
    SET NOCOUNT ON;
    SELECT UsuarioId, Usuario, Nombre FROM seg.Usuario WHERE Estado = 'Activo' ORDER BY Nombre;
END;
