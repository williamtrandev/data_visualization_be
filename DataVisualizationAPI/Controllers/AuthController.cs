using DataVisualizationAPI.Data;
using DataVisualizationAPI.Models;
using DataVisualizationAPI.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static BCrypt.Net.BCrypt;
using DataVisualizationAPI.Services;
using Microsoft.EntityFrameworkCore;

namespace DataVisualizationAPI.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly JwtService _jwtService;

        public AuthController(AppDbContext context, JwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Username) ||
        string.IsNullOrWhiteSpace(dto.Email) ||
        string.IsNullOrWhiteSpace(dto.Password))
            {
                return BadRequest(new { message = "Please provide all required fields: Username, Email, and Password." });
            }

            if (_context.Users.Any(u => u.Email == dto.Email || u.Username == dto.Username))
            {
                return BadRequest(new { message = "Email or Username already exists." });
            }

            var hashedPassword = HashPassword(dto.Password);

            var user = new User
            {
                Username = dto.Username,
                Email = dto.Email,
                Password = hashedPassword
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var userToken = new UserTokenDTO
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email
            };

            var token = _jwtService.GenerateToken(userToken);

            return Ok(new
            {
                message = "Registration successful.",
                token
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.UserName) || string.IsNullOrWhiteSpace(dto.Password))
            {
                return BadRequest(new { message = "Email and password are required." });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == dto.UserName);
            if (user == null)
            {
                return NotFound(new { message = "Email not found." });
            }

            if (!Verify(dto.Password, user.Password))
            {
                return Unauthorized(new { message = "Incorrect password." });
            }

            var userDto = new UserTokenDTO
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email
            };

            var token = _jwtService.GenerateToken(userDto);

            return Ok(new
            {
                message = "Login successful.",
                token
            });
        }
    }
}
