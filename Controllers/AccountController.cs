using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SecureAuthApi.Models;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;
using Google.Apis.Auth;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace SecureAuthApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration,
            IEmailSender emailSender,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _emailSender = emailSender;
            _logger = logger;
        }

        // Rejestracja użytkownika z wysłaniem linku weryfikacyjnego email
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                _logger.LogInformation("User created successfully with ID: {UserId}", user.Id);

                // Generowanie tokena weryfikacyjnego
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var confirmationLink = Url.Action("ConfirmEmail", "Account",
                    new { userId = user.Id, token = token }, Request.Scheme);

                // Wysłanie emaila
                await _emailSender.SendEmailAsync(user.Email, "Potwierdź swój email",
                    $"Kliknij w link, aby potwierdzić swój email: {confirmationLink}");

                return Ok(new { message = "Rejestracja zakończona sukcesem. Sprawdź swoją skrzynkę email." });
            }

            return BadRequest(result.Errors);
        }

        // Potwierdzanie adresu email (kliknięcie w link wysłany na email)
        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
                return BadRequest(new { message = "Nieprawidłowy link weryfikacyjny" });

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return BadRequest(new { message = "Użytkownik nie istnieje" });

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
                return Ok(new { message = "Email potwierdzony pomyślnie" });

            return BadRequest(new { message = "Nie udało się potwierdzić emaila" });
        }

        // Standardowe logowanie – uwzględnia potwierdzenie email
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return Unauthorized(new AuthResponse 
                { 
                    Token = string.Empty,
                    Message = "Nieprawidłowy email lub hasło",
                    Success = false
                });

            if (!await _userManager.IsEmailConfirmedAsync(user))
                return Unauthorized(new AuthResponse 
                { 
                    Token = string.Empty,
                    Message = "Email nie został potwierdzony",
                    Success = false
                });

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
            if (result.Succeeded)
            {
                _logger.LogInformation("User logged in successfully with ID: {UserId}", user.Id);
                var token = GenerateJwtToken(user);
                return Ok(new AuthResponse 
                { 
                    Token = token,
                    Message = "Logowanie zakończone sukcesem",
                    Success = true
                });
            }

            return Unauthorized(new AuthResponse 
            { 
                Token = string.Empty,
                Message = "Nieprawidłowy email lub hasło",
                Success = false
            });
        }

        // Żądanie resetowania hasła – wysłanie linku resetującego na email
        [HttpPost("forgotpassword")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                return BadRequest("Nieprawidłowy email");

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // Generacja linku do resetu hasła
            var resetLink = Url.Action(nameof(ResetPassword), "Account",
                new { email = user.Email, token = token }, Request.Scheme);

            await _emailSender.SendEmailAsync(user.Email, "Resetowanie hasła",
                $"Kliknij w poniższy link, aby zresetować hasło: <a href='{resetLink}'>Resetuj hasło</a>");

            return Ok("Link do resetowania hasła został wysłany");
        }

        // Resetowanie hasła – wykonanie zmiany hasła przy użyciu tokena
        [HttpPost("resetpassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return BadRequest("Nieprawidłowy email");

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);
            if (result.Succeeded)
                return Ok("Hasło zostało zresetowane pomyślnie");

            return BadRequest(result.Errors);
        }

        // Logowanie przez Google – weryfikacja tokena otrzymanego z klienta
        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginModel model)
        {
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings()
                {
                    Audience = new[] { _configuration["Authentication:Google:ClientId"] }
                };

                var payload = await GoogleJsonWebSignature.ValidateAsync(model.IdToken, settings);
                _logger.LogInformation("Google login successful for email: {Email}, User ID: {UserId}", 
                    payload.Email, payload.Subject);

                // Próba znalezienia użytkownika po emailu
                var user = await _userManager.FindByEmailAsync(payload.Email);
                if (user == null)
                {
                    // Jeśli użytkownika nie ma, tworzony jest nowy – email ustawiony jako potwierdzony
                    user = new ApplicationUser
                    {
                        UserName = payload.Email,
                        Email = payload.Email,
                        EmailConfirmed = true,
                        FullName = payload.Name
                    };
                    var result = await _userManager.CreateAsync(user);
                    if (!result.Succeeded)
                    {
                        _logger.LogError("Failed to create user from Google login: {Errors}", 
                            string.Join(", ", result.Errors.Select(e => e.Description)));
                        return BadRequest(new AuthResponse 
                        { 
                            Token = string.Empty,
                            Message = string.Join(", ", result.Errors.Select(e => e.Description)),
                            Success = false
                        });
                    }
                    _logger.LogInformation("Created new user from Google login with ID: {UserId}", user.Id);
                }

                // Generowanie tokena JWT
                var token = GenerateJwtToken(user);
                return Ok(new AuthResponse 
                { 
                    Token = token,
                    Message = "Logowanie przez Google zakończone sukcesem",
                    Success = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Google login");
                return BadRequest(new AuthResponse 
                { 
                    Token = string.Empty,
                    Message = "Nieprawidłowy token Google",
                    Success = false
                });
            }
        }

        // Metoda pomocnicza do generowania tokena JWT
        private string GenerateJwtToken(ApplicationUser user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Secret"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            _logger.LogInformation("Generating JWT token for user ID: {UserId}", user.Id);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(Convert.ToDouble(jwtSettings["ExpiryMinutes"])),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
