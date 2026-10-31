-- SEG-07: la API compara el sello del token con el de la base (con caché corta) en cada petición.
CREATE PROCEDURE seg.usp_Usuario_ObtenerSello
    @UsuarioId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT SelloSesion, Estado FROM seg.Usuario WHERE UsuarioId = @UsuarioId;
END;
