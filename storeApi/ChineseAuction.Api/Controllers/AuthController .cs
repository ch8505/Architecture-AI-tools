using ChineseAuction.Api.Dtos;
using ChineseAuction.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChineseAuction.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IUserService userService,
            ILogger<AuthController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// Authenticate user and get JWT token
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto loginDto)
        {
            var result = await _userService.AuthenticateAsync(loginDto.Email, loginDto.Password);

            if (result == null)
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }

            return Ok(result);
        }

        /// <summary>
        /// Register a new user
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<UserResponseDto>> Register([FromBody] UserCreateDto createDto)
        {
            try
            {
                var user = await _userService.CreateUserAsync(createDto);
                return Ok(user);

            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        [Authorize]
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            // ב-JWT Stateless, השרת לא באמת צריך לעשות כלום.
            // אבל אנחנו מחזירים תשובה חיובית כדי שהלקוח ידע למחוק את הטוקן אצלו.
            return Ok(new { message = "Logged out successfully" });
        }
    }
}

