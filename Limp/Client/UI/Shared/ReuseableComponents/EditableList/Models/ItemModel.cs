using Ethachat.Client.UI.Chat.UI.Childs.ChatHeader.Indicators;
using Ethachat.Client.UI.Shared.Icon;

namespace Ethachat.Client.UI.Shared.ReuseableComponents.EditableList.Models
{
    public class ItemModel
    {
        public Guid Id { get; set; }
        public string ItemName { get; set; } = "N/A";
        public bool IsEncryptionSettedUp { get; set; }
        public BadgeModel BadgeModel { get; set; } = new();
        public bool ShowBadge { get; set; }
        public CustomIcon[] Icons { get; set; } = [];
        public AvatarOnlineIndicator? AvatarOnlineIndicator { get; set; }
    }
}
