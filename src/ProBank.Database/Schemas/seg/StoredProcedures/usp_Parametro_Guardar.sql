-- Seguridad › Parámetros de acceso. @Valores: JSON [{"clave":"...","valor":n}]; cada valor dentro de su rango.
CREATE PROCEDURE seg.usp_Parametro_Guardar
    @Valores NVARCHAR(MAX), @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'params', 'editar') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para cambiar los parámetros de acceso.', 1;
    DECLARE @v TABLE (Clave VARCHAR(60) PRIMARY KEY, Valor INT);
    INSERT @v SELECT Clave, Valor FROM OPENJSON(@Valores) WITH (Clave VARCHAR(60) '$.clave', Valor INT '$.valor');
    IF EXISTS (SELECT 1 FROM @v v LEFT JOIN seg.ParametroAcceso p ON p.Clave = v.Clave
               WHERE p.Clave IS NULL OR v.Valor IS NULL OR v.Valor NOT BETWEEN p.Minimo AND p.Maximo)
        THROW 50422, N'Algún parámetro no existe o está fuera de su rango permitido.', 1;

    DECLARE @Detalle NVARCHAR(2000) = (SELECT STRING_AGG(CONCAT(p.Nombre, N': ', p.Valor, N' → ', v.Valor), N' · ')
                                       FROM @v v JOIN seg.ParametroAcceso p ON p.Clave = v.Clave
                                       WHERE p.Valor <> CAST(v.Valor AS NVARCHAR(200)));
    BEGIN TRAN;
    UPDATE p SET Valor = CAST(v.Valor AS NVARCHAR(200)) FROM seg.ParametroAcceso p JOIN @v v ON v.Clave = p.Clave;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Seguridad', N'Edición de parámetros de acceso', ISNULL(@Detalle, N'Sin cambios'), @Ip);
    COMMIT;
END;
