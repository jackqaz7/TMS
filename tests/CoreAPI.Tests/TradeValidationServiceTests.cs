using CoreAPI.Models;
using CoreAPI.Services;
using Xunit;

namespace CoreAPI.Tests;

public class TradeValidationServiceTests
{
    private readonly TradeValidationService _service = new();

    [Fact]
    public void ValidateCreateTrade_ReturnsNoErrors_ForValidSpotTrade()
    {
        var request = CreateValidSpotTrade();

        var errors = _service.ValidateCreateTrade(request);

        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateCreateTrade_ReturnsAmountError_WhenAmount1IsZero()
    {
        var request = CreateValidSpotTrade();
        request.Amount1 = 0;

        var errors = _service.ValidateCreateTrade(request);

        Assert.Contains("Amount 1 must be greater than zero.", errors);
    }

    [Fact]
    public void ValidateCreateTrade_ReturnsCurrencyError_WhenCurrenciesAreSame()
    {
        var request = CreateValidSpotTrade();
        request.Currency2 = "USD";

        var errors = _service.ValidateCreateTrade(request);

        Assert.Contains("Currency 1 and Currency 2 cannot be same.", errors);
    }

    [Fact]
    public void ValidateCreateTrade_ReturnsNearLegErrors_WhenForwardLegIsMissing()
    {
        var request = CreateValidSpotTrade();
        request.TradeType = "FX_FORWARD";

        var errors = _service.ValidateCreateTrade(request);

        // FX_FORWARD needs near-leg details because it settles on a future leg.
        Assert.Contains("Near leg date is required.", errors);
        Assert.Contains("Near leg rate must be greater than zero.", errors);
    }

    [Fact]
    public void ValidateCreateTrade_ReturnsFarLegDateError_WhenSwapFarLegIsNotAfterNearLeg()
    {
        var request = CreateValidSpotTrade();
        request.TradeType = "FX_SWAP";
        request.NearLegDate = request.TradeDate.AddDays(7);
        request.NearLegRate = 83.25m;
        request.NearLegAmount1 = 1000m;
        request.NearLegAmount2 = 83250m;
        request.FarLegDate = request.NearLegDate;
        request.FarLegRate = 83.40m;
        request.FarLegAmount1 = 1000m;
        request.FarLegAmount2 = 83400m;

        var errors = _service.ValidateCreateTrade(request);

        Assert.Contains("Far leg date must be after near leg date.", errors);
    }

    private static CreateTradeRequest CreateValidSpotTrade()
    {
        var tradeDate = new DateTime(2026, 06, 24);

        return new CreateTradeRequest
        {
            TradeReference = "TRD-001",
            TradeType = "FX_SPOT",
            Counterparty = "ABC Bank",
            Currency1 = "USD",
            Amount1 = 1000m,
            Currency2 = "INR",
            Amount2 = 83000m,
            FxRateUsed = 83m,
            RateDate = tradeDate,
            Side = "BUY",
            TradeDate = tradeDate,
            SettlementDate = tradeDate.AddDays(2),
            Fees = 0m
        };
    }
}
