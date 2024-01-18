CREATE TABLE [dbo].[Bid] (
    [BidId]     INT      NOT NULL,
    [AuctionId] INT      NOT NULL,
    [ClientId]  INT      NOT NULL,
    [Value]     MONEY    NOT NULL,
    [Time]      DATETIME NOT NULL,
    PRIMARY KEY CLUSTERED ([BidId] ASC)
);
