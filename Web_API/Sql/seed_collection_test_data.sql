-- Idempotent sample data for the collection pagination/filter/sort requests.
-- Run against the local KpzDb database.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @OwnerEmail nvarchar(254) = N'anastasiia.alieksieieva.pz.2024@lpnu.ua';
DECLARE @ReaderEmail nvarchar(254) = N'na8814733@gmail.com';
DECLARE @OwnerId int = (SELECT UserId FROM dbo.Users WHERE Email = @OwnerEmail);
DECLARE @ReaderId int = (SELECT UserId FROM dbo.Users WHERE Email = @ReaderEmail);
DECLARE @Lviv nvarchar(200) = NCHAR(1051) + NCHAR(1100) + NCHAR(1074) + NCHAR(1110) + NCHAR(1074);
DECLARE @AirportTitle nvarchar(150) =
    NCHAR(1058) + NCHAR(1077) + NCHAR(1089) + NCHAR(1090) + NCHAR(1086) + NCHAR(1074) + NCHAR(1080) + NCHAR(1081) + N' ' +
    NCHAR(1072) + NCHAR(1077) + NCHAR(1088) + NCHAR(1086) + NCHAR(1087) + NCHAR(1086) + NCHAR(1088) + NCHAR(1090) + N' ' +
    NCHAR(1084) + NCHAR(1072) + NCHAR(1088) + NCHAR(1096) + NCHAR(1088) + NCHAR(1091) + NCHAR(1090);
DECLARE @FlightTypeId int;

IF @OwnerId IS NULL OR @ReaderId IS NULL
    THROW 50010, 'Expected test users were not found in dbo.Users.', 1;

IF NOT EXISTS (SELECT 1 FROM dbo.Trips WHERE TripId = 1)
    THROW 50011, 'TripId 1 was not found.', 1;

-- Trip 1 is the target for the itinerary and member collection examples.
UPDATE dbo.Trips SET OwnerId = @OwnerId WHERE TripId = 1;

-- Add two more Lviv trips so the pageSize=5 example has a second page.
IF NOT EXISTS (SELECT 1 FROM dbo.Trips WHERE TripName = N'Collection Pagination Lviv A' AND OwnerId = @OwnerId)
    INSERT dbo.Trips (OwnerId, TripCategoryId, TripName, Destination, StartDate, EndDate, Description)
    VALUES (@OwnerId, 1, N'Collection Pagination Lviv A', @Lviv, '2026-12-20', '2026-12-24', N'Test data for collection pagination.');

IF NOT EXISTS (SELECT 1 FROM dbo.Trips WHERE TripName = N'Collection Pagination Lviv B' AND OwnerId = @OwnerId)
    INSERT dbo.Trips (OwnerId, TripCategoryId, TripName, Destination, StartDate, EndDate, Description)
    VALUES (@OwnerId, 1, N'Collection Pagination Lviv B', @Lviv, '2027-01-10', '2027-01-14', N'Test data for collection pagination.');

IF NOT EXISTS (SELECT 1 FROM dbo.ItineraryItemTypes WHERE Name = N'Flight')
    INSERT dbo.ItineraryItemTypes (Name) VALUES (N'Flight');

SELECT @FlightTypeId = ItineraryItemTypeId
FROM dbo.ItineraryItemTypes
WHERE Name = N'Flight';

-- Six matching records make page 1 of size 5 report a next page.
;WITH NumberedStops AS
(
    SELECT n FROM (VALUES (1), (2), (3), (4), (5), (6)) AS numbers(n)
)
INSERT dbo.ItineraryItems
    (TripId, ItineraryItemTypeId, Title, Description, StartDateTime, EndDateTime, Address, Latitude, Longitude)
SELECT
    1,
    @FlightTypeId,
    CONCAT(@AirportTitle, N' ', n),
    N'Test stop for itinerary filtering and pagination.',
    DATEADD(hour, (n - 1) * 2, CAST('2026-10-04T09:00:00' AS datetime2)),
    DATEADD(hour, (n - 1) * 2 + 1, CAST('2026-10-04T09:00:00' AS datetime2)),
    N'Lviv airport',
    49.8397,
    24.0297
FROM NumberedStops
WHERE NOT EXISTS
(
    SELECT 1 FROM dbo.ItineraryItems i
    WHERE i.TripId = 1 AND i.ItineraryItemTypeId = @FlightTypeId AND i.Title = CONCAT(@AirportTitle, N' ', n)
);

-- Make Anastasiia the owner (not a member of her own trip); demote the former
-- demo owner by removing only that stale membership from the transferred trip.
DELETE dbo.TripMembers WHERE TripId = 1 AND UserId IN (@OwnerId, 1) AND UserId <> @ReaderId;

-- Keep the reader account as TripReader on every trip in this test set.
DECLARE @TestTrips TABLE (TripId int PRIMARY KEY);
INSERT @TestTrips (TripId) VALUES (1);
INSERT @TestTrips (TripId)
SELECT TripId FROM dbo.Trips
WHERE OwnerId = @OwnerId
  AND (Destination = @Lviv OR TripName IN (N'Collection Pagination Lviv A', N'Collection Pagination Lviv B'));

UPDATE tm SET Role = N'TripReader'
FROM dbo.TripMembers tm
JOIN @TestTrips t ON t.TripId = tm.TripId
WHERE tm.UserId = @ReaderId;

INSERT dbo.TripMembers (TripId, UserId, Role)
SELECT t.TripId, @ReaderId, N'TripReader'
FROM @TestTrips t
WHERE NOT EXISTS
(
    SELECT 1 FROM dbo.TripMembers tm WHERE tm.TripId = t.TripId AND tm.UserId = @ReaderId
);

COMMIT TRANSACTION;

-- Show the records to verify after execution.
SELECT t.TripId, t.TripName, t.Destination, t.OwnerId, owner.Email AS OwnerEmail,
       reader.Email AS ReaderEmail, tm.Role AS ReaderRole
FROM @TestTrips testTrip
JOIN dbo.Trips t ON t.TripId = testTrip.TripId
JOIN dbo.Users owner ON owner.UserId = t.OwnerId
JOIN dbo.TripMembers tm ON tm.TripId = t.TripId AND tm.UserId = @ReaderId
JOIN dbo.Users reader ON reader.UserId = tm.UserId
ORDER BY t.TripId;

SELECT COUNT(*) AS MatchingAirportFlights
FROM dbo.ItineraryItems i
JOIN dbo.ItineraryItemTypes type ON type.ItineraryItemTypeId = i.ItineraryItemTypeId
WHERE i.TripId = 1 AND type.Name = N'Flight' AND i.Title LIKE N'%' + @AirportTitle + N'%';
