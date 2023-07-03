namespace Limp.Client.UIComponents.InputForms.Registration.Models
{
    public record CheckedInput
    {
        public CheckedInput
            (string name,
            Action<string, CheckedInput>? onValueSet = null)
        {
            Name = name;
            OnValueSet = onValueSet;
        }

        /// <summary>
        /// Triggers a manual validation against given string value
        /// </summary>
        /// <param name="reference"></param>
        public void ValidateAgainst(CheckedInput reference)
        {
            IsTouched = true;
            ErrorMessages.Clear();
            if (Value != reference.Value)
            {
                IsLastValidationSucceeded = false;
                ErrorMessages.Add("Password not matches with Password Confirmation.");
            }
        }

        /// <summary>
        /// Delegate that is being called in setter method for checked input instance value
        /// </summary>
        private Action<string, CheckedInput>? OnValueSet { get; init; }
        private string _value { get; set; } = string.Empty;
        public string Value
        {
            get => _value;
            set
            {
                _value = value;
                IsTouched = true;
                if (OnValueSet is not null)
                    OnValueSet(value, this);
            }
        }
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// Indicates whether user typed anything in
        /// </summary>
        public bool IsTouched { get; set; } = false;
        public bool IsLastValidationSucceeded { get; set; } = true;
        public bool IsValid => IsTouched && IsLastValidationSucceeded;
        public List<string> ErrorMessages { get; set; } = new();
    }
}
