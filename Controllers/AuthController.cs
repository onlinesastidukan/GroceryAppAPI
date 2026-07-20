using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GroceryOrderingApp.Backend.Services;
using GroceryOrderingApp.Backend.DTOs;

namespace GroceryOrderingApp.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            if ((string.IsNullOrWhiteSpace(request.UserId) && string.IsNullOrWhiteSpace(request.MobileNumber)) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("MobileNumber (or UserId) and Password are required");
            }

            var result = await _authService.LoginAsync(request);
            if (result == null)
                return Unauthorized("Invalid dealer/admin credentials");

            return Ok(result);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.FullName) ||
                string.IsNullOrWhiteSpace(request.MobileNumber) ||
                string.IsNullOrWhiteSpace(request.Address))
            {
                return BadRequest("Password, FullName, MobileNumber, and Address are required");
            }

            var result = await _authService.RegisterAsync(request);
            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result);
        }

        [Authorize]
        [HttpPost("update-fcm-token")]
        public async Task<IActionResult> UpdateFcmToken([FromBody] UpdateFcmTokenRequestDto request)
        {
            Console.WriteLine($"[FCM] Received FCM token update request");

            if (string.IsNullOrWhiteSpace(request.FcmToken))
            {
                Console.WriteLine($"[FCM] Empty FCM token received");
                return BadRequest("FCM token is required");
            }

            Console.WriteLine($"[FCM] Token length: {request.FcmToken.Length}");

            var userIdClaim = User.FindFirst("userId")?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                Console.WriteLine($"[FCM] User not authenticated or invalid userId claim");
                return Unauthorized("User not authenticated");
            }

            Console.WriteLine($"[FCM] Processing FCM token update for user ID: {userId}");

            var result = await _authService.UpdateFcmTokenAsync(userId, request.FcmToken);
            if (!result)
            {
                Console.WriteLine($"[FCM] Failed to update FCM token for user ID: {userId}");
                return BadRequest("Failed to update FCM token");
            }

            Console.WriteLine($"[FCM] FCM token update successful for user ID: {userId}");
            return Ok(new { Success = true, Message = "FCM token updated successfully" });
        }
    }
}
