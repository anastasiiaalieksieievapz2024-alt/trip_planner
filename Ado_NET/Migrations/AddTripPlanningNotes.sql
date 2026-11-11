BEGIN TRANSACTION;
ALTER TABLE [Trips] ADD [PlanningNotes] nvarchar(1000) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261111211043_AddTripPlanningNotes', N'10.0.15');

COMMIT;
GO

