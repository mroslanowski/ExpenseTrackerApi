using System.ComponentModel.DataAnnotations;

namespace SecureAuthApi.Models
{
    public class RegisterModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required]
        [MinLength(8, ErrorMessage = "Hasło musi mieć co najmniej 8 znaków")]
        public string Password { get; set; }

        [Required]
        [Compare("Password", ErrorMessage = "Hasła muszą być identyczne")]
        public string ConfirmPassword { get; set; }
    }
}
