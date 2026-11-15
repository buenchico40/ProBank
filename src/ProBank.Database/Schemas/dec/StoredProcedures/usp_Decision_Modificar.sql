-- DEC-07: un aprobador, cuando le toca firmar, corrige el contenido de la decisión en lugar de aprobarla o rechazarla (la API ya
-- verificó su clave). Nace una versión nueva con otra huella; las firmas de la versión anterior pasan a dec.FirmaAnterior y todos
-- los aprobadores vuelven a firmar (en secuencia, desde el primero). El tipo no cambia.
CREATE PROCEDURE dec.usp_Decision_Modificar
    @Token UNIQUEIDENTIFIER, @Titulo NVARCHAR(200), @Descripcion NVARCHAR(MAX), @Area NVARCHAR(100), @Fecha DATE,
    @Motivo NVARCHAR(500), @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45), @VersionNueva INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'pendientes', 'aprobar') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para firmar aprobaciones.', 1;
    IF NULLIF(LTRIM(RTRIM(@Motivo)), N'') IS NULL THROW 50422, N'Indique el motivo de la modificación.', 1;

    BEGIN TRAN;
    DECLARE @Id INT, @Codigo VARCHAR(12), @Estado NVARCHAR(20), @Secuencial BIT, @Huella VARBINARY(32), @Version INT, @Orden INT, @Resultado NVARCHAR(10);
    SELECT @Id = DecisionId, @Codigo = Codigo, @Estado = Estado, @Secuencial = Secuencial, @Huella = Huella, @Version = Version
    FROM dec.Decision WITH (UPDLOCK) WHERE Token = @Token;
    IF @Id IS NULL THROW 50404, N'La decisión no existe.', 1;
    SELECT @Orden = Orden, @Resultado = Resultado FROM dec.DecisionAprobador WITH (UPDLOCK) WHERE DecisionId = @Id AND UsuarioId = @ActorId;
    IF @Orden IS NULL THROW 50403, N'No es aprobador de esta decisión.', 1;
    IF @Estado <> N'En aprobación' THROW 50409, N'La decisión no está en aprobación.', 1;
    IF @Resultado <> N'Pendiente' THROW 50409, N'Ya firmó esta versión de la decisión.', 1;
    IF @Secuencial = 1 AND EXISTS (SELECT 1 FROM dec.DecisionAprobador WHERE DecisionId = @Id AND Resultado = N'Pendiente' AND Orden < @Orden)
        THROW 50409, N'Aún no es su turno: la decisión se aprueba en secuencia.', 1;
    IF @Huella IS NULL OR @Huella <> dec.fn_Huella(@Id)
        THROW 50409, N'El contenido de la decisión no coincide con su huella: no se puede modificar. Avise a Seguridad de la información.', 1;

    -- Las firmas de la versión vigente se conservan con su sello y todos vuelven a quedar pendientes
    INSERT dec.FirmaAnterior (DecisionId, Version, UsuarioId, Resultado, Comentario, Fecha, Ip, Sello)
    SELECT DecisionId, @Version, UsuarioId, Resultado, Comentario, Fecha, Ip, Sello
    FROM dec.DecisionAprobador WHERE DecisionId = @Id AND Resultado <> N'Pendiente';
    DECLARE @Anuladas INT = @@ROWCOUNT;
    UPDATE dec.DecisionAprobador SET Resultado = N'Pendiente', Comentario = NULL, Fecha = NULL, Ip = NULL, Sello = NULL WHERE DecisionId = @Id;

    SET @VersionNueva = @Version + 1;
    SET @Motivo = LTRIM(RTRIM(@Motivo));
    UPDATE dec.Decision SET Titulo = @Titulo, Descripcion = @Descripcion, Area = @Area, Fecha = @Fecha, Version = @VersionNueva
    WHERE DecisionId = @Id;
    UPDATE dec.Decision SET Huella = dec.fn_Huella(@Id) WHERE DecisionId = @Id;
    INSERT dec.DecisionVersion (DecisionId, Version, Titulo, Descripcion, Area, Fecha, Huella, Motivo, UsuarioId)
    SELECT DecisionId, @VersionNueva, Titulo, Descripcion, Area, Fecha, Huella, @Motivo, @ActorId FROM dec.Decision WHERE DecisionId = @Id;

    DECLARE @Detalle NVARCHAR(500) = LEFT(CONCAT(N'Versión ', @Version, N' → ', @VersionNueva, N' · ', @Motivo,
        N' · ', @Anuladas, N' firma(s) anteriores conservadas; todos vuelven a firmar · huella ',
        (SELECT CONVERT(VARCHAR(64), Huella, 2) FROM dec.Decision WHERE DecisionId = @Id)), 500);
    INSERT dec.DecisionEvento (DecisionId, UsuarioId, Evento, Detalle, Ip) VALUES (@Id, @ActorId, N'Modificación', @Detalle, @Ip);
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, N'Decisiones', N'Modificación de la decisión ' + @Codigo, @Detalle, @Ip);
    COMMIT;
END;
