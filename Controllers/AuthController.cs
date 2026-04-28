using BCrypt.Net;
using MemmoApi.Data;
using MemmoApi.DTOs;
using MemmoApi.Models;
using MemmoApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace MemmoApi.Controllers
{
    [ApiController]
    [AllowAnonymous]
    [Route("[Controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;

        public AuthController(ApplicationDbContext context, IConfiguration configuration, IEmailService emailService)
        {
            _context = context;
            _configuration = configuration;
            _emailService = emailService;
        }

        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> Register(RegisterDTO dto)
        {
            if (await _context.Users.AnyAsync(u => u.UserName == dto.UserName))
            {
                return BadRequest("This Username already Exist");
            }

            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            {
                return BadRequest("This Email already Exist");
            }

            var emailVerificationEnabled = _configuration.GetValue<bool>("EmailVerification:Enabled");

            var verificationToken = emailVerificationEnabled
                ? Convert.ToHexString(RandomNumberGenerator.GetBytes(32))
                : null;

            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                Name = dto.Name,
                UserName = dto.UserName,
                Email = dto.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                IsEmailVerified = !emailVerificationEnabled,
                EmailVerificationToken = verificationToken,
                EmailVerificationTokenExpiry = emailVerificationEnabled ? DateTime.UtcNow.AddHours(24) : null
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            if (!emailVerificationEnabled)
            {
                return Ok(new
                {
                    emailVerificationRequired = false,
                    message = "Registration successful. You can log in now."
                });
            }

            var frontendBaseUrl = _configuration["FrontendBaseUrl"] ?? "http://localhost:4200";
            var verificationLink = $"{frontendBaseUrl}/verify-email?token={verificationToken}";

            try
            {
                await _emailService.SendEmailVerificationAsync(user.Email!, user.Name!, verificationLink);
            }
            catch (Exception ex)
            {
                // Email server unavailable: keep account pending so user can request resend later.
                return StatusCode(500, new
                {
                    emailVerificationRequired = true,
                    message = "Registration saved, but the verification email could not be sent. Please contact support or try again later.",
                    detail = ex.Message
                });
            }

            return Ok(new
            {
                emailVerificationRequired = true,
                message = "Registration successful. Please check your email to verify your account."
            });
        }

        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login(LoginDTO login)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == login.UserName);
            if (user == null || !BCrypt.Net.BCrypt.Verify(login.Password, user.Password))
            {
                return Unauthorized("Username or Password Invalid");
            }

            var emailVerificationEnabled = _configuration.GetValue<bool>("EmailVerification:Enabled");
            if (emailVerificationEnabled && !user.IsEmailVerified)
            {
                return Unauthorized("Please verify your email before logging in.");
            }

            var token = GenerateToken(user, "access", TimeSpan.FromHours(3));
            var refreshToken = GenerateToken(user, "refresh", TimeSpan.FromDays(7));
            return Ok(new
            {
                token = token,
                refreshToken = refreshToken,
                message = "Login Success"
            });
        }

        [HttpGet]
        [Route("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return BadRequest("Verification token is required.");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.EmailVerificationToken == token);
            if (user == null)
            {
                return BadRequest("Invalid verification token.");
            }

            if (user.EmailVerificationTokenExpiry < DateTime.UtcNow)
            {
                return BadRequest("Verification token has expired.");
            }

            user.IsEmailVerified = true;
            user.EmailVerificationToken = null;
            user.EmailVerificationTokenExpiry = null;
            await _context.SaveChangesAsync();

            var accessToken = GenerateToken(user, "access", TimeSpan.FromHours(3));
            var refreshToken = GenerateToken(user, "refresh", TimeSpan.FromDays(7));

            return Ok(new
            {
                token = accessToken,
                refreshToken = refreshToken,
                message = "Email verified successfully."
            });
        }

        [HttpPost]
        [Route("refresh-token")]
        public async Task<IActionResult> RefreshToken(RefreshTokenDTO dto)
        {
            var principal = ValidateToken(dto.RefreshToken, "refresh");
            if (principal == null)
            {
                return Unauthorized("Refresh token is invalid or expired");
            }

            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized("Invalid token payload");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return Unauthorized("User not found");
            }

            var token = GenerateToken(user, "access", TimeSpan.FromHours(3));
            var refreshToken = GenerateToken(user, "refresh", TimeSpan.FromDays(7));

            return Ok(new
            {
                token = token,
                refreshToken = refreshToken,
                message = "Token refreshed successfully"
            });
        }

        private string GenerateToken(User user, string tokenType, TimeSpan expiresIn)
        {
            var (secretKey, issuer, audience) = GetJwtSettings();

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
            new Claim(ClaimTypes.NameIdentifier, user.Id ?? string.Empty),
            new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
            new Claim(ClaimTypes.Email, user.Email ?? ""),
            new Claim("FullName", user.Name ?? ""),
            new Claim("tokenType", tokenType),
            new Claim(JwtRegisteredClaimNames.Jti, Convert.ToHexString(RandomNumberGenerator.GetBytes(16)))
        };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.Add(expiresIn),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private ClaimsPrincipal? ValidateToken(string token, string expectedTokenType)
        {
            var (secretKey, issuer, audience) = GetJwtSettings();

            var tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out var validatedToken);

                if (validatedToken is not JwtSecurityToken jwtToken ||
                    !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return null;
                }

                var tokenType = principal.FindFirstValue("tokenType");
                if (!string.Equals(tokenType, expectedTokenType, StringComparison.Ordinal))
                {
                    return null;
                }

                return principal;
            }
            catch
            {
                return null;
            }
        }

        private (string secretKey, string issuer, string audience) GetJwtSettings()
        {
            var secretKey = _configuration["secretKey"];
            var issuer = _configuration["issuer"];
            var audience = _configuration["audience"];

            if (string.IsNullOrWhiteSpace(secretKey) ||
                string.IsNullOrWhiteSpace(issuer) ||
                string.IsNullOrWhiteSpace(audience))
            {
                throw new InvalidOperationException("JWT configuration is missing. Please check secretKey, issuer, and audience.");
            }

            return (secretKey, issuer, audience);
        }
    }
}
