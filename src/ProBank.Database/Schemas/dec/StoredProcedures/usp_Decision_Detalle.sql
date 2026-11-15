-- DEC-01, DEC-03, DEC-05, DEC-06 y DEC-08: contenido de una decisión con su huella guardada y la actual (Integra: el contenido no cambió
-- desde que se envió), sus aprobadores con la firma y si el sello sigue coincidiendo, sus notificados y su trazabilidad.
-- Solo para quien puede verla; cada consulta (o exportación, @Evento) queda en la trazabilidad antes de devolver los datos.
CREATE PROCEDURE dec.usp_Decision_Detalle
    @Token UNIQUEIDENTIFIER, @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45) = NULL, @Evento NVARCHAR(40) = N'Consulta'
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id INT, @Creador INT;
    SELECT @Id = DecisionId, @Creador = CreadoPorId FROM dec.Decision WHERE Token = @Token;
    IF @Id IS NULL THROW 50404, N'La decisión no existe.', 1;
    IF dec.fn_PuedeVer(@ActorId, @RolActivo, @Id) = 0
        THROW 50403, N'Solo ven esta decisión quien la registró, sus aprobadores, sus notificados y quien tiene el permiso de ver decisiones.', 1;
    IF @Evento NOT IN (N'Consulta', N'Exportación PDF') THROW 50422, N'Evento no válido.', 1;

    INSERT dec.DecisionEvento (DecisionId, UsuarioId, Evento, Detalle, Ip)
    VALUES (@Id, @ActorId, @Evento,
            CASE WHEN @Creador = @ActorId THEN N'Quien la registró'
                 WHEN EXISTS (SELECT 1 FROM dec.DecisionAprobador WHERE DecisionId = @Id AND UsuarioId = @ActorId) THEN N'Aprobador'
                 WHEN EXISTS (SELECT 1 FROM dec.DecisionNotificado WHERE DecisionId = @Id AND UsuarioId = @ActorId) THEN N'Notificado'
                 ELSE N'Con permiso de ver decisiones (' + @RolActivo + N')' END, @Ip);

    DECLARE @Actual VARBINARY(32) = dec.fn_Huella(@Id);
    SELECT d.Descripcion, d.MotivoAnulacion, CONVERT(VARCHAR(64), d.Huella, 2) AS Huella, CONVERT(VARCHAR(64), @Actual, 2) AS HuellaActual,
           CAST(CASE WHEN d.Huella IS NULL OR d.Huella = @Actual THEN 1 ELSE 0 END AS BIT) AS Integra,
           CAST(CASE WHEN EXISTS (SELECT 1 FROM dec.DecisionAprobador WHERE DecisionId = @Id AND Resultado <> N'Pendiente')
                       OR EXISTS (SELECT 1 FROM dec.FirmaAnterior WHERE DecisionId = @Id) THEN 1 ELSE 0 END AS BIT) AS TieneFirmas
    FROM dec.Decision d WHERE d.DecisionId = @Id;

    SELECT a.Orden, u.UsuarioId, u.Usuario, u.Nombre, a.Origen, a.Resultado, a.Comentario, a.Fecha, a.Ip, CONVERT(VARCHAR(64), a.Sello, 2) AS Sello,
           CAST(CASE WHEN a.Sello IS NULL THEN NULL
                     WHEN a.Sello = dec.fn_Sello(d.Huella, u.Usuario, a.Resultado, a.Fecha, a.Comentario, a.Ip) AND d.Huella = @Actual THEN 1 ELSE 0 END AS BIT) AS SelloValido
    FROM dec.DecisionAprobador a
    JOIN dec.Decision d ON d.DecisionId = a.DecisionId
    JOIN seg.Usuario u ON u.UsuarioId = a.UsuarioId
    WHERE a.DecisionId = @Id
    ORDER BY a.Orden, u.Nombre;

    SELECT u.UsuarioId, u.Usuario, u.Nombre, n.Fecha AS Desde, p.Nombre AS AgregadoPor,
           (SELECT MAX(e.Fecha) FROM dec.DecisionEvento e WHERE e.DecisionId = @Id AND e.UsuarioId = u.UsuarioId AND e.Evento = N'Consulta') AS UltimaConsulta
    FROM dec.DecisionNotificado n
    JOIN seg.Usuario u ON u.UsuarioId = n.UsuarioId
    JOIN seg.Usuario p ON p.UsuarioId = n.AgregadoPorId
    WHERE n.DecisionId = @Id
    ORDER BY u.Nombre;

    SELECT e.Fecha, u.Usuario, u.Nombre, e.Evento, e.Detalle, e.Ip
    FROM dec.DecisionEvento e JOIN seg.Usuario u ON u.UsuarioId = e.UsuarioId
    WHERE e.DecisionId = @Id
    ORDER BY e.EventoId DESC;

    -- DEC-07: versiones enviadas y firmas de las versiones anteriores (su sello se verifica contra la huella de su versión)
    SELECT v.Version, v.Titulo, v.Area, v.Fecha, CONVERT(VARCHAR(64), v.Huella, 2) AS Huella, v.Motivo, u.Nombre AS RegistradaPor,
           v.FechaRegistro, v.Descripcion
    FROM dec.DecisionVersion v JOIN seg.Usuario u ON u.UsuarioId = v.UsuarioId
    WHERE v.DecisionId = @Id
    ORDER BY v.Version DESC;

    SELECT f.Version, u.Usuario, u.Nombre, f.Resultado, f.Comentario, f.Fecha, f.Ip, CONVERT(VARCHAR(64), f.Sello, 2) AS Sello,
           CAST(CASE WHEN f.Sello = dec.fn_Sello(v.Huella, u.Usuario, f.Resultado, f.Fecha, f.Comentario, f.Ip) THEN 1 ELSE 0 END AS BIT) AS SelloValido
    FROM dec.FirmaAnterior f
    JOIN dec.DecisionVersion v ON v.DecisionId = f.DecisionId AND v.Version = f.Version
    JOIN seg.Usuario u ON u.UsuarioId = f.UsuarioId
    WHERE f.DecisionId = @Id
    ORDER BY f.Version DESC, f.Fecha;

    -- DEC-08: archivos de respaldo con la huella SHA-256 de cada uno (no forman parte de la huella de la decisión)
    SELECT Token, Nombre, TipoContenido, Tamano, CONVERT(VARCHAR(64), Hash, 2) AS Hash, Fecha FROM dec.DecisionAdjunto WHERE DecisionId = @Id ORDER BY AdjuntoId;
END;
