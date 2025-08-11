using System.ComponentModel.DataAnnotations;

namespace BrewMaster.Models
{
    public class UserAccountViewModel
    {
        public string? UserName { get; set; }

        [Required(ErrorMessage = "First name is required.")]
        public string? FirstName { get; set; }

        public string? SurName { get; set; }

        [Phone(ErrorMessage = "Invalid phone number.")]
        public string? Mobile { get; set; }

        public string? StreetAddress { get; set; }
        public string? City { get; set; }
        public string? UserState { get; set; }

        [RegularExpression(@"^\d{5}(-\d{4})?$", ErrorMessage = "Invalid postal code.")]
        public string? PostalCode { get; set; }

        public string? Country { get; set; }
    }
    public class UserAccountFieldUpdateViewModel
    {
        public string? FieldName { get; set; }
        public string? FieldValue { get; set; }
    }
}
