CREATE TABLE [dbo].[Client] (
    [ClientId]      INT            NOT NULL,
    [FullName]      NVARCHAR (64)  NOT NULL,
    [Username]      NVARCHAR (32)  NOT NULL,
    [Email]         NVARCHAR (320) NOT NULL,
    [Password]      NVARCHAR (64)  NOT NULL,
    [ProfilePic]    NVARCHAR (256) NULL,
    [OptNewsletter] BIT            DEFAULT ((0)) NOT NULL,
    PRIMARY KEY CLUSTERED ([ClientId] ASC),
    UNIQUE NONCLUSTERED ([Username] ASC, [Email] ASC)
);
