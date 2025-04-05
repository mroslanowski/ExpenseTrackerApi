using System.ComponentModel.DataAnnotations;

namespace SecureAuthApi.Models
{
    public class ResetPasswordModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Token { get; set; }

        [Required]
        [MinLength(8, ErrorMessage = "Hasło musi mieć co najmniej 8 znaków")]
        public string NewPassword { get; set; }

        [Required]
        [Compare("NewPassword", ErrorMessage = "Hasła muszą być identyczne")]
        public string ConfirmPassword { get; set; }
    }
}
