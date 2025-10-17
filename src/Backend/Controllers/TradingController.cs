using AutoBot.Models.Api;
using AutoBot.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoBot.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TradingController(ITradeManager _tradeManager, ILogger<TradingController> _logger) : ControllerBase
{
    [HttpGet("account-details")]
    public ActionResult<AccountDetailsResponse> GetAccountDetails()
    {
        try
        {
            _logger.LogDebug("{Endpoint}", nameof(GetAccountDetails));

            var accountOverview = _tradeManager.GetAccountDetails();
            if (accountOverview == null)
            {
                return StatusCode(500, "Failed to retrieve account details");
            }

            var details = new AccountDetailsResponse
            {
                TotalNetValue = accountOverview.Value.TotalNetValue,
                Balances = accountOverview.Value.Balances,
                TotalQuantity = accountOverview.Value.TotalQuantity,
                Margins = accountOverview.Value.Margins,
                ProfitLoss = accountOverview.Value.ProfitLoss,
                CurrentPrice = accountOverview.Value.CurrentPrice,
            };

            return Ok(details);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Endpoint}: ", nameof(GetAccountDetails));
            return StatusCode(500, "An unexpected error occurred");
        }
    }

    [HttpPost("create-managed-position")]
    public async Task<IActionResult> CreateManagedPosition([FromBody] CreateManagedPositionRequest request)
    {
        try
        {
            _logger.LogDebug("{Endpoint}: {Request}", nameof(CreateManagedPosition), request);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest($"Validation failed: {string.Join(", ", errors)}");
            }

            if (!await _tradeManager.CreateManagedPositionAsync(request.AmountInSats))
            {
                return StatusCode(500, "Failed to create managed position");
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Endpoint}: ", nameof(CreateManagedPosition));
            return StatusCode(500, "An unexpected error occurred");
        }
    }
}
