using System.ComponentModel.DataAnnotations;

namespace SwiftSpecBuild.Models.Authentication
{
    public class ConfirmViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        public string Code { get; set; }
    }
}
