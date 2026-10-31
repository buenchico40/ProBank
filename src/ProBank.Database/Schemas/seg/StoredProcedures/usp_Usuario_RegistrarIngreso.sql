-- Resultado de cada intento de inicio de sesión. Éxito: limpia los intentos y guarda la conexión e IP (la anterior queda
-- para el perfil). Fallo: suma un intento y, al llegar al máximo, bloquea la cuenta (SEG-06; seg.ParametroAcceso).
-- Todo queda en auditoría.
CREATE PROCEDURE seg.usp_Usuario_RegistrarIngreso
    @Usuario VARCHAR(60), @Exitoso BIT, @Motivo NVARCHAR(200), @Ip VARCHAR(45)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    DECLARE @UsuarioId INT = (SELECT UsuarioId FROM seg.Usuario WHERE Usuario = @Usuario);
    DECLARE @Maximo INT = seg.fn_Parametro('IntentosFallidos', 5), @Minutos INT = seg.fn_Parametro('MinutosBloqueo', 15);
    BEGIN TRAN;
    IF @UsuarioId IS NOT NULL
    BEGIN
        IF @Exitoso = 1
            UPDATE seg.Usuario
            SET IntentosFallidos = 0, BloqueadoHasta = NULL,
                ConexionAnterior = UltimaConexion, IpAnterior = UltimaIp, UltimaConexion = SYSDATETIME(), UltimaIp = @Ip
            WHERE UsuarioId = @UsuarioId;
        ELSE
            UPDATE seg.Usuario
            SET IntentosFallidos = IntentosFallidos + 1,
                BloqueadoHasta = CASE WHEN IntentosFallidos + 1 >= @Maximo THEN DATEADD(MINUTE, @Minutos, SYSDATETIME()) ELSE BloqueadoHasta END
            WHERE UsuarioId = @UsuarioId;
    END;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (@UsuarioId, NULL, 'Seguridad',
            CASE WHEN @Exitoso = 1 THEN N'Inicio de sesión' ELSE N'Inicio de sesión fallido' END,
            CONCAT(@Usuario, CASE WHEN @Motivo IS NULL THEN N'' ELSE N' · ' + @Motivo END), @Ip);
    COMMIT;
END;
