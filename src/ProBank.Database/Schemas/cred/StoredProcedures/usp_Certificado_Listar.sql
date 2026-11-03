-- CRE-01: certificados con sus días para vencer y su estado (o solo uno con @Token), del que vence antes al que vence después.
CREATE PROCEDURE cred.usp_Certificado_Listar @Hoy DATE, @Token UNIQUEIDENTIFIER = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT c.Token, c.Codigo, c.NombreComun, c.Tipo, c.Emisor, c.Vence, DATEDIFF(DAY, @Hoy, c.Vence) AS DiasParaVencer, c.Almacen, c.Responsable,
           c.DiasAviso, c.Huella, c.NombresAlternativos, c.UsadoPor, c.Activo, cred.fn_EstadoVence(c.Activo, c.Vence, c.DiasAviso, @Hoy) AS Estado,
           (SELECT MAX(r.Fecha) FROM cred.CertificadoRenovacion r WHERE r.CertificadoId = c.CertificadoId) AS UltimaRenovacion
    FROM cred.Certificado c
    WHERE @Token IS NULL OR c.Token = @Token
    ORDER BY c.Activo DESC, c.Vence, c.Codigo;
END;
