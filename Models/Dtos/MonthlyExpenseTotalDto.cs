namespace SecureAuthApi.Models.Dtos
{
    public class MonthlyExpenseTotalDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal TotalAmount { get; set; }
    }
}