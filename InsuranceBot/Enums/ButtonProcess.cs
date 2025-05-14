using System.ComponentModel;

namespace InsuranceBot.Enums;

// Buttons for display
public enum ButtonProcess
{
    [Description("✍️Start")]
    Start,
    [Description("🏁Сancel processing")]
    Cancel,
    [Description("❌No")]
    No,
    [Description("✅Yes")]
    Yes
}