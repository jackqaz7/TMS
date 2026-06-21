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
            return Ok(new { Service = "Treasury", Status = "Running", Timestamp = DateTime.UtcNow });
        }

        [Route("Process")]
        [MethodFilter("POST")]
        public IActionResult ProcessTrade([FromBody] TransactionDto transaction)
        {
            // Kept as a simple starter endpoint from the first API slice. New trade capture
            // logic lives in POST /api/treasury/trades below.
            return Ok(new { service = "Treasury", transaction.Id });
        }

        [HttpPost("trades")]
        public async Task<ActionResult<TradeResponse>> CreateTrade([FromBody] CreateTradeRequest request)
        {
            // API request DTOs are mapped into database entities instead of saving the
            // request object directly. This keeps external contracts separate from storage.
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

            // Add marks the entity as new in EF Core's change tracker. SaveChangesAsync
            // translates that pending change into an INSERT statement.
            _tmsDbContext.Trades.Add(trade);
            await _tmsDbContext.SaveChangesAsync();

            // 201 Created is useful for REST clients because it returns the created object
            // and points to the endpoint that can retrieve it later.
            return CreatedAtAction(nameof(GetTrade), new { id = trade.Id }, ToResponse(trade));
        }

        [HttpGet("trades")]
        public async Task<ActionResult<IEnumerable<TradeResponse>>> GetTrades()
        {
            // This LINQ query is not executed until ToListAsync. EF Core converts the sort
            // and projection into SQL where possible.
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
            // FindAsync searches by primary key. EF Core can return a tracked entity from
            // memory first, or query the database if it is not already loaded.
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
            // Position is a read model: it is calculated from trades, not stored directly.
            // GroupBy lets SQL Server aggregate by currency before results return to .NET.
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
            // Response DTOs give the API freedom to shape output without exposing every
            // database field or EF tracking detail to clients.
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
