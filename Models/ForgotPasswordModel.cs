using System.ComponentModel.DataAnnotations;

namespace SecureAuthApi.Models
{
    public class ForgotPasswordModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
