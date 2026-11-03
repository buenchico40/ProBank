-- SQL-04: configura los parámetros ya leídos del procedimiento ([{"nombre","etiqueta","tipo","obligatorio","valorDefecto","opciones"}]):
-- etiqueta, control (Texto, Número, Fecha o Lista con sus opciones), obligatorio y valor por defecto fijo o variable.
CREATE PROCEDURE rpt.usp_Consulta_GuardarParametros
    @Codigo VARCHAR(20), @ParametrosJson NVARCHAR(MAX), @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'consultas', 'editar') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para editar consultas.', 1;
    DECLARE @Id INT = (SELECT ConsultaId FROM rpt.Consulta WHERE Codigo = @Codigo);
    IF @Id IS NULL THROW 50404, N'La consulta no existe.', 1;
    BEGIN TRAN;
    UPDATE p SET Etiqueta = j.etiqueta, Tipo = j.tipo, Obligatorio = j.obligatorio, ValorDefecto = NULLIF(j.valorDefecto, N''),
           Opciones = CASE WHEN j.tipo = 'Lista' THEN NULLIF(j.opciones, N'') END
    FROM rpt.Parametro p
    JOIN OPENJSON(@ParametrosJson) WITH (nombre SYSNAME, etiqueta NVARCHAR(100), tipo VARCHAR(10), obligatorio BIT, valorDefecto NVARCHAR(200), opciones NVARCHAR(1000)) j
      ON j.nombre = p.Nombre
    WHERE p.ConsultaId = @Id;
    IF EXISTS (SELECT 1 FROM rpt.Parametro WHERE ConsultaId = @Id AND Tipo = 'Lista' AND Opciones IS NULL)
        THROW 50422, N'Un parámetro de tipo lista necesita sus opciones.', 1;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Consultas dinámicas', N'Configuración de parámetros de ' + @Codigo,
            (SELECT STRING_AGG(CONCAT(Etiqueta, N'=', ISNULL(ValorDefecto, N'(sin valor)')), N'; ') WITHIN GROUP (ORDER BY Orden)
             FROM rpt.Parametro WHERE ConsultaId = @Id), @Ip);
    COMMIT;
END;
