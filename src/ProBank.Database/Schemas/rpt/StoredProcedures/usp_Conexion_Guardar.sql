-- SQL-02: crea (@Codigo NULL, código CNX-NNN) o edita una base de datos. Solo guarda el nombre del secreto de Key Vault con la
-- cadena de conexión; la cadena la carga operaciones en Key Vault y la API la lee al ejecutar.
CREATE PROCEDURE rpt.usp_Conexion_Guardar
    @Codigo VARCHAR(20) = NULL, @Nombre NVARCHAR(150), @Servidor VARCHAR(200), @BaseDatos VARCHAR(128), @Autenticacion VARCHAR(40),
    @SecretoKeyVault VARCHAR(127), @Estado VARCHAR(20),
    @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45), @CodigoGuardado VARCHAR(20) OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'conexiones', CASE WHEN @Codigo IS NULL THEN 'crear' ELSE 'editar' END) = 0
        THROW 50403, N'Tu rol activo no tiene permiso para guardar bases de datos.', 1;
    BEGIN TRAN;
    IF EXISTS (SELECT 1 FROM rpt.Conexion WITH (UPDLOCK, HOLDLOCK) WHERE Nombre = @Nombre AND Codigo <> ISNULL(@Codigo, ''))
        THROW 50409, N'Ya existe una base de datos con ese nombre.', 1;
    IF @Codigo IS NULL
    BEGIN
        DECLARE @n INT = 1 + ISNULL((SELECT MAX(CAST(SUBSTRING(Codigo, 5, 10) AS INT)) FROM rpt.Conexion WHERE Codigo LIKE 'CNX-%'), 0);
        SET @CodigoGuardado = 'CNX-' + RIGHT('00' + CAST(@n AS VARCHAR(10)), 3);
        INSERT rpt.Conexion (Codigo, Nombre, Servidor, BaseDatos, Autenticacion, SecretoKeyVault, Estado)
        VALUES (@CodigoGuardado, @Nombre, @Servidor, @BaseDatos, @Autenticacion, @SecretoKeyVault, @Estado);
    END
    ELSE
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM rpt.Conexion WHERE Codigo = @Codigo) THROW 50404, N'La base de datos no existe.', 1;
        UPDATE rpt.Conexion SET Nombre = @Nombre, Servidor = @Servidor, BaseDatos = @BaseDatos, Autenticacion = @Autenticacion,
               SecretoKeyVault = @SecretoKeyVault, Estado = @Estado
        WHERE Codigo = @Codigo;
        SET @CodigoGuardado = @Codigo;
    END;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Consultas dinámicas',
            CASE WHEN @Codigo IS NULL THEN N'Nueva base de datos ' ELSE N'Edición de la base de datos ' END + @CodigoGuardado,
            CONCAT(@Nombre, N' · ', @Servidor, N'/', @BaseDatos, N' · secreto ', @SecretoKeyVault, N' · ', @Estado), @Ip);
    COMMIT;
END;
