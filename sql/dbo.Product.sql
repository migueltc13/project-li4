CREATE TABLE [dbo].[Product]
(
	[ProductId] INT NOT NULL PRIMARY KEY,
	[Name] VARCHAR(MAX) NOT NULL,
	[Description] VARCHAR(MAX) NOT NULL,
	[Price] INT NOT NULL,
	[AuctionId] INT NOT NULL,
	[UserId] INT NOT NULL
)
