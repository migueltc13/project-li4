CREATE TABLE [dbo].[Auction] (
    [AuctionId]   INT      NOT NULL,
    [StartTime]   DATETIME NOT NULL,
    [EndTime]     DATETIME NOT NULL,
    [ClientId]    INT      NOT NULL,
    [ProductId]   INT      NOT NULL,
    [MinimumBid]  INT      NOT NULL,
    [IsCompleted] BIT      DEFAULT ((0)) NOT NULL,
    PRIMARY KEY CLUSTERED ([AuctionId] ASC)
);
