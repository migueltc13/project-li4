CREATE TABLE [dbo].[Bid] (
    [BidId]     INT      NOT NULL,
    [Value]     INT      NOT NULL,
    [Time]      DATETIME NOT NULL,
    [ClientId]  INT      NOT NULL,
    [AuctionId] INT      NOT NULL,
    PRIMARY KEY CLUSTERED ([BidId] ASC)
);