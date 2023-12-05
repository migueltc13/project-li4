CREATE TABLE [dbo].[Users] (
    [Id]       INT            NOT NULL,
	[FullName] NVARCHAR (MAX) NOT NULL,
    [Username] NVARCHAR (MAX) NOT NULL,
    [Email]    NVARCHAR (MAX) NOT NULL,
    [Password] NVARCHAR (MAX) NOT NULL,
    [OptNewsletter] BIT NOT NULL DEFAULT 0, 
    PRIMARY KEY CLUSTERED ([Id] ASC)
);
