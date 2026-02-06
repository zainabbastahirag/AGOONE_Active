-- ============================================================================
-- AG ONE SSO - Token Storage Database Migration
-- ============================================================================
-- Run this SQL script against your database to create the token storage tables.
-- 
-- Alternatively, if you add the entities to your existing DbContext,
-- just run: dotnet ef migrations add AddAgOneTokenStorage
-- ============================================================================

-- Table: UserTokens
-- Stores access tokens, refresh tokens, and metadata per user per product
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='UserTokens' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[UserTokens] (
        [Id]                    BIGINT          IDENTITY(1,1) NOT NULL,
        [UserId]                NVARCHAR(128)   NOT NULL,
        [Email]                 NVARCHAR(256)   NULL,
        [DisplayName]           NVARCHAR(256)   NULL,
        [ProductName]           NVARCHAR(50)    NOT NULL,
        [AccessToken]           NVARCHAR(MAX)   NOT NULL,
        [RefreshToken]          NVARCHAR(MAX)   NULL,
        [IdToken]               NVARCHAR(MAX)   NULL,
        [AccessTokenExpiresUtc] DATETIME2       NOT NULL,
        [RefreshTokenExpiresUtc] DATETIME2      NULL,
        [GrantedScopes]         NVARCHAR(1024)  NULL,
        [TenantId]              NVARCHAR(128)   NULL,
        [IsActive]              BIT             NOT NULL DEFAULT 1,
        [CreatedUtc]            DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedUtc]            DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
        [LastIpAddress]         NVARCHAR(45)    NULL,
        [LastUserAgent]         NVARCHAR(512)   NULL,
        [RefreshCount]          INT             NOT NULL DEFAULT 0,

        CONSTRAINT [PK_UserTokens] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    -- Unique: one token per user per product
    CREATE UNIQUE INDEX [IX_UserTokens_UserId_Product]
        ON [dbo].[UserTokens] ([UserId], [ProductName]);

    -- Fast lookup by user
    CREATE INDEX [IX_UserTokens_UserId]
        ON [dbo].[UserTokens] ([UserId]);

    -- Find expired tokens for cleanup
    CREATE INDEX [IX_UserTokens_Expiry]
        ON [dbo].[UserTokens] ([AccessTokenExpiresUtc]);

    -- Active tokens per user
    CREATE INDEX [IX_UserTokens_Active_UserId]
        ON [dbo].[UserTokens] ([IsActive], [UserId]);

    PRINT 'Created table: UserTokens';
END
ELSE
BEGIN
    PRINT 'Table UserTokens already exists. Skipping.';
END
GO

-- Table: UserSessions
-- Tracks SSO sessions across products
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='UserSessions' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[UserSessions] (
        [Id]                    BIGINT          IDENTITY(1,1) NOT NULL,
        [SessionId]             NVARCHAR(128)   NOT NULL,
        [UserId]                NVARCHAR(128)   NOT NULL,
        [Email]                 NVARCHAR(256)   NULL,
        [InitiatedByProduct]    NVARCHAR(50)    NOT NULL,
        [ProductsAccessed]      NVARCHAR(512)   NULL,
        [StartedUtc]            DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
        [LastActivityUtc]       DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
        [EndedUtc]              DATETIME2       NULL,
        [IsActive]              BIT             NOT NULL DEFAULT 1,
        [IpAddress]             NVARCHAR(45)    NULL,
        [UserAgent]             NVARCHAR(512)   NULL,

        CONSTRAINT [PK_UserSessions] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    CREATE UNIQUE INDEX [IX_UserSessions_SessionId]
        ON [dbo].[UserSessions] ([SessionId]);

    CREATE INDEX [IX_UserSessions_UserId]
        ON [dbo].[UserSessions] ([UserId]);

    CREATE INDEX [IX_UserSessions_Active_UserId]
        ON [dbo].[UserSessions] ([IsActive], [UserId]);

    CREATE INDEX [IX_UserSessions_LastActivity]
        ON [dbo].[UserSessions] ([LastActivityUtc]);

    PRINT 'Created table: UserSessions';
END
ELSE
BEGIN
    PRINT 'Table UserSessions already exists. Skipping.';
END
GO
