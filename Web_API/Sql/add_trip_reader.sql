-- Run this in the KpzDb database. Change @TripId if the reader should join another trip.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @ReaderEmail nvarchar(254) = N'na8814733@gmail.com';
DECLARE @ReaderName nvarchar(150) =
    NCHAR(1040) + NCHAR(1085) + NCHAR(1072) + NCHAR(1089) + NCHAR(1090) + NCHAR(1072) +
    NCHAR(1089) + NCHAR(1110) + NCHAR(1103) + N' ' +
    NCHAR(1040) + NCHAR(1083) + NCHAR(1077) + NCHAR(1082) + NCHAR(1089) +
    NCHAR(1108) + NCHAR(1108) + NCHAR(1074) + NCHAR(1072);
DECLARE @TripId int = 1;
DECLARE @ReaderUserId int;

IF NOT EXISTS (SELECT 1 FROM dbo.Trips WHERE TripId = @TripId)
    THROW 50001, 'TripId does not exist. Change @TripId to an existing trip.', 1;

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = @ReaderEmail)
BEGIN
    -- PasswordHash is not used for Auth0 authentication; this account signs in through Auth0.
    INSERT INTO dbo.Users (FullName, Email, PasswordHash)
    VALUES (@ReaderName, @ReaderEmail, N'auth0-managed');
END;
ELSE
BEGIN
    UPDATE dbo.Users
    SET FullName = @ReaderName
    WHERE Email = @ReaderEmail;
END;

SELECT @ReaderUserId = UserId
FROM dbo.Users
WHERE Email = @ReaderEmail;

IF EXISTS (
    SELECT 1 FROM dbo.TripMembers
    WHERE TripId = @TripId AND UserId = @ReaderUserId
)
BEGIN
    UPDATE dbo.TripMembers
    SET Role = N'TripReader'
    WHERE TripId = @TripId AND UserId = @ReaderUserId;
END
ELSE
BEGIN
    INSERT INTO dbo.TripMembers (TripId, UserId, Role)
    VALUES (@TripId, @ReaderUserId, N'TripReader');
END;

COMMIT TRANSACTION;

SELECT u.UserId, u.FullName, u.Email, tm.TripId, tm.Role, tm.JoinedAt
FROM dbo.Users AS u
JOIN dbo.TripMembers AS tm ON tm.UserId = u.UserId
WHERE u.Email = @ReaderEmail AND tm.TripId = @TripId;
