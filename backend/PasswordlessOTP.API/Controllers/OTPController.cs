using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PasswordlessOTP.API.Models;
using PasswordlessOTP.API.Services;
using PasswordlessOTP.API.DTOs;
using FluentValidation;

namespace PasswordlessOTP.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class OTPController : ControllerBase
{
    private readonly IOTPService _otpService;
    private readonly ILogger<OTPController> _logger;

    public OTPController(IOTPService otpService, ILogger<OTPController> logger)
    {
        _otpService = otpService;
        _logger = logger;
    }

    /// <summary>
    /// Generates and sends OTP to the specified user
    /// </summary>
    /// <param name="request">OTP generation request</param>
    /// <returns>OTP generation result</returns>
    [HttpPost("generate")]
    [ProducesResponseType(typeof(OTPGenerationResult), 200)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GenerateOTP([FromBody] GenerateOTPRequest request)
    {
        try
        {
            _logger.LogInformation("Generating OTP for {Identifier} via {DeliveryMethod}", 
                request.Identifier, request.DeliveryMethod);

            var result = await _otpService.GenerateOTPAsync(request.Identifier, request.DeliveryMethod);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(new { error = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating OTP for {Identifier}", request.Identifier);
            return StatusCode(500, new { error = "An error occurred while generating OTP" });
        }
    }

    /// <summary>
    /// Validates the provided OTP code
    /// </summary>
    /// <param name="request">OTP validation request</param>
    /// <returns>OTP validation result with JWT tokens</returns>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(OTPValidationResult), 200)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> ValidateOTP([FromBody] ValidateOTPRequest request)
    {
        try
        {
            _logger.LogInformation("Validating OTP for {Identifier}", request.Identifier);

            var result = await _otpService.ValidateOTPAsync(request.Identifier, request.OTPCode);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(new { error = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating OTP for {Identifier}", request.Identifier);
            return StatusCode(500, new { error = "An error occurred while validating OTP" });
        }
    }

    /// <summary>
    /// Resends OTP with retry logic (30s → 1m → 1.5m)
    /// </summary>
    /// <param name="request">OTP resend request</param>
    /// <returns>OTP generation result</returns>
    [HttpPost("resend")]
    [ProducesResponseType(typeof(OTPGenerationResult), 200)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> ResendOTP([FromBody] ResendOTPRequest request)
    {
        try
        {
            _logger.LogInformation("Resending OTP for {Identifier} via {DeliveryMethod}", 
                request.Identifier, request.DeliveryMethod);

            var result = await _otpService.ResendOTPAsync(request.Identifier, request.DeliveryMethod);

            if (result.Success)
            {
                return Ok(result);
            }

            return BadRequest(new { error = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resending OTP for {Identifier}", request.Identifier);
            return StatusCode(500, new { error = "An error occurred while resending OTP" });
        }
    }

    /// <summary>
    /// Cancels an active OTP request
    /// </summary>
    /// <param name="request">OTP cancellation request</param>
    /// <returns>Success/failure result</returns>
    [HttpPost("cancel")]
    [ProducesResponseType(typeof(bool), 200)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> CancelOTP([FromBody] CancelOTPRequest request)
    {
        try
        {
            _logger.LogInformation("Cancelling OTP for {Identifier}", request.Identifier);

            var result = await _otpService.CancelOTPAsync(request.Identifier);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling OTP for {Identifier}", request.Identifier);
            return StatusCode(500, new { error = "An error occurred while cancelling OTP" });
        }
    }

    /// <summary>
    /// Gets the current OTP status for a user
    /// </summary>
    /// <param name="identifier">User identifier (email or phone)</param>
    /// <returns>OTP status information</returns>
    [HttpGet("status/{identifier}")]
    [ProducesResponseType(typeof(OTPStatusResult), 200)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetOTPStatus(string identifier)
    {
        try
        {
            _logger.LogInformation("Getting OTP status for {Identifier}", identifier);

            var result = await _otpService.GetOTPStatusAsync(identifier);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting OTP status for {Identifier}", identifier);
            return StatusCode(500, new { error = "An error occurred while getting OTP status" });
        }
    }

    /// <summary>
    /// Health check endpoint for OTP service
    /// </summary>
    /// <returns>Service health status</returns>
    [HttpGet("health")]
    [ProducesResponseType(typeof(object), 200)]
    public IActionResult Health()
    {
        return Ok(new
        {
            service = "AccessOTP Service",
            status = "Healthy",
            timestamp = DateTime.UtcNow,
            version = "1.0.0"
        });
    }
}

