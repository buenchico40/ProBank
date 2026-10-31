-- Lo usa ProBank.Setup: crea o resetea la única cuenta local (administrador) restringida a una IP.
CREATE PROCEDURE seg.usp_Usuario_CrearAdmin
    @Usuario VARCHAR(60), @Nombre NVARCHAR(150), @ClaveHash VARBINARY(64), @ClaveSal VARBINARY(32), @IpPermitida VARCHAR(45)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRAN;
    MERGE seg.Usuario AS t
    USING (SELECT @Usuario AS Usuario) AS s ON t.Usuario = s.Usuario
    WHEN MATCHED THEN UPDATE SET ClaveHash = @ClaveHash, ClaveSal = @ClaveSal, ClaveCambiada = SYSDATETIME(), DebeCambiarClave = 0,
        IpPermitida = @IpPermitida, EsCuentaLocal = 1, Estado = 'Activo', IntentosFallidos = 0, BloqueadoHasta = NULL, SelloSesion = t.SelloSesion + 1
    WHEN NOT MATCHED THEN INSERT (Usuario, Nombre, EsCuentaLocal, ClaveHash, ClaveSal, ClaveCambiada, IpPermitida, RolPredeterminado)
        VALUES (@Usuario, @Nombre, 1, @ClaveHash, @ClaveSal, SYSDATETIME(), @IpPermitida, 'Administrador');
    INSERT seg.UsuarioRol (UsuarioId, RolId)
    SELECT u.UsuarioId, r.RolId FROM seg.Usuario u CROSS JOIN seg.Rol r
    WHERE u.Usuario = @Usuario AND r.Nombre = 'Administrador'
      AND NOT EXISTS (SELECT 1 FROM seg.UsuarioRol x WHERE x.UsuarioId = u.UsuarioId AND x.RolId = r.RolId);
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    SELECT UsuarioId, 'Administrador', 'Seguridad', 'Administrador local creado o reseteado', @Usuario, @IpPermitida FROM seg.Usuario WHERE Usuario = @Usuario;
    COMMIT;
END;
