using System.ComponentModel.DataAnnotations;

namespace SecureAuthApi.Models
{
    public class Category
    {
        public Category()
        {
            Expenses = new List<Expense>();
        }

        public int Id { get; set; }

        [Required(ErrorMessage = "Nazwa kategorii jest wymagana")]
        [MinLength(2, ErrorMessage = "Nazwa kategorii musi mieć co najmniej 2 znaki")]
        public string Name { get; set; }

        public ICollection<Expense> Expenses { get; set; }
    }
}
