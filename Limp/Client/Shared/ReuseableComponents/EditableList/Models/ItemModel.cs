using BlazorBootstrap;

namespace Ethachat.Client.Shared.ReuseableComponents.EditableList.Models
{
    public class ItemModel
    {
        public Guid Id { get; set; }
        public string ItemName { get; set; } = "N/A";
        public bool IsActive { get; set; }
        public bool IsEncryptionSettedUp { get; set; }
        public BadgeModel BadgeModel { get; set; } = new();
        public bool ShowBadge { get; set; }
        public Icon[] Icons { get; set; } = Array.Empty<Icon>();
    }
}
