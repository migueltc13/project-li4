CREATE TABLE [dbo].[Notification] (
    [NotificationId] INT            NOT NULL,
    [ClientId]       INT            NOT NULL,
    [AuctionId]      INT            NOT NULL,
    [Message]        NVARCHAR (256) NOT NULL,
    [Timestamp]      DATETIME       NOT NULL,
    [IsRead]         BIT            DEFAULT ((0)) NOT NULL,
    PRIMARY KEY CLUSTERED ([NotificationId] ASC)
);
