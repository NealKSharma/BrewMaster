using System.ComponentModel.DataAnnotations;

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
        [Range(0.01, 999999.99, ErrorMessage = "Price must be between 0.01 and 999999.99")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Stock is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock cannot be negative")]
        public int Stock { get; set; }

        public DateTime CreatedDate { get; set; }

        [DataType(DataType.Upload)]
        public IFormFile? ImageUpload { get; set; }
    }
}