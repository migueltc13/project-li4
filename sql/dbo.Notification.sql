CREATE TABLE [dbo].[Notification]
(
	[NotificationId] INT NOT NULL PRIMARY KEY,
	[Message] NVARCHAR(256) NOT NULL,
	[Timestamp] DATETIME NOT NULL,
	[ClientId] INT NOT NULL,
	[AuctionId] INT NOT NULL,
	[IsRead] BIT NOT NULL DEFAULT 0
);