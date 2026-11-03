-- CRE-01: renovaciones registradas de un certificado, de la más reciente a la más antigua.
CREATE PROCEDURE cred.usp_Certificado_Renovaciones @Token UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT r.Fecha, r.VenceAnterior, r.VenceNuevo, r.HuellaAnterior, r.HuellaNueva, u.Nombre AS RegistradoPor
    FROM cred.CertificadoRenovacion r
    JOIN cred.Certificado c ON c.CertificadoId = r.CertificadoId
    JOIN seg.Usuario u ON u.UsuarioId = r.UsuarioId
    WHERE c.Token = @Token
    ORDER BY r.RenovacionId DESC;
END;
