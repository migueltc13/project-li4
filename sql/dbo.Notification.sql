CREATE TABLE [dbo].[Notification] (
    [NotificationId] INT            NOT NULL,
    [ClientId]       INT            NOT NULL,
    [AuctionId]      INT            NOT NULL,
    [Message]        NVARCHAR (256) NOT NULL,
    [Timestamp]      DATETIME       NOT NULL,
    [IsRead]         BIT            DEFAULT ((0)) NOT NULL,
    PRIMARY KEY CLUSTERED ([NotificationId] ASC),
    CONSTRAINT FK_Notification_ClientId FOREIGN KEY ([ClientId]) REFERENCES [dbo].[Client]([ClientId]),
    CONSTRAINT FK_Notification_AuctionId FOREIGN KEY ([AuctionId]) REFERENCES [dbo].[Auction]([AuctionId])
);
