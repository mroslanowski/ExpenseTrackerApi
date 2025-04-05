using System.ComponentModel.DataAnnotations;

namespace SecureAuthApi.Models
{
    public class GoogleLoginModel
    {
        // IdToken otrzymany po stronie klienta z Google
        [Required]
        public string IdToken { get; set; }
    }
}
