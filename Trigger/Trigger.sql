CREATE TRIGGER UserInsertTrigger
ON Users
AFTER INSERT
AS
BEGIN
    INSERT INTO UsersAudit (Id, Username, IsAdmin, InstertedOn)
    SELECT NEWID(), i.Username, i.IsAdmin, GETUTCDATE()
    FROM inserted i;
END;



INSERT INTO Users (Username, Password, IsAdmin)
VALUES ('user3', '966154FCCAF76A277B4F69B73BC89C96', 0);

