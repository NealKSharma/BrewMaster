namespace BrewMaster.Models
{
    public class HomeViewModel
    {
        public List<ProductViewModel> Products { get; set; } = new List<ProductViewModel>();
        public string? ToastMessage { get; set; }
        public string ToastType { get; set; } = "info";
        public bool HasProducts => Products != null && Products.Any();
    }
}