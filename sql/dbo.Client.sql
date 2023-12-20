CREATE TABLE [dbo].[Client] (
    [ClientId]      INT            NOT NULL,
    [FullName]      NVARCHAR (MAX) NOT NULL,
    [Username]      NVARCHAR (MAX) NOT NULL,
    [Email]         NVARCHAR (MAX) NOT NULL,
    [Password]      NVARCHAR (MAX) NOT NULL,
    [OptNewsletter] BIT            DEFAULT ((0)) NOT NULL,
    PRIMARY KEY CLUSTERED ([ClientId] ASC)
);

