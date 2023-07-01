﻿using System.Text.RegularExpressions;

namespace Limp.Client.UIComponents.InputForms.Registration.Models
{
    public class RegisterFormModel
    {
        public CheckedInput Username = new("Username", ValidateUsername);
        public CheckedInput Password = new("Password", ValidatePassword);
        public CheckedInput PasswordConfirmation = new("PasswordConfirmation");

        #region Property Value Validation
        private static void ValidateUsername(string value, CheckedInput checkedInput)
        {
            checkedInput.IsTouched = true;
            checkedInput.ErrorMessages.Clear();
            if (value == "You")
            {
                checkedInput.IsLastValidationSucceeded = false;
                checkedInput.ErrorMessages.Add("You cant use 'You' as your username.");
            }
            else if (!Regex.IsMatch(value, @"^[a-zA-Z0-9]+$"))
            {
                checkedInput.IsLastValidationSucceeded = false;
                checkedInput.ErrorMessages.Add("Only english alhabet letters and arabic numbers are allowed.");
            }
            else if (value.Length < 3)
            {
                checkedInput.IsLastValidationSucceeded = false;
                checkedInput.ErrorMessages.Add("Your username should have atleast 3 characters.");
            }
            else
            {
                checkedInput.IsLastValidationSucceeded = true;
            }
        }

        private static void ValidatePassword(string value, CheckedInput checkedInput)
        {
            checkedInput.IsTouched = true;
            checkedInput.ErrorMessages.Clear();
            if (value.Length < 16)
            {
                checkedInput.IsLastValidationSucceeded = false;
                checkedInput.ErrorMessages.Add("Password should be atleast 16 characters long.");
            }
            else if (!(value.Any(char.IsUpper) && value.Any(char.IsLower)))
            {
                checkedInput.IsLastValidationSucceeded = false;
                checkedInput.ErrorMessages.Add("Password should contain lower and upper case characters.");
            }
            else if (!Regex.IsMatch(value, @"[0-9]"))
            {
                checkedInput.IsLastValidationSucceeded = false;
                checkedInput.ErrorMessages.Add("Password should contain atleast 1 arabic number");
            }
            else if (!Regex.IsMatch(value, @"[!@#$%^&*()_+=\[\]{};':\""<>,.?\\/|]"))
            {
                checkedInput.IsLastValidationSucceeded = false;
                checkedInput.ErrorMessages.Add("Password should contain atleast 1 special character.");
            }
            else
            {
                checkedInput.IsLastValidationSucceeded = true;
            }
        }
        #endregion
    }
}