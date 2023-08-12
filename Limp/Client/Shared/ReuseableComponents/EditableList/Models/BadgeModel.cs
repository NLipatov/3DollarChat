namespace Limp.Client.Shared.ReuseableComponents.EditableList.Models
{
    public record BadgeModel
    {
        public string Text { get; set; } = string.Empty;
        public string InlineStyles { get; set; } = string.Empty;
        public string Classes { get; set; } = string.Empty;
    }
}
