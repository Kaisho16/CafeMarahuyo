using System.Threading.Tasks;
using CafeMarahuyo.Shared.Auth;
using CafeMarahuyo.Shared.Data;
using CafeMarahuyo.Shared.DTOs;
using CafeMarahuyo.Shared.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using System.Security.Claims;
using System.Linq;

namespace CafeMarahuyo.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly CafeDbContext _context;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _config;

        public AuthController(CafeDbContext context, Microsoft.Extensions.Configuration.IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            if (string.IsNullOrEmpty(req.Username) || string.IsNullOrEmpty(req.Password))
                return BadRequest(new { error = "Username and password are required" });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == req.Username);
            if (user == null)
                return Unauthorized(new { error = "Invalid credentials" });

            bool validPassword = BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash);
            if (!validPassword)
                return Unauthorized(new { error = "Invalid credentials" });

            var token = JwtHelper.GenerateToken(user.Id, user.Username, user.Role, user.DisplayName);

            return Ok(new LoginResponse
            {
                Token = token,
                User = new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    DisplayName = user.DisplayName,
                    Role = user.Role
                }
            });
        }

        [HttpGet("me")]
        [Authorize]
        public IActionResult GetMe()
        {
            var userIdStr = User.FindFirstValue("id");
            var username = User.FindFirstValue("username");
            var displayName = User.FindFirstValue("displayName");
            var role = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role");

            return Ok(new
            {
                id = int.Parse(userIdStr!),
                username,
                displayName,
                role
            });
        }

        [HttpGet("users")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    DisplayName = u.DisplayName,
                    Role = u.Role,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest req)
        {
            if (string.IsNullOrEmpty(req.Username) || string.IsNullOrEmpty(req.Password) || string.IsNullOrEmpty(req.DisplayName))
                return BadRequest(new { error = "Username, password, and display name are required" });

            var validRoles = new[] { "admin", "staff", "Inventory Manager" };
            if (!string.IsNullOrEmpty(req.Role) && !validRoles.Contains(req.Role))
                return BadRequest(new { error = "Role must be admin, staff, or Inventory Manager" });

            var role = req.Role ?? "staff";

            if (role == "admin" || !User.Identity!.IsAuthenticated)
            {
                var secretKey = _config["AdminSecretKey"];
                if (string.IsNullOrEmpty(req.SecretKey) || req.SecretKey != secretKey)
                {
                    return Unauthorized(new { error = "Valid secret key is required to register an admin or to register without being logged in" });
                }
            }
            else
            {
                // Authenticated user trying to create a staff account
                if (!User.IsInRole("admin"))
                {
                    return Forbid();
                }
            }

            var existing = await _context.Users.AnyAsync(u => u.Username == req.Username);
            if (existing)
                return Conflict(new { error = "Username already exists" });

            var user = new User
            {
                Username = req.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
                DisplayName = req.DisplayName,
                Role = role
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return StatusCode(201, new
            {
                id = user.Id,
                username = user.Username,
                displayName = user.DisplayName,
                role = user.Role
            });
        }
    }
}
