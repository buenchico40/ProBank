-- CFG-01: guarda los valores cambiados ([{"clave","valor"}]; la API ya validó el conjunto). Cada cambio queda en la auditoría con su
-- valor anterior y el nuevo (los secretos son solo nombres de Key Vault, nunca su contenido).
CREATE PROCEDURE cfg.usp_Parametro_Guardar
    @ValoresJson NVARCHAR(MAX), @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45), @Cambios INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'config', 'editar') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para cambiar la configuración global.', 1;
    DECLARE @Nuevos TABLE (Clave VARCHAR(100) PRIMARY KEY, Valor NVARCHAR(1000) NULL);
    INSERT @Nuevos SELECT DISTINCT j.clave, NULLIF(LTRIM(RTRIM(j.valor)), N'')
    FROM OPENJSON(@ValoresJson) WITH (clave VARCHAR(100), valor NVARCHAR(1000)) j;
    IF EXISTS (SELECT 1 FROM @Nuevos n WHERE NOT EXISTS (SELECT 1 FROM cfg.Parametro p WHERE p.Clave = n.Clave))
        THROW 50422, N'Uno de los parámetros no existe.', 1;

    DECLARE @Cambiados TABLE (Clave VARCHAR(100), Nombre NVARCHAR(150), Categoria NVARCHAR(60), Anterior NVARCHAR(1000), Nuevo NVARCHAR(1000));
    BEGIN TRAN;
    UPDATE p SET Valor = n.Valor, ModificadoPorId = NULLIF(@ActorId, 0), FechaModificacion = SYSDATETIME()
    OUTPUT inserted.Clave, inserted.Nombre, inserted.Categoria, deleted.Valor, inserted.Valor INTO @Cambiados
    FROM cfg.Parametro p WITH (UPDLOCK) JOIN @Nuevos n ON n.Clave = p.Clave
    WHERE ISNULL(p.Valor, N'') <> ISNULL(n.Valor, N'') COLLATE Latin1_General_BIN2;
    SET @Cambios = @@ROWCOUNT;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    SELECT NULLIF(@ActorId, 0), @RolActivo, 'Configuración', N'Cambio de parámetro: ' + c.Categoria + N' · ' + c.Nombre,
           CONCAT(c.Clave, N': ', ISNULL(c.Anterior, N'(vacío)'), N' → ', ISNULL(c.Nuevo, N'(vacío)')), @Ip
    FROM @Cambiados c;
    COMMIT;
END;
