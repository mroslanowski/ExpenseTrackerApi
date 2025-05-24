namespace SecureAuthApi.Models
{
    public class Expense
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; }

        // relacja do kategorii
        public int CategoryId { get; set; }
        public Category Category { get; set; }

        // opcjonalnie: relacja do użytkownika
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
    }
}
