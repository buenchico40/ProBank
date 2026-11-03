-- Guarda la definición leída del procedimiento en una base: parámetros ([{"nombre","tipoSql","tipo"}]) y columnas del resultado.
-- Los parámetros que ya existían conservan su configuración (etiqueta, obligatorio, valor por defecto, opciones); los nuevos
-- nacen obligatorios con su nombre como etiqueta y los que ya no están se borran. Si cambian los parámetros de una consulta
-- publicada, vuelve a Borrador (@VolvioBorrador).
CREATE PROCEDURE rpt.usp_Consulta_GuardarDefinicion
    @Codigo VARCHAR(20), @ParametrosJson NVARCHAR(MAX), @ColumnasJson NVARCHAR(MAX), @ConexionCodigo VARCHAR(20),
    @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45), @VolvioBorrador BIT OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'consultas', 'editar') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para editar consultas.', 1;
    DECLARE @Id INT, @Estado VARCHAR(20);
    SELECT @Id = ConsultaId, @Estado = Estado FROM rpt.Consulta WHERE Codigo = @Codigo;
    IF @Id IS NULL THROW 50404, N'La consulta no existe.', 1;

    DECLARE @Leidos TABLE (Nombre SYSNAME PRIMARY KEY, TipoSql VARCHAR(40), Tipo VARCHAR(10), Orden INT);
    INSERT @Leidos SELECT JSON_VALUE(o.value, '$.nombre'), JSON_VALUE(o.value, '$.tipoSql'), JSON_VALUE(o.value, '$.tipo'), CAST(o.[key] AS INT) + 1
    FROM OPENJSON(@ParametrosJson) o;

    SET @VolvioBorrador = CASE WHEN @Estado = 'Publicada'
        AND (EXISTS (SELECT Nombre FROM rpt.Parametro WHERE ConsultaId = @Id EXCEPT SELECT Nombre FROM @Leidos)
             OR EXISTS (SELECT Nombre FROM @Leidos EXCEPT SELECT Nombre FROM rpt.Parametro WHERE ConsultaId = @Id)) THEN 1 ELSE 0 END;
    BEGIN TRAN;
    DELETE rpt.Parametro WHERE ConsultaId = @Id AND Nombre NOT IN (SELECT Nombre FROM @Leidos);
    UPDATE p SET TipoSql = l.TipoSql, Orden = l.Orden, Tipo = CASE WHEN p.Tipo = 'Lista' THEN 'Lista' ELSE l.Tipo END
    FROM rpt.Parametro p JOIN @Leidos l ON l.Nombre = p.Nombre WHERE p.ConsultaId = @Id;
    INSERT rpt.Parametro (ConsultaId, Nombre, Etiqueta, Tipo, TipoSql, Obligatorio, Orden)
    SELECT @Id, l.Nombre, REPLACE(l.Nombre, '@', ''), l.Tipo, l.TipoSql, 1, l.Orden
    FROM @Leidos l WHERE NOT EXISTS (SELECT 1 FROM rpt.Parametro p WHERE p.ConsultaId = @Id AND p.Nombre = l.Nombre);
    UPDATE rpt.Consulta SET ColumnasJson = @ColumnasJson, DefinicionLeida = SYSDATETIME(),
           Estado = CASE WHEN @VolvioBorrador = 1 THEN 'Borrador' ELSE Estado END
    WHERE ConsultaId = @Id;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Consultas dinámicas', N'Lectura de la definición de ' + @Codigo,
            CONCAT((SELECT COUNT(*) FROM @Leidos), N' parámetro(s) leídos en ', @ConexionCodigo,
                   CASE WHEN @VolvioBorrador = 1 THEN N' · cambiaron: vuelve a Borrador' ELSE N'' END), @Ip);
    COMMIT;
END;
