using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Utils;
using WikiClientLibrary.Pages;
using WikiClientLibrary.Sites;
using WikiClientLibrary.Wikibase;

namespace VitalList;

public static class SectionConverter
{
    public static async Task<(string Long, string Mid, string Short, string Missing)> GetSectionPerCategory(
        string enSection,
        WikiSite wikidata,
        WikiSite enwiki,
        WikiSite ruwiki,
        WikiSite hywiki)
    {
        var longSection = "";
        var midSection = "";
        var shortSection = "";
        var missingSection = "";
        var march = Regex.Match(enSection, "^(=+) *([^=]+) *=+\n", RegexOptions.Multiline);
        var title = march.Groups[2].Value;
        var sectionRank = march.Groups[1].Value;
        if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(sectionRank))
        {
            return ("", "", "", "");
        }

        var lines = enSection.Trim().Split('\n');

        ConcurrentBag<List<object>> longTable = [];
        ConcurrentBag<List<object>> midTable = [];
        ConcurrentBag<List<object>> shortTable = [];
        ConcurrentBag<List<object>> missingTable = [];

        await ProcessPages(wikidata, enwiki, ruwiki, hywiki, lines, missingTable, shortTable, midTable, longTable);
        var longTableList = longTable.ToList();
        var midTableList = midTable.ToList();
        var shortTableList = shortTable.ToList();
        var missingTableList = missingTable.ToList();
        longTableList.Sort((l1, l2) => string.Compare(l1[1].ToString(), l2[1].ToString(), StringComparison.Ordinal));
        midTableList.Sort((l1, l2) => string.Compare(l1[1].ToString(), l2[1].ToString(), StringComparison.Ordinal));
        shortTableList.Sort((l1, l2) => string.Compare(l1[1].ToString(), l2[1].ToString(), StringComparison.Ordinal));
        missingTableList.Sort((l1, l2) => string.Compare(l1[1].ToString(), l2[1].ToString(), StringComparison.Ordinal));

        List<string> existsHeader =
        [
            "#",
            "{{Tooltip|Ա․ կ․|Անգլերեն կարգավիճակ}}",
            "Անգլերեն հոդված",
            "Անգլերեն չափ",
            "Ռուսերեն հոդված",
            "Ռուսերեն չափ",
            "Դատա",
            "Հայերեն հոդված",
            "Հայերեն չափ",
            "Ստորագրություն"
        ];
        longSection += $"{sectionRank} {Helper.GetTitle(title)} {sectionRank}\n";
        longSection += WikitextUtils.ToWikiTable(longTableList, existsHeader, true);
        midSection += $"{sectionRank} {Helper.GetTitle(title)} {sectionRank}\n";
        midSection += WikitextUtils.ToWikiTable(midTableList, existsHeader, true);
        shortSection += $"{sectionRank} {Helper.GetTitle(title)} {sectionRank}\n";
        shortSection += WikitextUtils.ToWikiTable(shortTableList, existsHeader, true);
        missingSection += $"{sectionRank} {Helper.GetTitle(title)} {sectionRank}\n";
        missingSection += WikitextUtils.ToWikiTable(missingTableList, [
            "#",
            "{{Tooltip|Ա․ կ․|Անգլերեն կարգավիճակ}}",
            "Անգլերեն հոդված",
            "Անգլերեն չափ",
            "Ռուսերեն հոդված",
            "Ռուսերեն չափ",
            "Դատա",
            "Հայերեն հոդված",
            "Ստորագրություն"
        ], true);

        return (longSection, midSection, shortSection, missingSection);
    }

    private static async Task ProcessPages(
        WikiSite wikidata, WikiSite enwiki, WikiSite ruwiki, WikiSite hywiki,
        string[] lines,
        ConcurrentBag<List<object>> missingTable, ConcurrentBag<List<object>> shortTable,
        ConcurrentBag<List<object>> midTable, ConcurrentBag<List<object>> longTable)
    {
        List<Task> tasks = new();
        foreach (var line in lines)
        {
            tasks.Add(Task.Run(async () =>
            {
                var entitle = Regex.Match(line, @"\[\[:?([^\]|]+)\|?[^\]]*\]\]").Groups[1].Value;
                var icon = Regex.Match(line, @"({{[Ic]con\|.+?}})").Groups[1].Value;
                if (string.IsNullOrEmpty(entitle) || string.IsNullOrEmpty(icon) || entitle.StartsWith("Category:") ||
                    entitle.StartsWith("Wikipedia:"))
                {
                    return;
                }

                var pages = await GetPages(entitle, wikidata, enwiki, ruwiki, hywiki);
                var row = new List<object>
                {
                    icon,
                    pages.En.Title,
                    pages.En.LengthStr,
                    pages.Ru.Title,
                    pages.Ru.LengthStr,
                    pages.Q,
                    pages.Hy.Title,
                    pages.Hy.LengthStr,
                    Helper.GetSign(entitle)
                };
                var logOutput = $"En: {pages.En.Title}" +
                                (!string.IsNullOrEmpty(pages.Ru.Title) ? $", Ru: {pages.Ru.Title}" : "") +
                                (!string.IsNullOrEmpty(pages.Hy.Title) ? $", Hy: {pages.Hy.Title}" : "");
                switch (pages.Hy.Length)
                {
                    case 0:
                        row.RemoveAt(8);
                        missingTable.Add(row);
                        break;
                    case <= 8000:
                        shortTable.Add(row);
                        break;
                    case > 8000 and <= 16000:
                        midTable.Add(row);
                        break;
                    case > 16000:
                        longTable.Add(row);
                        break;
                }
                Console.WriteLine(logOutput);
            }));
        }

        await Task.WhenAll(tasks);
    }

    private static async Task<(PageResult En, PageResult Ru, PageResult Hy, string Q)> GetPages(
        string entitle,
        WikiSite wikidata,
        WikiSite enwiki,
        WikiSite ruwiki,
        WikiSite hywiki)
    {
        (PageResult En, PageResult Ru, PageResult Hy, string Q) result = (new PageResult($"[[:en:{entitle}]]"),
            new PageResult(),
            new PageResult(), "");
        var enPage = new WikiPage(enwiki, entitle);
        await enPage.RefreshAsync(PageQueryOptions.None);
        if (!enPage.Exists)
        {
            return result;
        }

        result.En.Length = enPage.ContentLength;
        var q = await enPage.GetQ();
        if (q is null)
        {
            return result;
        }

        result.Q = $"[[:d:{q}]]";
        var item = new Entity(wikidata, q);
        var ruPage = await item.ConvertTo(ruwiki);
        if (ruPage?.Title is not null)
        {
            await ruPage.RefreshAsync(PageQueryOptions.None);
            result.Ru.Title = $"[[:ru:{ruPage.Title}]]";
            result.Ru.Length = ruPage.ContentLength;
        }

        var hyPage = await item.ConvertTo(hywiki);
        if (hyPage?.Title is not null)
        {
            await hyPage.RefreshAsync(PageQueryOptions.None);
            result.Hy.Title = $"[[{hyPage.Title}]]";
            result.Hy.Length = hyPage.ContentLength;
        }

        return result;
    }
}

public class PageResult(string title = "", int length = 0)
{
    public string Title { get; set; } = title;
    public int Length { get; set; } = length;
    public string LengthStr => Length == 0 ? string.Empty : Length.ToString();
}