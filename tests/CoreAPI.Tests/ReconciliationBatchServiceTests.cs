using CoreAPI.Data;
using CoreAPI.Models;
using CoreAPI.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CoreAPI.Tests;

public class ReconciliationBatchServiceTests
{
    [Fact]
    public async Task RunBatchAsync_ReturnsMatchedGroup_WhenLedgerMatchesTrades()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Trades.Add(CreateTrade("TRD-001", "USD", "BUY", 1000m));
        dbContext.Trades.Add(CreateTrade("TRD-002", "USD", "SELL", 250m));
        await dbContext.SaveChangesAsync();

        var service = new ReconciliationBatchService(dbContext);
        var request = new ReconciliationBatchRequest
        {
            BatchSize = 1,
            MaxDegreeOfParallelism = 2,
            LedgerEntries =
            {
                CreateLedgerEntry("LED-001", "USD", "BUY", 1000m),
                CreateLedgerEntry("LED-002", "USD", "SELL", 250m)
            }
        };

        var response = await service.RunBatchAsync(request);

        Assert.Equal(2, response.TradeCount);
        Assert.Equal(2, response.LedgerEntryCount);
        Assert.Equal(1, response.GroupCount);
        Assert.Equal(1, response.MatchedGroupCount);
        Assert.Equal(0, response.BreakGroupCount);
        Assert.Equal(2, response.MaxDegreeOfParallelism);

        var result = Assert.Single(response.Results);
        Assert.True(result.IsMatched);
        Assert.Equal(0m, result.BuyBreakAmount);
        Assert.Equal(0m, result.SellBreakAmount);
    }

    [Fact]
    public async Task RunBatchAsync_ReturnsBreakGroup_WhenLedgerDiffersFromTrades()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Trades.Add(CreateTrade("TRD-003", "EUR", "BUY", 500m));
        await dbContext.SaveChangesAsync();

        var service = new ReconciliationBatchService(dbContext);
        var request = new ReconciliationBatchRequest
        {
            Tolerance = 0.01m,
            LedgerEntries =
            {
                CreateLedgerEntry("LED-003", "EUR", "BUY", 475m)
            }
        };

        var response = await service.RunBatchAsync(request);

        Assert.Equal(1, response.GroupCount);
        Assert.Equal(0, response.MatchedGroupCount);
        Assert.Equal(1, response.BreakGroupCount);

        var result = Assert.Single(response.Results);
        Assert.False(result.IsMatched);
        Assert.Equal(-25m, result.BuyBreakAmount);
    }

    private static TmsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TmsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TmsDbContext(options);
    }

    private static Trade CreateTrade(string reference, string currency, string side, decimal amount)
    {
        var tradeDate = new DateTime(2026, 06, 24);

        return new Trade
        {
            TradeReference = reference,
            TradeType = "FX_SPOT",
            Counterparty = "ABC Bank",
            Currency1 = currency,
            Amount1 = amount,
            Currency2 = "INR",
            Amount2 = amount * 83m,
            FxRateUsed = 83m,
            RateDate = tradeDate,
            Side = side,
            TradeDate = tradeDate,
            SettlementDate = tradeDate.AddDays(2),
            Fees = 0m,
            CreatedBy = "test",
            CreatedUtc = DateTime.UtcNow
        };
    }

    private static ReconciliationLedgerEntry CreateLedgerEntry(
        string reference,
        string currency,
        string side,
        decimal amount)
    {
        return new ReconciliationLedgerEntry
        {
            ExternalReference = reference,
            Currency = currency,
            Side = side,
            Amount = amount,
            SettlementDate = new DateTime(2026, 06, 26)
        };
    }
}
