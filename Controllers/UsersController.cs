using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GroceryOrderingApp.Backend.Repositories;
using System.Threading.Tasks;

namespace GroceryOrderingApp.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserRepository userRepository, ILogger<UsersController> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        [HttpPost("fcm-token")]
        [Authorize]
        public async Task<IActionResult> RegisterFcmToken([FromBody] RegisterFcmTokenRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst("userId")?.Value;
                if (!int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized();
                }

                var user = await _userRepository.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return NotFound("User not found");
                }

                user.FcmToken = request.FcmToken;
                user.UpdatedAt = DateTime.UtcNow;
                await _userRepository.UpdateUserAsync(user);

                _logger.LogInformation($"FCM token registered for user {userId}");
                return Ok(new { Success = true, Message = "FCM token registered successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering FCM token");
                return StatusCode(500, "Failed to register FCM token");
            }
        }
    }

    public class RegisterFcmTokenRequest
    {
        public string FcmToken { get; set; } = string.Empty;
    }
}
