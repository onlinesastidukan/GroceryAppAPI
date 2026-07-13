using Microsoft.AspNetCore.Mvc;
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
    }
}
