namespace Limp.Client.UIComponents.InputForms.Registration.Models
{
    public class RegisterFormModel
    {
        public CheckedInput Username { get; set; } = new("Username");
        public CheckedInput Password { get; set; } = new("Password");
        public CheckedInput PasswordConfirmation { get; set; } = new("PasswordConfirmation");
    }
}