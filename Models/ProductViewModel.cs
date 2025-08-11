using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;

namespace BrewMaster.Models
{
    public class ProductViewModel
    {
        [Key]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Product Name is required")]
        [StringLength(255, ErrorMessage = "Product Name cannot exceed 255 characters")]
        public string ProductName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Product Description is required")]
        public string ProductDescription { get; set; } = string.Empty;

        public byte[]? ProductImage { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, 999999.99, ErrorMessage = "Price should be valid")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Stock is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock cannot be negative")]
        public int Stock { get; set; }

        public DateTime CreatedDate { get; set; }

        [NotMapped]
        [Display(Name = "Product Image")]
        public IFormFile? ImageUpload { get; set; }

        [NotMapped]
        public DataTable? ProductTable { get; set; }

        [NotMapped]
        public string FormattedPrice => $"₹{Price:N2}";

        [NotMapped]
        public string StockStatus => Stock switch
        {
            0 => "Out of Stock",
            < 10 => $"{Stock} Left",
            _ => "In Stock"
        };

        [NotMapped]
        public string StockClass => Stock switch
        {
            0 => "out-of-stock",
            < 10 => "low-stock",
            _ => "in-stock"
        };

        [NotMapped]
        public bool IsAvailable => Stock > 0;

        [NotMapped]
        public string ImageUrl => $"/User/ProductImage/{ProductId}";

        public string TruncatedDescription(int maxLength = 250)
        {
            if (string.IsNullOrEmpty(ProductDescription))
                return "No description available";

            return ProductDescription.Length > maxLength
                ? ProductDescription.Substring(0, maxLength) + "..."
                : ProductDescription;
        }
    }
}