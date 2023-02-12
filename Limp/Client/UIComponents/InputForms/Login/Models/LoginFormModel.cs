using System.ComponentModel.DataAnnotations;

namespace Limp.Client.UIComponents.InputForms.Login.Models
{
    public class LoginFormModel
    {
        [StringLength(30, MinimumLength = 3, ErrorMessage = $"{nameof(Username)} length should be in range of [3;30]")]
        [Required]
        public string Username { get; set; } = string.Empty;
        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
