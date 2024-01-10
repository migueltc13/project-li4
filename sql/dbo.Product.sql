CREATE TABLE [dbo].[Product] (
    [ProductId]   INT           NOT NULL,
    [Name]        VARCHAR (MAX) NOT NULL,
    [Description] VARCHAR (MAX) NOT NULL,
    [Price]       INT           NOT NULL,
    [AuctionId]   INT           NOT NULL,
    [ClientId]    INT           NOT NULL DEFAULT 0,
    PRIMARY KEY CLUSTERED ([ProductId] ASC)
);