-- Run this once against your database. Or add entities to your DbContext and use EF migration.

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='UserTokens' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[UserTokens] (
        [Id]                     BIGINT         IDENTITY(1,1) NOT NULL,
        [UserId]                 NVARCHAR(128)  NOT NULL,
        [Email]                  NVARCHAR(256)  NULL,
        [DisplayName]            NVARCHAR(256)  NULL,
        [ProductName]            NVARCHAR(50)   NOT NULL,
        [AccessToken]            NVARCHAR(MAX)  NOT NULL,
        [RefreshToken]           NVARCHAR(MAX)  NULL,
        [IdToken]                NVARCHAR(MAX)  NULL,
        [AccessTokenExpiresUtc]  DATETIME2      NOT NULL,
        [RefreshTokenExpiresUtc] DATETIME2      NULL,
        [GrantedScopes]          NVARCHAR(1024) NULL,
        [TenantId]               NVARCHAR(128)  NULL,
        [IsActive]               BIT            NOT NULL DEFAULT 1,
        [CreatedUtc]             DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedUtc]             DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
        [LastIpAddress]          NVARCHAR(45)   NULL,
        [LastUserAgent]          NVARCHAR(512)  NULL,
        [RefreshCount]           INT            NOT NULL DEFAULT 0,
        CONSTRAINT [PK_UserTokens] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    CREATE UNIQUE INDEX [IX_UserTokens_UserId_Product] ON [dbo].[UserTokens] ([UserId], [ProductName]);
    CREATE INDEX [IX_UserTokens_UserId] ON [dbo].[UserTokens] ([UserId]);
END
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='UserSessions' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[UserSessions] (
        [Id]                  BIGINT         IDENTITY(1,1) NOT NULL,
        [SessionId]           NVARCHAR(128)  NOT NULL,
        [UserId]              NVARCHAR(128)  NOT NULL,
        [Email]               NVARCHAR(256)  NULL,
        [InitiatedByProduct]  NVARCHAR(50)   NOT NULL,
        [ProductsAccessed]    NVARCHAR(512)  NULL,
        [StartedUtc]          DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
        [LastActivityUtc]     DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
        [EndedUtc]            DATETIME2      NULL,
        [IsActive]            BIT            NOT NULL DEFAULT 1,
        [IpAddress]           NVARCHAR(45)   NULL,
        [UserAgent]           NVARCHAR(512)  NULL,
        CONSTRAINT [PK_UserSessions] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    CREATE UNIQUE INDEX [IX_UserSessions_SessionId] ON [dbo].[UserSessions] ([SessionId]);
    CREATE INDEX [IX_UserSessions_UserId] ON [dbo].[UserSessions] ([UserId]);
END
GO
