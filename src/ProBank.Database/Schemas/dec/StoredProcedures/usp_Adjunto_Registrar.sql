-- DEC-08: registra un archivo ya guardado en el almacén como adjunto de una decisión en borrador (solo quien la registró).
CREATE PROCEDURE dec.usp_Adjunto_Registrar
    @Token UNIQUEIDENTIFIER, @DecisionToken UNIQUEIDENTIFIER, @Nombre NVARCHAR(200), @TipoContenido VARCHAR(150), @Tamano BIGINT, @Hash VARBINARY(32),
    @Ruta VARCHAR(300), @MaximoArchivos INT, @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRAN;
    DECLARE @Id INT, @Codigo VARCHAR(12), @Estado NVARCHAR(20), @Creador INT;
    SELECT @Id = DecisionId, @Codigo = Codigo, @Estado = Estado, @Creador = CreadoPorId FROM dec.Decision WITH (UPDLOCK) WHERE Token = @DecisionToken;
    IF @Id IS NULL THROW 50404, N'La decisión no existe.', 1;
    IF @Creador <> @ActorId OR seg.fn_TienePermiso(@ActorId, @RolActivo, 'decisiones', 'editar') = 0
        THROW 50403, N'Solo quien registró la decisión agrega sus archivos.', 1;
    IF @Estado <> N'Borrador' THROW 50409, N'La decisión ya se envió: sus archivos quedaron fijos.', 1;
    IF EXISTS (SELECT 1 FROM dec.DecisionAdjunto WHERE DecisionId = @Id AND Nombre = @Nombre)
        THROW 50409, N'La decisión ya tiene un archivo con ese nombre: quítelo o cambie el nombre.', 1;
    IF (SELECT COUNT(*) FROM dec.DecisionAdjunto WHERE DecisionId = @Id) >= @MaximoArchivos
        THROW 50422, N'La decisión llegó al máximo de archivos.', 1;
    INSERT dec.DecisionAdjunto (Token, DecisionId, Nombre, TipoContenido, Tamano, Hash, Ruta, UsuarioId)
    VALUES (@Token, @Id, @Nombre, @TipoContenido, @Tamano, @Hash, @Ruta, @ActorId);
    INSERT dec.DecisionEvento (DecisionId, UsuarioId, Evento, Detalle, Ip)
    VALUES (@Id, @ActorId, N'Archivo agregado', CONCAT(@Nombre, N' · ', @Tamano, N' bytes · SHA-256 ', CONVERT(VARCHAR(64), @Hash, 2)), @Ip);
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, N'Decisiones', N'Archivo agregado a la decisión ' + @Codigo, @Nombre, @Ip);
    COMMIT;
END;
