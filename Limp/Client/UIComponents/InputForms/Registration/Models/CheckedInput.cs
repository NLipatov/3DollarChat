namespace Limp.Client.UIComponents.InputForms.Registration.Models
{
    public record CheckedInput
    {
        public CheckedInput(string name)
        {
            Name = name;
        }
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public bool IsTouched { get; set; } = false;
        public bool IsValid { get; set; } = true;
        public List<string> ErrorMessages { get; set; } = new();
    }
}
