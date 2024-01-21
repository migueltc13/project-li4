CREATE TABLE [dbo].[Product] (
    [ProductId]   INT             NOT NULL,
    [AuctionId]   INT             NOT NULL,
    [ClientId]    INT             DEFAULT ((0)) NOT NULL,
    [Name]        NVARCHAR (64)   NOT NULL,
    [Description] NVARCHAR (2048) NOT NULL,
    [Price]       MONEY           NOT NULL,
    [Images]      NVARCHAR (2048) NULL,
    PRIMARY KEY CLUSTERED ([ProductId] ASC)
);
