using System.ComponentModel;
using System.Reflection;

namespace InsuranceBot.Exentions;

public static class EnumExtension
{
    // Returns the Description attribute of an Enum value, or the Enum 
    public static string GetDescription(this Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        return field?.GetCustomAttribute<DescriptionAttribute>()?.Description ?? value.ToString();
    }
}
