-- Publicar (queda disponible para los roles definidos) o Retirar (vuelve a Borrador y no se ejecuta ni se envía programada).
-- Para publicar: definición leída, al menos una base activa y un rol, y las listas con sus opciones.
CREATE PROCEDURE rpt.usp_Consulta_CambiarEstado
    @Codigo VARCHAR(20), @Accion VARCHAR(10), @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'consultas', 'aprobar') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para publicar o retirar consultas.', 1;
    DECLARE @Id INT, @Estado VARCHAR(20), @Leida DATETIME2(0);
    SELECT @Id = ConsultaId, @Estado = Estado, @Leida = DefinicionLeida FROM rpt.Consulta WHERE Codigo = @Codigo;
    IF @Id IS NULL THROW 50404, N'La consulta no existe.', 1;
    IF @Accion = 'Publicar'
    BEGIN
        IF @Estado = 'Publicada' THROW 50422, N'La consulta ya está publicada.', 1;
        IF @Leida IS NULL THROW 50422, N'Lea la definición del procedimiento antes de publicar.', 1;
        IF NOT EXISTS (SELECT 1 FROM rpt.ConsultaConexion cc JOIN rpt.Conexion c ON c.ConexionId = cc.ConexionId AND c.Estado = 'Activo' WHERE cc.ConsultaId = @Id)
           OR NOT EXISTS (SELECT 1 FROM rpt.ConsultaRol WHERE ConsultaId = @Id)
            THROW 50422, N'Defina al menos una base de datos activa y un rol antes de publicar.', 1;
        IF EXISTS (SELECT 1 FROM rpt.Parametro WHERE ConsultaId = @Id AND Tipo = 'Lista' AND Opciones IS NULL)
            THROW 50422, N'Un parámetro de tipo lista necesita sus opciones.', 1;
        UPDATE rpt.Consulta SET Estado = 'Publicada' WHERE ConsultaId = @Id;
    END
    ELSE IF @Accion = 'Retirar'
    BEGIN
        IF @Estado <> 'Publicada' THROW 50422, N'Solo se retiran consultas publicadas.', 1;
        UPDATE rpt.Consulta SET Estado = 'Borrador' WHERE ConsultaId = @Id;
    END
    ELSE THROW 50422, N'La acción es Publicar o Retirar.', 1;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Consultas dinámicas',
            CASE @Accion WHEN 'Publicar' THEN N'Publicación de la consulta ' ELSE N'Retiro de la consulta ' END + @Codigo, NULL, @Ip);
END;
