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
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            // Generowanie tokena dla potwierdzenia email
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            // Generowanie linku potwierdzającego email (używając Url.Action)
            var confirmationLink = Url.Action(nameof(ConfirmEmail), "Account",
                new { userId = user.Id, token = token }, Request.Scheme);

            // Wysłanie emaila z linkiem weryfikacyjnym
            await _emailSender.SendEmailAsync(user.Email, "Potwierdź swój email",
                $"Kliknij w poniższy link, aby potwierdzić swój email: <a href='{confirmationLink}'>Potwierdź email</a>");

            // Generowanie tokena JWT dla nowego użytkownika
            var jwtToken = GenerateJwtToken(user);

            var response = new
            {
                Token = jwtToken,
                Message = "Rejestracja zakończona. Sprawdź email, aby potwierdzić konto.",
                Success = true
            };

            // Logowanie odpowiedzi
            _logger.LogInformation("Register response: {Response}", JsonSerializer.Serialize(response));

            return Ok(response);
        }

        // Potwierdzanie adresu email (kliknięcie w link wysłany na email)
        [HttpGet("confirmemail")]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
                return BadRequest("Nieprawidłowe dane");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound("Użytkownik nie został znaleziony");

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
                return Ok("Email został pomyślnie potwierdzony");

            return BadRequest("Błąd przy potwierdzaniu emaila");
        }

        // Standardowe logowanie – uwzględnia potwierdzenie email
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return Unauthorized("Nieprawidłowe dane logowania");

            // Sprawdzenie, czy email został potwierdzony
            if (!await _userManager.IsEmailConfirmedAsync(user))
                return Unauthorized("Email nie został potwierdzony");

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
            if (!result.Succeeded)
                return Unauthorized("Nieprawidłowe dane logowania");

            var token = GenerateJwtToken(user);
            return Ok(new AuthResponse 
            { 
                Token = token,
                Message = "Login successful",
                Success = true
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
        [HttpPost("googlelogin")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginModel model)
        {
            // Weryfikacja przekazanego tokena od Google
            GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(model.IdToken);
            }
            catch
            {
                return BadRequest("Nieprawidłowy token Google");
            }

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
                    return BadRequest(result.Errors);
            }

            // Generowanie tokena JWT
            var token = GenerateJwtToken(user);
            return Ok(new { Token = token });
        }

        // Metoda pomocnicza do generowania tokena JWT
        private string GenerateJwtToken(ApplicationUser user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Secret"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id)
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
