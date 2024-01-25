using System.Text.RegularExpressions;

namespace Utils;

public sealed class TranslationRecord
{
    public Dictionary<string, string> Text { get; set; }

    public Dictionary<string, string> Regex
    {
        get => _regex;
        set
        {
            _regex = value;
            _compiledRegexes.Clear();
            foreach (var r in _regex)
            {
                _compiledRegexes.Add(r.Key, new Regex(r.Key));
            }
        }
    }

    private readonly Dictionary<string, Regex> _compiledRegexes = new();
    private Dictionary<string, string> _regex;

    public string? GetTranslation(string str)
    {
        var plainTrans = GetStr(str);
        if (plainTrans is not null)
        {
            return plainTrans;
        }

        var regexTrans = GetRegex(str);
        if (regexTrans.Item1 is not null && regexTrans.Item2 is not null)
        {
            return regexTrans.Item1.Replace(str, regexTrans.Item2.Replace(@"\", "$"));
        }

        return null;
    }

    private (Regex?, string?) GetRegex(string str)
    {
        foreach (var r in _compiledRegexes.Where(r => r.Value.IsMatch(str)))
        {
            return (r.Value, _regex[r.Key]);
        }

        return (null, null);
    }

    private string? GetStr(string str)
    {
        return Text.GetValueOrDefault(str);
    }
    
    public bool Contains(string str)
    {
        return Text.ContainsKey(str) || Regex.Select(kvp => new Regex(kvp.Key)).Any(regex => regex.IsMatch(str));
    }
}