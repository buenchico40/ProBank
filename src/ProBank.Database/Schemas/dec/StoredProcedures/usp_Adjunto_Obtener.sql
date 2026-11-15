-- DEC-08: datos de un adjunto para descargarlo, solo para quien puede ver la decisión; la descarga queda en la trazabilidad.
CREATE PROCEDURE dec.usp_Adjunto_Obtener
    @Token UNIQUEIDENTIFIER, @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id INT, @Nombre NVARCHAR(200);
    SELECT @Id = DecisionId, @Nombre = Nombre FROM dec.DecisionAdjunto WHERE Token = @Token;
    IF @Id IS NULL THROW 50404, N'El archivo no existe.', 1;
    IF dec.fn_PuedeVer(@ActorId, @RolActivo, @Id) = 0 THROW 50403, N'No puede ver esta decisión ni sus archivos.', 1;
    INSERT dec.DecisionEvento (DecisionId, UsuarioId, Evento, Detalle, Ip) VALUES (@Id, @ActorId, N'Descarga de archivo', @Nombre, @Ip);
    SELECT Nombre, TipoContenido, Ruta, CONVERT(VARCHAR(64), Hash, 2) AS Hash FROM dec.DecisionAdjunto WHERE Token = @Token;
END;
