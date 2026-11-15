-- DEC-01, DEC-02 y DEC-05: registra (@Token NULL, código DEC-NNNN) o edita una decisión en borrador con sus notificados
-- (@Notificados: ids de usuario separados por comas). Al registrarla, o si en borrador cambia de tipo, toma como aprobadores los
-- de su tipo (activos y nunca quien la registró); después se agregan o quitan aparte.
-- Solo la edita quien la registró.
CREATE PROCEDURE dec.usp_Decision_Guardar
    @Token UNIQUEIDENTIFIER = NULL, @TipoCodigo VARCHAR(10), @Titulo NVARCHAR(200), @Descripcion NVARCHAR(MAX),
    @Area NVARCHAR(100), @Fecha DATE, @Notificados VARCHAR(MAX) = NULL,
    @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45), @TokenGuardado UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'decisiones', CASE WHEN @Token IS NULL THEN 'crear' ELSE 'editar' END) = 0
        THROW 50403, N'Tu rol activo no tiene permiso para registrar decisiones.', 1;
    DECLARE @TipoId INT = (SELECT TipoId FROM dec.TipoDecision WHERE Codigo = @TipoCodigo AND Activo = 1);
    IF @TipoId IS NULL THROW 50422, N'Elija un tipo de decisión activo.', 1;
    DECLARE @Notif TABLE (UsuarioId INT PRIMARY KEY);
    INSERT @Notif SELECT DISTINCT TRY_CAST(LTRIM(RTRIM(value)) AS INT) FROM STRING_SPLIT(ISNULL(@Notificados, ''), ',') WHERE LTRIM(RTRIM(value)) <> '' AND TRY_CAST(LTRIM(RTRIM(value)) AS INT) IS NOT NULL;
    IF EXISTS (SELECT 1 FROM @Notif n LEFT JOIN seg.Usuario u ON u.UsuarioId = n.UsuarioId AND u.Estado = 'Activo' WHERE u.UsuarioId IS NULL)
        THROW 50422, N'Los notificados deben ser usuarios activos de ProBank.', 1;

    BEGIN TRAN;
    DECLARE @Id INT, @Codigo VARCHAR(12), @TipoAnterior INT, @Proponer BIT = 0;
    IF @Token IS NULL
    BEGIN
        DECLARE @n INT = 1 + ISNULL((SELECT MAX(CAST(SUBSTRING(Codigo, 5, 10) AS INT)) FROM dec.Decision WITH (UPDLOCK, HOLDLOCK)), 0);
        SET @Codigo = 'DEC-' + RIGHT('000' + CAST(@n AS VARCHAR(10)), CASE WHEN @n < 10000 THEN 4 ELSE 10 END);
        SET @TokenGuardado = NEWID();
        INSERT dec.Decision (Token, Codigo, TipoId, Titulo, Descripcion, Area, Fecha, CreadoPorId)
        VALUES (@TokenGuardado, @Codigo, @TipoId, @Titulo, @Descripcion, @Area, @Fecha, @ActorId);
        SET @Id = SCOPE_IDENTITY();
        SET @Proponer = 1;
        INSERT dec.DecisionEvento (DecisionId, UsuarioId, Evento, Detalle, Ip) VALUES (@Id, @ActorId, N'Registro', @Titulo, @Ip);
    END
    ELSE
    BEGIN
        DECLARE @Estado NVARCHAR(20), @Creador INT;
        SELECT @Id = DecisionId, @Codigo = Codigo, @Estado = Estado, @Creador = CreadoPorId, @TipoAnterior = TipoId FROM dec.Decision WITH (UPDLOCK) WHERE Token = @Token;
        IF @Id IS NULL THROW 50404, N'La decisión no existe.', 1;
        IF @Estado <> N'Borrador' THROW 50409, N'Solo se edita una decisión en borrador: enviada, su contenido queda fijo para las firmas.', 1;
        IF @Creador <> @ActorId THROW 50403, N'Solo quien registró la decisión puede editarla.', 1;
        UPDATE dec.Decision SET TipoId = @TipoId, Titulo = @Titulo, Descripcion = @Descripcion, Area = @Area, Fecha = @Fecha WHERE DecisionId = @Id;
        SET @TokenGuardado = @Token;
        SET @Proponer = CASE WHEN @TipoAnterior <> @TipoId THEN 1 ELSE 0 END;
        INSERT dec.DecisionEvento (DecisionId, UsuarioId, Evento, Detalle, Ip)
        VALUES (@Id, @ActorId, N'Edición', CONCAT(@Titulo, N' · ', @TipoCodigo, N' · ', @Area), @Ip);
    END;

    IF @Proponer = 1
    BEGIN
        DELETE dec.DecisionAprobador WHERE DecisionId = @Id;
        INSERT dec.DecisionAprobador (DecisionId, UsuarioId, Orden, Origen, AgregadoPorId)
        SELECT @Id, a.UsuarioId, a.Orden, N'Tipo', @ActorId
        FROM dec.TipoAprobador a JOIN seg.Usuario u ON u.UsuarioId = a.UsuarioId AND u.Estado = 'Activo'
        WHERE a.TipoId = @TipoId AND a.UsuarioId <> @ActorId;
        INSERT dec.DecisionEvento (DecisionId, UsuarioId, Evento, Detalle, Ip)
        VALUES (@Id, @ActorId, N'Aprobadores del tipo', ISNULL((SELECT STRING_AGG(u.Usuario, ', ') WITHIN GROUP (ORDER BY a.Orden)
                FROM dec.DecisionAprobador a JOIN seg.Usuario u ON u.UsuarioId = a.UsuarioId WHERE a.DecisionId = @Id), N'Ninguno: agréguelos antes de enviarla'), @Ip);
    END;

    -- Notificados: en borrador se reemplazan (quién entra y quién sale queda en la trazabilidad)
    INSERT dec.DecisionEvento (DecisionId, UsuarioId, Evento, Detalle, Ip)
    SELECT @Id, @ActorId, N'Notificado agregado', u.Nombre + N' (' + u.Usuario + N')', @Ip
    FROM @Notif n JOIN seg.Usuario u ON u.UsuarioId = n.UsuarioId
    WHERE NOT EXISTS (SELECT 1 FROM dec.DecisionNotificado x WHERE x.DecisionId = @Id AND x.UsuarioId = n.UsuarioId);
    INSERT dec.DecisionEvento (DecisionId, UsuarioId, Evento, Detalle, Ip)
    SELECT @Id, @ActorId, N'Notificado quitado', u.Nombre + N' (' + u.Usuario + N')', @Ip
    FROM dec.DecisionNotificado x JOIN seg.Usuario u ON u.UsuarioId = x.UsuarioId
    WHERE x.DecisionId = @Id AND NOT EXISTS (SELECT 1 FROM @Notif n WHERE n.UsuarioId = x.UsuarioId);
    DELETE x FROM dec.DecisionNotificado x WHERE x.DecisionId = @Id AND NOT EXISTS (SELECT 1 FROM @Notif n WHERE n.UsuarioId = x.UsuarioId);
    INSERT dec.DecisionNotificado (DecisionId, UsuarioId, AgregadoPorId)
    SELECT @Id, n.UsuarioId, @ActorId FROM @Notif n WHERE NOT EXISTS (SELECT 1 FROM dec.DecisionNotificado x WHERE x.DecisionId = @Id AND x.UsuarioId = n.UsuarioId);

    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, N'Decisiones',
            CASE WHEN @Token IS NULL THEN N'Nueva decisión ' ELSE N'Edición de la decisión ' END + @Codigo,
            CONCAT(@Titulo, N' · ', @TipoCodigo, N' · ', @Area,
                   N' · ', (SELECT COUNT(*) FROM @Notif), N' notificados'), @Ip);
    COMMIT;
END;
