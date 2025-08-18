using System.ComponentModel.DataAnnotations;

namespace BrewMaster.Models
{
    public class CartViewModel
    {
        public List<CartItem> Items { get; set; } = new List<CartItem>();
        public decimal Total => Items.Sum(x => x.Price * x.Quantity);
        public int ItemCount => Items.Sum(x => x.Quantity);
        public bool IsEmpty => !Items.Any();
    }

    public class CartItem
    {
        public int CartId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public int Stock { get; set; }
        public decimal ItemTotal => Price * Quantity;
    }
}