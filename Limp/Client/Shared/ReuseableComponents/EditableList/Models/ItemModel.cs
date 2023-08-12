namespace Limp.Client.Shared.ReuseableComponents.EditableList.Models
{
    public class ItemModel
    {
        public string ItemName { get; set; } = "N/A";
        public bool IsActive { get; set; } = false;
        public bool IsEncryptionSettedUp { get; set; } = false;
        public BadgeModel BadgeModel { get; set; } = new();
        public bool ShowBadge { get; set; } = false;
    }
}
