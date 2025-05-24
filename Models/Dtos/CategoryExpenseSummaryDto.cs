namespace SecureAuthApi.Models.Dtos
{
    public class CategoryExpenseSummaryDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = default!;
        public decimal TotalAmount { get; set; }
    }
}
