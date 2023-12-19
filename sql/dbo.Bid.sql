CREATE TABLE [dbo].[Bid]
(
	[Id] INT NOT NULL PRIMARY KEY,
	[Value] INT NOT NULL,
	[Status] VARCHAR(MAX) NOT NULL,
	[Time] TIME NOT NULL,
	[UserId] INT NOT NULL,
	[AuctionID] INT NOT NULL
)
