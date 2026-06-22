using System;
using System.Security.Claims;
using CoreAPI.Data;
using CoreAPI.Filters;
using CoreAPI.Models;
using CoreAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;

namespace CoreAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TreasuryController : ControllerBase
    {
        private readonly TmsDbContext _tmsDbContext;
        private readonly ITradeValidationService _tradeValidationService;

        public TreasuryController(
            TmsDbContext tmsDbContext,
            ITradeValidationService tradeValidationService)
        {
            _tmsDbContext = tmsDbContext;
            _tradeValidationService = tradeValidationService;
        }

        [Route("Status")]
        [MethodFilter("GET")]
        public IActionResult GetStatus()
        {
            return Ok(new { Service = "Treasury", Status = "Running", Timestamp = DateTime.UtcNow });
        }

        [Route("Process")]
        [MethodFilter("POST")]
        public IActionResult ProcessTrade([FromBody] TransactionDto transaction)
        {
            return Ok(new { service = "Treasury", transaction.Id });
        }


        [HttpGet("fx-rates/latest")]
        public async Task<ActionResult<FxRateResponse>> GetLatestFxRate(
            [FromQuery] string fromCurrency,
            [FromQuery] string toCurrency)
        {
            var from = fromCurrency?.Trim().ToUpperInvariant() ?? string.Empty;
            var to = toCurrency?.Trim().ToUpperInvariant() ?? string.Empty;

            if (!IsValidCurrencyCode(from) || !IsValidCurrencyCode(to))
            {
                return BadRequest(new { Message = "Both currencies must be valid 3-letter currency codes." });
            }

            if (from == to)
            {
                return Ok(new FxRateResponse
                {
                    FromCurrency = from,
                    ToCurrency = to,
                    Rate = 1,
                    RateDate = DateTime.UtcNow.Date,
                    Source = "Same currency",
                    IsCrossRate = false
                });
            }

            // First try an exact pair from the database, for example USD -> INR.
            var directRate = await FindLatestStoredFxRate(from, to);

            if (directRate != null)
            {
                return Ok(ToFxRateResponse(directRate, from, to, directRate.Rate, false));
            }

            // Then try the inverse pair. If DB has USD -> EUR, we can serve EUR -> USD
            // as 1 / USD->EUR without storing a duplicate row.
            var inverseRate = await FindLatestStoredFxRate(to, from);

            if (inverseRate != null)
            {
                return Ok(ToFxRateResponse(inverseRate, from, to, 1 / inverseRate.Rate, true));
            }

            // Finally calculate a USD cross rate. With USD->EUR and USD->GBP stored:
            // EUR->GBP = (USD->GBP) / (USD->EUR).
            var usdToFrom = await FindLatestStoredFxRate("USD", from);
            var usdToTo = await FindLatestStoredFxRate("USD", to);

            if (usdToFrom == null || usdToTo == null)
            {
                return NotFound(new
                {
                    Message = $"No FX rate found for {from}->{to}, and USD cross-rate data is incomplete."
                });
            }

            return Ok(new FxRateResponse
            {
                FromCurrency = from,
                ToCurrency = to,
                Rate = usdToTo.Rate / usdToFrom.Rate,
                RateDate = usdToFrom.RateDate <= usdToTo.RateDate ? usdToFrom.RateDate : usdToTo.RateDate,
                Source = $"USD cross rate from {usdToFrom.Source} / {usdToTo.Source}",
                IsCrossRate = true
            });
        }
        [HttpPost("trades")]
        public async Task<ActionResult<TradeResponse>> CreateTrade([FromBody] CreateTradeRequest request)
        {
            var validationErrors = _tradeValidationService.ValidateCreateTrade(request);

            if (validationErrors.Count > 0)
            {
                return BadRequest(new
                {
                    Message = "Trade validation failed.",
                    Errors = validationErrors
                });
            }

            // CreatedBy is taken from the authenticated JWT so the client cannot fake audit data.
            var createdBy = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? User.Identity?.Name
                ?? "system";

            var trade = new Trade
            {
                TradeReference = request.TradeReference.Trim(),
                TradeType = request.TradeType.Trim().ToUpperInvariant(),
                Counterparty = request.Counterparty.Trim(),
                CounterpartyBankAccount = request.CounterpartyBankAccount?.Trim(),
                Currency1 = request.Currency1.Trim().ToUpperInvariant(),
                Amount1 = request.Amount1,
                Currency2 = request.Currency2.Trim().ToUpperInvariant(),
                Amount2 = request.Amount2,
                FxRateUsed = request.FxRateUsed,
                RateDate = request.RateDate,
                Side = request.Side.Trim().ToUpperInvariant(),
                TradeDate = request.TradeDate,
                SettlementDate = request.SettlementDate,
                Fees = request.Fees,
                Comments = request.Comments?.Trim(),
                NearLegDate = request.NearLegDate,
                NearLegRate = request.NearLegRate,
                NearLegAmount1 = request.NearLegAmount1,
                NearLegAmount2 = request.NearLegAmount2,
                FarLegDate = request.FarLegDate,
                FarLegRate = request.FarLegRate,
                FarLegAmount1 = request.FarLegAmount1,
                FarLegAmount2 = request.FarLegAmount2,
                SwapPoints = request.SwapPoints,
                CreatedBy = createdBy,
                CreatedUtc = DateTime.UtcNow
            };

            _tmsDbContext.Trades.Add(trade);
            await _tmsDbContext.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTrade), new { id = trade.Id }, ToResponse(trade));
        }

        [HttpGet("trades")]
        public async Task<ActionResult<IEnumerable<TradeResponse>>> GetTrades()
        {
            var trades = await _tmsDbContext.Trades
                .OrderByDescending(t => t.TradeDate)
                .ThenByDescending(t => t.Id)
                .Select(t => ToResponse(t))
                .ToListAsync();

            return Ok(trades);
        }

        [HttpGet("trades/{id:int}")]
        public async Task<ActionResult<TradeResponse>> GetTrade(int id)
        {
            var trade = await _tmsDbContext.Trades.FindAsync(id);

            if (trade == null)
            {
                return NotFound();
            }

            return Ok(ToResponse(trade));
        }

        [HttpGet("positions")]
        public async Task<ActionResult<IEnumerable<PositionSummaryDto>>> GetPositions()
        {
            var positions = await _tmsDbContext.Trades
                .GroupBy(t => t.Currency1)
                .Select(group => new PositionSummaryDto
                {
                    Currency = group.Key,
                    BuyNotional = group.Where(t => t.Side == "BUY").Sum(t => t.Amount1),
                    SellNotional = group.Where(t => t.Side == "SELL").Sum(t => t.Amount1),
                    NetNotional = group.Sum(t => t.Side == "BUY" ? t.Amount1 : -t.Amount1),
                    WeightedAverageRate = group.Sum(t => t.Amount1) == 0
                        ? 0
                        : group.Sum(t => t.Amount1 * t.FxRateUsed) / group.Sum(t => t.Amount1),
                    TradeCount = group.Count()
                })
                .OrderBy(p => p.Currency)
                .ToListAsync();

            return Ok(positions);
        }


        private async Task<FxRate?> FindLatestStoredFxRate(string fromCurrency, string toCurrency)
        {
            // AsNoTracking is used because FX lookup is read-only. EF Core can skip
            // change tracking, which makes simple read queries lighter.
            return await _tmsDbContext.FxRates
                .AsNoTracking()
                .Where(r => r.FromCurrency == fromCurrency && r.ToCurrency == toCurrency)
                .OrderByDescending(r => r.RateDate)
                .ThenByDescending(r => r.Id)
                .FirstOrDefaultAsync();
        }

        private static FxRateResponse ToFxRateResponse(
            FxRate storedRate,
            string fromCurrency,
            string toCurrency,
            decimal effectiveRate,
            bool isCrossRate)
        {
            return new FxRateResponse
            {
                FromCurrency = fromCurrency,
                ToCurrency = toCurrency,
                Rate = effectiveRate,
                RateDate = storedRate.RateDate,
                Source = storedRate.Source,
                IsCrossRate = isCrossRate
            };
        }

        private static bool IsValidCurrencyCode(string currency)
        {
            return currency.Length == 3 && currency.All(char.IsLetter);
        }
        private static TradeResponse ToResponse(Trade trade)
        {
            return new TradeResponse
            {
                Id = trade.Id,
                TradeReference = trade.TradeReference,
                TradeType = trade.TradeType,
                Counterparty = trade.Counterparty,
                CounterpartyBankAccount = trade.CounterpartyBankAccount,
                Currency1 = trade.Currency1,
                Amount1 = trade.Amount1,
                Currency2 = trade.Currency2,
                Amount2 = trade.Amount2,
                FxRateUsed = trade.FxRateUsed,
                RateDate = trade.RateDate,
                Side = trade.Side,
                TradeDate = trade.TradeDate,
                SettlementDate = trade.SettlementDate,
                Fees = trade.Fees,
                Comments = trade.Comments,
                NearLegDate = trade.NearLegDate,
                NearLegRate = trade.NearLegRate,
                NearLegAmount1 = trade.NearLegAmount1,
                NearLegAmount2 = trade.NearLegAmount2,
                FarLegDate = trade.FarLegDate,
                FarLegRate = trade.FarLegRate,
                FarLegAmount1 = trade.FarLegAmount1,
                FarLegAmount2 = trade.FarLegAmount2,
                SwapPoints = trade.SwapPoints,
                CreatedBy = trade.CreatedBy,
                CreatedUtc = trade.CreatedUtc,
                EditedBy = trade.EditedBy,
                LastEditedUtc = trade.LastEditedUtc
            };
        }
    }
}




