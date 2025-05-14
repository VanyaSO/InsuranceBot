using System.Text.RegularExpressions;

namespace InsuranceBot.Exentions;


public static class StringExtension
{
    private static readonly Regex EmojiRegex = new Regex(@"^[\p{Cs}\p{So}\p{Sm}\u200B\uD83C-\uDBFF\uDC00-\uDFFF]", RegexOptions.Compiled);

    // Determines whether the string starts with an emoji or special symbol character
    public static bool StartsWithEmoji(this string text) => EmojiRegex.IsMatch(text);
}