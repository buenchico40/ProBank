CREATE PROCEDURE pry.usp_Item_ObtenerId @Token UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ItemId FROM pry.Item WHERE Token = @Token AND Estado NOT IN ('Anulado','Completado');
END;
