USE [AdventureWorks2022];
GO

IF OBJECT_ID(N'dbo.Trades', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Trades
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Trades PRIMARY KEY,
        TradeReference NVARCHAR(40) NOT NULL,
        Counterparty NVARCHAR(120) NOT NULL,
        Instrument NVARCHAR(40) NOT NULL,
        Currency NVARCHAR(3) NOT NULL,
        Side NVARCHAR(4) NOT NULL,
        Notional DECIMAL(18, 2) NOT NULL,
        Rate DECIMAL(18, 6) NOT NULL,
        TradeDate DATETIME2 NOT NULL,
        SettlementDate DATETIME2 NOT NULL,
        CreatedUtc DATETIME2 NOT NULL,
        CONSTRAINT UX_Trades_TradeReference UNIQUE (TradeReference),
        CONSTRAINT CK_Trades_Side CHECK (Side IN ('BUY', 'SELL'))
    );
END
GO
