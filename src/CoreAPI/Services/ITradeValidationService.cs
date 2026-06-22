using CoreAPI.Models;

namespace CoreAPI.Services
{
    // Interface for trade validation.
    // Using an interface keeps the controller dependent on an abstraction instead of
    // a concrete class. That makes the validation rules easier to unit test and easier
    // to replace later, for example with DB-backed rules or FluentValidation.
    public interface ITradeValidationService
    {
        // Returns all validation errors instead of throwing on the first one.
        // This is friendlier for WPF/React because the UI can show the user every
        // problem with the trade in one response.
        IReadOnlyList<string> ValidateCreateTrade(CreateTradeRequest request);
    }
}
