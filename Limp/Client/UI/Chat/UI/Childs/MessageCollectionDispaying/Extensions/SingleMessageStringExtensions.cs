using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;

namespace Ethachat.Client.UI.Chat.UI.Childs.MessageCollectionDispaying.Extensions
{
    public static class SingleMessageStringExtensions
    {
        public static MarkupString AsMarkupString(this string text)
        {
            return new MarkupString($"<span>{text}</span>");
        }

        public static MarkupString FormatLinks(this MarkupString markupString)
        {
            var pattern = @"(?:http|https)://[^\s""]+";

            Regex regex = new Regex(pattern);

            string formattedText = regex.Replace(markupString.Value, match =>
            {
                string url = match.Value;
                return $"<a href=\"{url}\" target=\"_blank\" style=\"text-decoration: none;\">{url}</a>";
            });

            return new MarkupString(formattedText);
        }
    }
}