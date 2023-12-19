﻿CREATE TABLE [dbo].[Users] (
    [UserId]            INT            NOT NULL,
    [FullName]      NVARCHAR (MAX) NOT NULL,
    [Username]      NVARCHAR (MAX) NOT NULL,
    [Email]         NVARCHAR (MAX) NOT NULL,
    [Password]      NVARCHAR (MAX) NOT NULL,
    [OptNewsletter] BIT            DEFAULT ((0)) NOT NULL,
    PRIMARY KEY CLUSTERED ([UserId] ASC)
);

