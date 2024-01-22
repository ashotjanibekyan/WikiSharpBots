using System.Text.RegularExpressions;

namespace CommonEnglishDescriptions;

internal class TranslationRecord
{
    public Dictionary<string, string> Text { get; set; }
    public Dictionary<string, string> Regex { get; set; }

    public bool Contains(string str)
    {
        return Text.ContainsKey(str) || Regex.Select(kvp => new Regex(kvp.Key)).Any(regex => regex.IsMatch(str));
    }
}