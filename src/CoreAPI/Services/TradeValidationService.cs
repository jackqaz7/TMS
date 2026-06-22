using CoreAPI.Models;

namespace CoreAPI.Services
{
    // Central API-side business validation for trades.
    // This service is intentionally outside the controller so the same validation logic
    // can be reused by multiple endpoints later, such as validate-only, amend trade,
    // import trades, or bulk upload trades.
    public class TradeValidationService : ITradeValidationService
    {
        // HashSet gives quick lookup and avoids long if/else chains.
        // StringComparer.OrdinalIgnoreCase makes "fx_spot" and "FX_SPOT" equivalent.
        private static readonly HashSet<string> AllowedTradeTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "FX_SPOT",
            "FX_FORWARD",
            "FX_SWAP"
        };

        private static readonly HashSet<string> AllowedSides = new(StringComparer.OrdinalIgnoreCase)
        {
            "BUY",
            "SELL"
        };

        public IReadOnlyList<string> ValidateCreateTrade(CreateTradeRequest request)
        {
            var errors = new List<string>();

            // API validation is the authoritative validation layer.
            // WPF and future React can do their own UX checks, but every client must
            // pass through these rules before a trade is saved to the database.
            if (request == null)
            {
                errors.Add("Trade request is required.");
                return errors;
            }

            // Normalize values locally before comparing them. This keeps validation
            // consistent even if a client sends extra spaces or lowercase codes.
            // We do not mutate the request here; the controller still maps/normalizes
            // the values when creating the Trade entity for persistence.
            var tradeReference = NormalizeText(request.TradeReference);
            var tradeType = NormalizeCode(request.TradeType);
            var counterparty = NormalizeText(request.Counterparty);
            var counterpartyBankAccount = NormalizeText(request.CounterpartyBankAccount);
            var currency1 = NormalizeCode(request.Currency1);
            var currency2 = NormalizeCode(request.Currency2);
            var side = NormalizeCode(request.Side);
            var comments = NormalizeText(request.Comments);

            // These length checks mirror dbo.Trades column sizes. This gives the caller
            // a clean 400 response instead of a lower-level SQL truncation/constraint error.
            ValidateRequiredText(errors, tradeReference, "Trade reference", 40);
            ValidateRequiredText(errors, counterparty, "Counterparty", 120);
            ValidateOptionalText(errors, counterpartyBankAccount, "Bank account", 50);
            ValidateOptionalText(errors, comments, "Comments", 500);

            // Trade type decides which fields are required later in the request.
            if (!AllowedTradeTypes.Contains(tradeType))
            {
                errors.Add("Trade type must be FX_SPOT, FX_FORWARD, or FX_SWAP.");
            }

            // Side controls position sign later: BUY adds exposure, SELL reduces exposure.
            if (!AllowedSides.Contains(side))
            {
                errors.Add("Side must be BUY or SELL.");
            }

            // For now we validate only the code shape. Later this should check dbo.Currencies
            // so inactive or unsupported currencies cannot be traded.
            if (!IsValidCurrency(currency1) || !IsValidCurrency(currency2))
            {
                errors.Add("Both currencies must be valid 3-letter currency codes.");
            }
            else if (currency1 == currency2)
            {
                errors.Add("Currency 1 and Currency 2 cannot be same.");
            }

            // Amount and rate rules are core business sanity checks. A trade with zero or
            // negative notional/rate would corrupt positions and average-rate calculations.
            if (request.Amount1 <= 0) errors.Add("Amount 1 must be greater than zero.");
            if (request.Amount2 <= 0) errors.Add("Amount 2 must be greater than zero.");
            if (request.FxRateUsed <= 0) errors.Add("FX rate must be greater than zero.");
            if (request.Fees < 0) errors.Add("Fees cannot be negative.");

            // DateTime default usually means the client did not send a date.
            if (request.RateDate == default) errors.Add("Rate date is required.");
            if (request.TradeDate == default) errors.Add("Trade date is required.");
            if (request.SettlementDate == default) errors.Add("Settlement date is required.");

            if (request.SettlementDate.Date < request.TradeDate.Date)
            {
                errors.Add("Settlement date cannot be before trade date.");
            }

            // FX_SPOT uses the basic fields only. FX_FORWARD needs near-leg fields.
            if (tradeType == "FX_FORWARD")
            {
                ValidateForwardLeg(errors, request);
            }

            // FX_SWAP is a two-leg structure, so it needs both near and far leg fields.
            if (tradeType == "FX_SWAP")
            {
                ValidateForwardLeg(errors, request);
                ValidateSwapLeg(errors, request);
            }

            return errors;
        }

        private static void ValidateForwardLeg(List<string> errors, CreateTradeRequest request)
        {
            // Forward-style trades need near-leg information because the forward leg
            // is the actual future settlement leg.
            if (request.NearLegDate == null) errors.Add("Near leg date is required.");
            if (request.NearLegRate == null || request.NearLegRate <= 0) errors.Add("Near leg rate must be greater than zero.");
            if (request.NearLegAmount1 is <= 0) errors.Add("Near leg amount 1 must be greater than zero.");
            if (request.NearLegAmount2 is <= 0) errors.Add("Near leg amount 2 must be greater than zero.");

            if (request.NearLegDate != null && request.NearLegDate.Value.Date < request.TradeDate.Date)
            {
                errors.Add("Near leg date cannot be before trade date.");
            }
        }

        private static void ValidateSwapLeg(List<string> errors, CreateTradeRequest request)
        {
            // FX swap requires a second leg that reverses/exchanges cashflows later.
            // The far leg must be after the near leg; otherwise it is not a valid swap timeline.
            if (request.FarLegDate == null) errors.Add("Far leg date is required.");
            if (request.FarLegRate == null || request.FarLegRate <= 0) errors.Add("Far leg rate must be greater than zero.");
            if (request.FarLegAmount1 is <= 0) errors.Add("Far leg amount 1 must be greater than zero.");
            if (request.FarLegAmount2 is <= 0) errors.Add("Far leg amount 2 must be greater than zero.");

            if (request.NearLegDate != null && request.FarLegDate != null &&
                request.FarLegDate.Value.Date <= request.NearLegDate.Value.Date)
            {
                errors.Add("Far leg date must be after near leg date.");
            }
        }

        private static bool IsValidCurrency(string currency)
        {
            // ISO currency codes are three alphabetic characters, for example USD/INR/EUR.
            return currency.Length == 3 && currency.All(char.IsLetter);
        }

        private static string NormalizeText(string? value)
        {
            // Null strings become empty strings so downstream validation can use one path.
            return value?.Trim() ?? string.Empty;
        }

        private static string NormalizeCode(string? value)
        {
            // Codes such as currency, side, and trade type should be compared uppercase.
            return NormalizeText(value).ToUpperInvariant();
        }

        private static void ValidateRequiredText(List<string> errors, string value, string fieldName, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add($"{fieldName} is required.");
                return;
            }

            ValidateOptionalText(errors, value, fieldName, maxLength);
        }

        private static void ValidateOptionalText(List<string> errors, string value, string fieldName, int maxLength)
        {
            if (!string.IsNullOrEmpty(value) && value.Length > maxLength)
            {
                errors.Add($"{fieldName} cannot be more than {maxLength} characters.");
            }
        }
    }
}
