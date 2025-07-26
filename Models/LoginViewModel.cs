using System.ComponentModel.DataAnnotations;

namespace BrewMaster.Models
{
    // Used for Login
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Username is required.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }

    // Used for AJAX lookup
    public class UsernameRequest
    {
        public string Username { get; set; } = string.Empty;
    }

    // Used for Password Reset
    public class ResetPasswordViewModel
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        public string? SecurityQuestion { get; set; }

        [Required(ErrorMessage = "Security answer is required.")]
        public string SecurityAnswer { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm password is required.")]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    // Wrapper ViewModel for Login + Reset
    public class CombinedLoginResetViewModel
    {
        public LoginViewModel Login { get; set; } = new();
        public ResetPasswordViewModel Reset { get; set; } = new();
    }
}
