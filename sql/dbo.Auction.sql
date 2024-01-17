CREATE TABLE [dbo].[Auction] (
    [AuctionId]       INT           NOT NULL,
    [StartTime]       DATETIME      NOT NULL,
    [EndTime]         DATETIME      NOT NULL,
    [ClientId]        INT           NOT NULL,
    [ProductId]       INT           NOT NULL,
    [MinimumBid]      MONEY         NOT NULL,
	[IsCheckHasEnded] BIT           DEFAULT ((0)) NOT NULL,
    [IsCompleted]     BIT           DEFAULT ((0)) NOT NULL,
    [PaymentMethod]   NVARCHAR (32) NULL,
    PRIMARY KEY CLUSTERED ([AuctionId] ASC)
);