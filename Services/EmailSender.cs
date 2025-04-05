using Microsoft.AspNetCore.Identity.UI.Services;
using System.Threading.Tasks;

namespace SecureAuthApi.Services
{
    public class EmailSender : IEmailSender
    {
        // W prawdziwej aplikacji wdroż wysyłkę emaili (np. za pomocą SMTP lub zewnętrznego serwisu).
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // Dla celów demonstracyjnych wypisujemy dane do konsoli.
            Console.WriteLine($"Wysyłanie emaila do: {email}");
            Console.WriteLine($"Temat: {subject}");
            Console.WriteLine($"Treść: {htmlMessage}");
            return Task.CompletedTask;
        }
    }
}
