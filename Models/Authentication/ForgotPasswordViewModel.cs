﻿using System.ComponentModel.DataAnnotations;

namespace SwiftSpecBuild.Models.Authentication
{
    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
