using System;
using System.Security.Claims;
using CoreAPI.Data;
using CoreAPI.Filters;
using CoreAPI.Models;
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

        public TreasuryController(TmsDbContext tmsDbContext)
        {
            _tmsDbContext = tmsDbContext;
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

        [HttpPost("trades")]
        public async Task<ActionResult<TradeResponse>> CreateTrade([FromBody] CreateTradeRequest request)
        {
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
