using Microsoft.AspNetCore.Identity;

namespace SecureAuthApi.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Dodatkowa właściwość – pełne imię i nazwisko
        public string FullName { get; set; }
    }
}
