-- DEC-02: crea (@Codigo NULL, código TDE-NN) o edita un tipo de decisión con sus aprobadores
-- (@Aprobadores: [{"usuarioId":1,"orden":1}]). Los cambios rigen para las decisiones que se envíen después:
-- las que ya están en aprobación conservan sus aprobadores.
CREATE PROCEDURE dec.usp_Tipo_Guardar
    @Codigo VARCHAR(10) = NULL, @Nombre NVARCHAR(100), @Descripcion NVARCHAR(300) = NULL, @Secuencial BIT, @Activo BIT, @Aprobadores NVARCHAR(MAX),
    @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45), @CodigoGuardado VARCHAR(10) OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'tiposdec', CASE WHEN @Codigo IS NULL THEN 'crear' ELSE 'editar' END) = 0
        THROW 50403, N'Tu rol activo no tiene permiso para guardar tipos de decisión.', 1;
    DECLARE @Lista TABLE (UsuarioId INT PRIMARY KEY, Orden INT NOT NULL);
    INSERT @Lista (UsuarioId, Orden)
    SELECT j.usuarioId, j.orden FROM OPENJSON(@Aprobadores) WITH (usuarioId INT, orden INT) j;
    IF NOT EXISTS (SELECT 1 FROM @Lista) THROW 50422, N'Agregue al menos un aprobador.', 1;
    IF EXISTS (SELECT 1 FROM @Lista l LEFT JOIN seg.Usuario u ON u.UsuarioId = l.UsuarioId AND u.Estado = 'Activo' WHERE u.UsuarioId IS NULL)
        THROW 50422, N'Los aprobadores deben ser usuarios activos de ProBank.', 1;
    IF EXISTS (SELECT 1 FROM @Lista WHERE Orden NOT BETWEEN 1 AND 99) THROW 50422, N'El orden va de 1 a 99.', 1;

    BEGIN TRAN;
    IF EXISTS (SELECT 1 FROM dec.TipoDecision WITH (UPDLOCK, HOLDLOCK) WHERE Nombre = @Nombre AND Codigo <> ISNULL(@Codigo, ''))
        THROW 50409, N'Ya existe un tipo de decisión con ese nombre.', 1;
    DECLARE @TipoId INT;
    IF @Codigo IS NULL
    BEGIN
        DECLARE @n INT = 1 + ISNULL((SELECT MAX(CAST(SUBSTRING(Codigo, 5, 10) AS INT)) FROM dec.TipoDecision WITH (UPDLOCK, HOLDLOCK)), 0);
        SET @CodigoGuardado = 'TDE-' + CASE WHEN @n < 10 THEN '0' ELSE '' END + CAST(@n AS VARCHAR(10));
        INSERT dec.TipoDecision (Codigo, Nombre, Descripcion, Secuencial, Activo) VALUES (@CodigoGuardado, @Nombre, @Descripcion, @Secuencial, @Activo);
        SET @TipoId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        SET @TipoId = (SELECT TipoId FROM dec.TipoDecision WITH (UPDLOCK) WHERE Codigo = @Codigo);
        IF @TipoId IS NULL THROW 50404, N'El tipo de decisión no existe.', 1;
        UPDATE dec.TipoDecision SET Nombre = @Nombre, Descripcion = @Descripcion, Secuencial = @Secuencial, Activo = @Activo WHERE TipoId = @TipoId;
        SET @CodigoGuardado = @Codigo;
    END;
    DELETE dec.TipoAprobador WHERE TipoId = @TipoId;
    INSERT dec.TipoAprobador (TipoId, UsuarioId, Orden) SELECT @TipoId, UsuarioId, Orden FROM @Lista;

    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, N'Decisiones',
            CASE WHEN @Codigo IS NULL THEN N'Nuevo tipo de decisión ' ELSE N'Edición del tipo de decisión ' END + @CodigoGuardado,
            CONCAT(@Nombre, CASE WHEN @Secuencial = 1 THEN N' · en secuencia' ELSE N' · todos a la vez' END, CASE WHEN @Activo = 0 THEN N' · inactivo' END,
                   N' · aprobadores: ', (SELECT STRING_AGG(u.Usuario, ', ')
                                         WITHIN GROUP (ORDER BY l.Orden) FROM @Lista l JOIN seg.Usuario u ON u.UsuarioId = l.UsuarioId)), @Ip);
    COMMIT;
END;
