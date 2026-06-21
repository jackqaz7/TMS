using System;
using CoreAPI.Data;
using CoreAPI.Filters;
using CoreAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
            // OK is helper method from ControllerBase class.
            // new {} is an anonymous object to return service status.
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
            var trade = new Trade
            {
                TradeReference = request.TradeReference.Trim(),
                Counterparty = request.Counterparty.Trim(),
                Instrument = request.Instrument.Trim(),
                Currency = request.Currency.Trim().ToUpperInvariant(),
                Side = request.Side.Trim().ToUpperInvariant(),
                Notional = request.Notional,
                Rate = request.Rate,
                TradeDate = request.TradeDate,
                SettlementDate = request.SettlementDate,
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
                .GroupBy(t => t.Currency)
                .Select(group => new PositionSummaryDto
                {
                    Currency = group.Key,
                    BuyNotional = group.Where(t => t.Side == "BUY").Sum(t => t.Notional),
                    SellNotional = group.Where(t => t.Side == "SELL").Sum(t => t.Notional),
                    NetNotional = group.Sum(t => t.Side == "BUY" ? t.Notional : -t.Notional),
                    WeightedAverageRate = group.Sum(t => t.Notional) == 0
                        ? 0
                        : group.Sum(t => t.Notional * t.Rate) / group.Sum(t => t.Notional),
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
                Counterparty = trade.Counterparty,
                Instrument = trade.Instrument,
                Currency = trade.Currency,
                Side = trade.Side,
                Notional = trade.Notional,
                Rate = trade.Rate,
                LocalAmount = trade.Notional * trade.Rate,
                TradeDate = trade.TradeDate,
                SettlementDate = trade.SettlementDate,
                CreatedUtc = trade.CreatedUtc
            };
        }
    }
}
