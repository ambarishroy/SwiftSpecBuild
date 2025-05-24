using System.ComponentModel.DataAnnotations;

namespace SwiftSpecBuild.Models
{
    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
