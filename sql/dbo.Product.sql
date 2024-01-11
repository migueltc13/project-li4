CREATE TABLE [dbo].[Product] (
    [ProductId]   INT           NOT NULL,
    [Name]        VARCHAR (MAX) NOT NULL,
    [Description] VARCHAR (MAX) NOT NULL,
    [Price]       INT           NOT NULL,
    [AuctionId]   INT           NOT NULL,
    [ClientId]    INT           DEFAULT ((0)) NOT NULL,
	[Images]	  VARCHAR (MAX) NULL,
    PRIMARY KEY CLUSTERED ([ProductId] ASC)
);