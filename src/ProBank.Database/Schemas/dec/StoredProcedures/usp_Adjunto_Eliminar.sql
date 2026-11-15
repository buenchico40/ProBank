-- DEC-08: quita un adjunto de una decisión en borrador (solo quien la registró) y devuelve su ruta para borrar el archivo.
CREATE PROCEDURE dec.usp_Adjunto_Eliminar
    @Token UNIQUEIDENTIFIER, @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45), @Ruta VARCHAR(300) OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRAN;
    DECLARE @AdjuntoId INT, @Id INT, @Nombre NVARCHAR(200), @Codigo VARCHAR(12), @Estado NVARCHAR(20), @Creador INT;
    SELECT @AdjuntoId = a.AdjuntoId, @Id = d.DecisionId, @Nombre = a.Nombre, @Ruta = a.Ruta, @Codigo = d.Codigo, @Estado = d.Estado, @Creador = d.CreadoPorId
    FROM dec.DecisionAdjunto a JOIN dec.Decision d WITH (UPDLOCK) ON d.DecisionId = a.DecisionId WHERE a.Token = @Token;
    IF @AdjuntoId IS NULL THROW 50404, N'El archivo no existe.', 1;
    IF @Creador <> @ActorId OR seg.fn_TienePermiso(@ActorId, @RolActivo, 'decisiones', 'editar') = 0
        THROW 50403, N'Solo quien registró la decisión quita sus archivos.', 1;
    IF @Estado <> N'Borrador' THROW 50409, N'La decisión ya se envió: sus archivos quedaron fijos.', 1;
    DELETE dec.DecisionAdjunto WHERE AdjuntoId = @AdjuntoId;
    INSERT dec.DecisionEvento (DecisionId, UsuarioId, Evento, Detalle, Ip) VALUES (@Id, @ActorId, N'Archivo quitado', @Nombre, @Ip);
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, N'Decisiones', N'Archivo quitado de la decisión ' + @Codigo, @Nombre, @Ip);
    COMMIT;
END;
