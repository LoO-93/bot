using AutoBot.Models.Api;
using AutoBot.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoBot.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TradingController(ITradeManager _tradeManager, ILogger<TradingController> _logger) : ControllerBase
{
    [HttpGet("user-balance")]
    public ActionResult<UserBalanceResponse> GetUserBalance()
    {
        try
        {
            _logger.LogDebug("{Endpoint}", nameof(GetUserBalance));

            var user = _tradeManager.GetUser();
            if (user == null)
            {
                return StatusCode(500, "Failed to retrieve user balance");
            }

            var balance = new UserBalanceResponse
            {
                BalanceInSats = decimal.ToInt64(user.balance),
                SyntheticUsdBalance = user.synthetic_usd_balance,
            };

            return Ok(balance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Endpoint}: ", nameof(GetUserBalance));
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
