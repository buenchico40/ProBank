-- Crea (@Nueva = 1) un área del presupuesto o cambia el nombre y si está activa. El código no cambia.
CREATE PROCEDURE com.usp_Area_Guardar
    @Nueva BIT, @Codigo VARCHAR(30), @Nombre NVARCHAR(100), @Activa BIT,
    @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'areaspres', CASE WHEN @Nueva = 1 THEN 'crear' ELSE 'editar' END) = 0
        THROW 50403, N'Tu rol activo no tiene permiso para guardar áreas del presupuesto.', 1;
    BEGIN TRAN;
    IF EXISTS (SELECT 1 FROM com.Area WITH (UPDLOCK, HOLDLOCK) WHERE Nombre = @Nombre AND Codigo <> @Codigo)
        THROW 50409, N'Ya existe un área con ese nombre.', 1;
    IF @Nueva = 1
    BEGIN
        IF EXISTS (SELECT 1 FROM com.Area WHERE Codigo = @Codigo) THROW 50409, N'Ya existe un área con ese código.', 1;
        INSERT com.Area (Codigo, Nombre, Activa) VALUES (@Codigo, @Nombre, @Activa);
    END
    ELSE
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM com.Area WHERE Codigo = @Codigo) THROW 50404, N'El área no existe.', 1;
        UPDATE com.Area SET Nombre = @Nombre, Activa = @Activa WHERE Codigo = @Codigo;
    END;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Presupuesto', CASE WHEN @Nueva = 1 THEN N'Nueva área del presupuesto ' ELSE N'Edición del área del presupuesto ' END + @Codigo,
            CONCAT(@Nombre, CASE WHEN @Activa = 1 THEN N' · activa' ELSE N' · inactiva' END), @Ip);
    COMMIT;
END;
