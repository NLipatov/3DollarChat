using System.ComponentModel.DataAnnotations;

namespace Limp.Client.Pages.Registration.Models
{
    public class Credentials
    {
        [StringLength(30, MinimumLength = 3, ErrorMessage = $"Length should be in range of [3;30]")]
        [Required(ErrorMessage = "Required")]
        public string Username { get; set; }

        [RegularExpression(@"^(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])(?=.*?[#?!@$%^&*-]).{24,}$", ErrorMessage = "Uppercase and lowecase letter, extra symbol, number and legth more than 24 characters is required.")]
        [Required(ErrorMessage = "Required")]
        public string Password { get; set; }

        [Compare("Password", ErrorMessage = $"{nameof(PasswordConfirmation)} Should match {nameof(Password)}")]
        public string PasswordConfirmation { get; set; }
    }
}
