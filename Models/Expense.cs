using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SecureAuthApi.Models
{
    public class Expense
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Opis jest wymagany")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Kwota jest wymagana")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Kwota musi być większa od 0")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Data jest wymagana")]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "Kategoria jest wymagana")]
        public int CategoryId { get; set; }

        public string UserId { get; set; }

        // Navigation properties
        public virtual Category Category { get; set; }
        
        [JsonIgnore]
        public virtual ApplicationUser User { get; set; }
    }
}
