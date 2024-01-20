CREATE TABLE [dbo].[Bid] (
    [BidId]     INT      NOT NULL,
    [AuctionId] INT      NOT NULL,
    [ClientId]  INT      NOT NULL,
    [Value]     MONEY    NOT NULL,
    [Time]      DATETIME NOT NULL,
    PRIMARY KEY CLUSTERED ([BidId] ASC),
    CONSTRAINT FK_Bid_AuctionId FOREIGN KEY ([AuctionId]) REFERENCES [dbo].[Auction]([AuctionId]),
    CONSTRAINT FK_Bid_ClientId FOREIGN KEY ([ClientId]) REFERENCES [dbo].[Client]([ClientId])
);
