using AutoBot.Models.Api;
using AutoBot.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoBot.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TradingController(ITradeManager _tradeManager, ILogger<TradingController> _logger) : ControllerBase
{
    [HttpPost("create-managed-position")]
    public async Task<ActionResult<ApiResponse<object>>> CreateManagedPosition([FromBody] CreateManagedPositionRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage);
                return BadRequest(ApiResponseFactory.CreateErrorResult($"Validation failed: {string.Join(", ", errors)}"));
            }

            _logger.LogInformation("API request to create managed position with {Amount} sats", request.AmountInSats);

            var success = await _tradeManager.CreateManagedPositionAsync(request.AmountInSats);

            if (!success)
            {
                return StatusCode(500, ApiResponseFactory.CreateErrorResult("Failed to create managed position"));
            }

            return Ok(ApiResponseFactory.CreateSuccessResult($"Successfully created managed position with {request.AmountInSats} sats"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating managed position via API");
            return StatusCode(500, ApiResponseFactory.CreateErrorResult("An unexpected error occurred"));
        }
    }
}
