using DataVisualizationAPI.Data;
using DataVisualizationAPI.Models;
using DataVisualizationAPI.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static BCrypt.Net.BCrypt;
using DataVisualizationAPI.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace DataVisualizationAPI.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly JwtService _jwtService;
        private readonly IEmailService _emailService;
        private readonly IRedisService _redisService;
        private readonly IConfiguration _configuration;

        public AuthController(
            AppDbContext context, 
            JwtService jwtService, 
            IEmailService emailService, 
            IRedisService redisService,
            IConfiguration configuration)
        {
            _context = context;
            _jwtService = jwtService;
            _emailService = emailService;
            _redisService = redisService;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Username) ||
                string.IsNullOrWhiteSpace(dto.Email) ||
                string.IsNullOrWhiteSpace(dto.Password) ||
                string.IsNullOrWhiteSpace(dto.FullName))
            {
                return BadRequest(new { message = "Please provide all required fields: Username, FullName, Email, and Password." });
            }

            if (_context.Users.Any(u => u.Email == dto.Email || u.Username == dto.Username))
            {
                return BadRequest(new { message = "Email or Username already exists." });
            }

            var hashedPassword = HashPassword(dto.Password);

            var user = new User
            {
                Username = dto.Username,
                FullName = dto.FullName,
                Email = dto.Email,
                Password = hashedPassword,
                Phone = dto.Phone,
                Company = dto.Company
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var userToken = new UserTokenDTO
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                Company = user.Company
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

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDTO dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
            {
                // Return OK even if user not found to prevent email enumeration
                return Ok(new { message = "If your email is registered, you will receive a password reset link." });
            }

            // Generate reset token
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            var tokenExpiry = DateTime.UtcNow.AddHours(1);

            // Store token in Redis with 1 hour expiry
            var tokenData = new ResetTokenData
            {
                UserId = user.Id,
                Email = user.Email,
                Expiry = tokenExpiry
            };
            await _redisService.SetAsync($"reset_token:{token}", tokenData, TimeSpan.FromHours(1));

            // Generate reset link with URL encoded token
            var encodedToken = Uri.EscapeDataString(token);
            var resetLink = $"{_configuration["FrontendUrl"]}/reset-password?token={encodedToken}&email={Uri.EscapeDataString(user.Email)}";

            // Send email using template
            var emailBody = EmailTemplates.GetPasswordResetTemplate(resetLink);

            // Fire and forget email sending
            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailService.SendEmailAsync(
                        user.Email,
                        "Password Reset Request",
                        emailBody
                    );
                }
                catch (Exception ex)
                {
                    // Log the error but don't throw
                    Console.WriteLine($"Error sending email: {ex.Message}");
                }
            });

            return Ok(new { message = "If your email is registered, you will receive a password reset link." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDTO dto)
        {
            // Decode the token
            var decodedToken = Uri.UnescapeDataString(dto.Token);

            // Get token data from Redis
            var tokenData = await _redisService.GetAsync<ResetTokenData>($"reset_token:{decodedToken}");
            if (tokenData == null)
            {
                return BadRequest(new { message = "Invalid or expired reset token." });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null || user.Id != tokenData.UserId)
            {
                return BadRequest(new { message = "Invalid request." });
            }

            // Update password
            user.Password = HashPassword(dto.NewPassword);
            await _context.SaveChangesAsync();

            // Delete the used token from Redis
            await _redisService.DeleteAsync($"reset_token:{decodedToken}");

            return Ok(new { message = "Password has been reset successfully." });
        }

        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var user = await _context.Users.FindAsync(int.Parse(userId));
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(new
            {
                id = user.Id,
                username = user.Username,
                fullName = user.FullName,
                email = user.Email,
                phone = user.Phone,
                company = user.Company
            });
        }

        [Authorize]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile(UpdateProfileDTO dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var user = await _context.Users.FindAsync(int.Parse(userId));
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Update profile fields
            user.FullName = dto.FullName;
            user.Phone = dto.Phone;
            user.Company = dto.Company;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Profile updated successfully",
                id = user.Id,
                username = user.Username,
                fullName = user.FullName,
                email = user.Email,
                phone = user.Phone,
                company = user.Company
            });
        }

        [Authorize]
        [HttpPost("request-email-change")]
        public async Task<IActionResult> RequestEmailChange(RequestEmailChangeDTO dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var user = await _context.Users.FindAsync(int.Parse(userId));
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Check if new email is already taken
            if (await _context.Users.AnyAsync(u => u.Email == dto.NewEmail))
            {
                return BadRequest(new { message = "Email already exists" });
            }

            // Generate OTP
            var otp = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
            var otpExpiry = DateTime.UtcNow.AddMinutes(5);

            // Store OTP in Redis
            var otpData = new EmailChangeOTPData
            {
                UserId = user.Id,
                CurrentEmail = user.Email,
                NewEmail = dto.NewEmail,
                OTP = otp,
                Expiry = otpExpiry
            };
            await _redisService.SetAsync($"email_change_otp:{user.Id}", otpData, TimeSpan.FromMinutes(5));

            // Send OTP email using template
            var emailBody = EmailTemplates.GetEmailChangeOTPTemplate(otp);

            // Fire and forget email sending
            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailService.SendEmailAsync(
                        dto.NewEmail,
                        "Email Change Verification",
                        emailBody
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending email: {ex.Message}");
                }
            });

            return Ok(new { message = "Verification code has been sent to the new email address." });
        }

        [Authorize]
        [HttpPost("verify-email-change")]
        public async Task<IActionResult> VerifyEmailChange(VerifyEmailChangeDTO dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var user = await _context.Users.FindAsync(int.Parse(userId));
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Get OTP data from Redis
            var otpData = await _redisService.GetAsync<EmailChangeOTPData>($"email_change_otp:{user.Id}");
            if (otpData == null)
            {
                return BadRequest(new { message = "No email change request found or verification code has expired." });
            }

            // Verify OTP
            if (dto.OTP != otpData.OTP)
            {
                return BadRequest(new { message = "Invalid verification code." });
            }

            var oldEmail = user.Email;
            // Update email
            user.Email = dto.NewEmail;
            await _context.SaveChangesAsync();

            // Delete OTP from Redis
            await _redisService.DeleteAsync($"email_change_otp:{user.Id}");

            // Send notification to old email using template
            var notificationEmailBody = EmailTemplates.GetEmailChangeNotificationTemplate(oldEmail, dto.NewEmail);

            // Fire and forget email sending
            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailService.SendEmailAsync(
                        oldEmail,
                        "Email Address Changed",
                        notificationEmailBody
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending notification email: {ex.Message}");
                }
            });

            return Ok(new
            {
                message = "Email has been updated successfully.",
                id = user.Id,
                username = user.Username,
                fullName = user.FullName,
                email = user.Email,
                phone = user.Phone,
                company = user.Company
            });
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordDTO dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var user = await _context.Users.FindAsync(int.Parse(userId));
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            if (!Verify(dto.CurrentPassword, user.Password))
            {
                return BadRequest(new { message = "Current password is incorrect" });
            }

            user.Password = HashPassword(dto.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Password changed successfully" });
        }
    }
}
