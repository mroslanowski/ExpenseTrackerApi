using System.ComponentModel.DataAnnotations;

namespace SecureAuthApi.Models.Dtos
{
    public class CreateExpenseDto
    {
        [Required(ErrorMessage = "Kwota jest wymagana")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Kwota musi być większa od 0")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Opis jest wymagany")]
        [MinLength(3, ErrorMessage = "Opis musi mieć co najmniej 3 znaki")]
        public string Description { get; set; } = default!;

        [Required(ErrorMessage = "Data jest wymagana")]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "Kategoria jest wymagana")]
        public int CategoryId { get; set; }
    }
} 