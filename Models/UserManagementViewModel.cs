using System.ComponentModel.DataAnnotations;
using System.Data;

namespace BrewMaster.Models
{
    public class UserManagementViewModel
    {
        public string? SearchUsername { get; set; }

        [Required(ErrorMessage = "Username is required")]
        public string? Username { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        public string? Email { get; set; }

        [Display(Name = "First Name")]
        public string? FirstName { get; set; }

        [Display(Name = "Last Name")]
        public string? LastName { get; set; }

        [Required(ErrorMessage = "User role is required")]
        [Display(Name = "User Role")]
        public string UserRole { get; set; } = "User";

        [StringLength(255, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 255 characters")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])[A-Za-z\d@$!%*?&]{6,}$",
            ErrorMessage = "Password must contain at least one uppercase and one lowercase letter")]
        public string? Password { get; set; }

        public DataTable? UsersTable { get; set; }
    }
}
