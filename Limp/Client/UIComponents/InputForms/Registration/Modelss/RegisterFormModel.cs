using System.ComponentModel.DataAnnotations;

namespace Limp.Client.UIComponents.InputForms.Registration.Models
{
    public class RegisterFormModel
    {
        [StringLength(30, MinimumLength = 3, ErrorMessage = $"{nameof(Username)} length should be in range of [3;30]")]
        public string Username { get; set; }

        [RegularExpression(@"^(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])(?=.*?[#?!@$%^&*-]).{24,}$", ErrorMessage = "Uppercase and lowecase letter, extra symbol, number and legth more than 24 characters is required.")]
        public string Password { get; set; }

        [Compare("Password", ErrorMessage = $"{nameof(PasswordConfirmation)} should match {nameof(Password)}")]
        public string PasswordConfirmation { get; set; }
    }
}