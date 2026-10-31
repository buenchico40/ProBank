-- PRY-08: revisión semanal de un ítem con sus acuerdos. @Acuerdos: JSON [{descripcion, responsableId, fecha}].
-- PRY-09: «Registrar acuerdo semanal».
CREATE PROCEDURE pry.usp_Acuerdo_Registrar
    @ItemToken UNIQUEIDENTIFIER, @Fecha DATE, @Temas NVARCHAR(2000) = NULL, @Acuerdos NVARCHAR(MAX),
    @ActorId INT, @RolActivo VARCHAR(60), @Ip VARCHAR(45), @Registrados INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF seg.fn_TienePermiso(@ActorId, @RolActivo, 'acuerdos', 'crear') = 0
        THROW 50403, N'Tu rol activo no tiene permiso para registrar acuerdos.', 1;
    IF pry.fn_PuedeAccion(@RolActivo, 'registrar_acuerdo') = 0
        THROW 50403, N'Tu rol activo no está habilitado para «Registrar acuerdo semanal» (Portafolio › Parametrización).', 1;
    DECLARE @ItemId INT, @Codigo VARCHAR(20), @Estado VARCHAR(20);
    SELECT @ItemId = ItemId, @Codigo = Codigo, @Estado = Estado FROM pry.Item WHERE Token = @ItemToken;
    IF @ItemId IS NULL THROW 50404, N'El proyecto, mejora o bug no existe.', 1;
    IF @Estado IN ('Borrador', 'Anulado') THROW 50422, N'No se registran acuerdos en un borrador o un ítem anulado.', 1;
    DECLARE @a TABLE (Descripcion NVARCHAR(500), ResponsableId INT, Fecha DATE);
    INSERT @a SELECT Descripcion, ResponsableId, Fecha
    FROM OPENJSON(@Acuerdos) WITH (Descripcion NVARCHAR(500) '$.descripcion', ResponsableId INT '$.responsableId', Fecha DATE '$.fecha')
    WHERE NULLIF(LTRIM(Descripcion), N'') IS NOT NULL;
    IF NOT EXISTS (SELECT 1 FROM @a) THROW 50422, N'Agregue al menos un acuerdo.', 1;
    IF EXISTS (SELECT 1 FROM @a x WHERE x.Fecha IS NULL OR NOT EXISTS (SELECT 1 FROM seg.Usuario u WHERE u.UsuarioId = x.ResponsableId AND u.Estado = 'Activo'))
        THROW 50422, N'Cada acuerdo necesita un responsable activo y una fecha de cumplimiento.', 1;
    BEGIN TRAN;
    INSERT pry.Revision (ItemId, Fecha, Temas, UsuarioId) VALUES (@ItemId, @Fecha, NULLIF(@Temas, N''), @ActorId);
    DECLARE @RevisionId INT = SCOPE_IDENTITY();
    INSERT pry.Acuerdo (RevisionId, ItemId, Descripcion, ResponsableId, FechaCumplimiento) SELECT @RevisionId, @ItemId, Descripcion, ResponsableId, Fecha FROM @a;
    SET @Registrados = @@ROWCOUNT;
    INSERT aud.Auditoria (UsuarioId, RolActivo, Modulo, Accion, Detalle, Ip)
    VALUES (NULLIF(@ActorId, 0), @RolActivo, 'Portafolio', N'Revisión semanal de ' + @Codigo, CONCAT(@Registrados, N' acuerdo(s) · ', CONVERT(CHAR(10), @Fecha, 103)), @Ip);
    COMMIT;
END;
