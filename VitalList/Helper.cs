using System.Globalization;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;

namespace VitalList;

public static class Helper
{
    private static Dictionary<string, string>? Titles { get; set; }
    private static Dictionary<string, string>? Signs { get; set; }

    public static Dictionary<string, string> GetTitles()
    {
        using var reader = new StreamReader("վերնագրեր.csv");
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.CurrentCulture)
        {
            Delimiter = ","
        });
        var result = csv.GetRecords<dynamic>().ToList();

        Titles = new Dictionary<string, string>();
        foreach (var test in result)
        {
            Titles[test.English] = test.Armenian;
        }

        return Titles;
    }

    public static string GetTitle(string title)
    {
        if (Titles is null)
        {
            GetTitles();
        }

        title = title.Trim();
        return Titles!.GetValueOrDefault(title, title);
    }

    public static List<TopicPages> GetTopicPagesList()
    {
        using var reader = new StreamReader("ցանկեր.csv");
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.CurrentCulture)
        {
            Delimiter = "\t"
        });
        return csv.GetRecords<TopicPages>().ToList();
    }

    public static Dictionary<string, string> GetSigns(string text)
    {
        var res = new Dictionary<string, string>();
        var lines = text.Split('\n');

        foreach (var line in lines)
        {
            if (line.Length <= 3 || line[0] != '|')
            {
                continue;
            }

            var m = Regex.Match(line, @"^\|\d+.+\[\[:en:([^\]]+)\]\](\|\|[^|]+){4,6}\|\|(.*)$");

            if (m.Success && m.Groups[3].Value.Length > 4)
            {
                res[m.Groups[1].Value.Trim()] = m.Groups[3].Value;
            }
        }

        Signs = res;
        return res;
    }

    public static string GetSign(string title)
    {
        if (Signs is null)
        {
            throw new InvalidOperationException("Please get signs first");
        }

        return Signs.GetValueOrDefault(title, "");
    }

    public static string[] SplitAndKeepSeparator(string input, string separator)
    {
        return input.Split(separator).Select((s, i) => i == 0 ? s : separator + s).ToArray();
    }
}