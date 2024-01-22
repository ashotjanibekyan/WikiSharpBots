using System.Collections.Concurrent;
using MissingLabels;
using Utils;
using WikiClientLibrary.Generators;
using WikiClientLibrary.Pages;

var hywiki = await WikiSiteFactory.GetWikipediaSite("hy");
var wikidata = await WikiSiteFactory.GetWikidataSite();

var parser = new Parser(hywiki, wikidata);

var gen = new CategoryMembersGenerator(hywiki,
    "Կատեգորիա:Վիքիպեդիա:Վիքիդատայի հայերեն չթարգմանված տարրեր պարունակող հոդվածներ");

var freqDict = new ConcurrentDictionary<string, int>();
var tasks = new List<Task>();

await foreach (var page in gen.EnumPagesAsync())
{
    var task = parser.GetMissingQs(page.Title!);
    tasks.Add(task);
    task.ContinueWith(tsk =>
    {
        var qs = tsk.Result.Item1;
        foreach (var q in qs)
        {
            if (freqDict.ContainsKey(q))
            {
                freqDict[q] += 1;
            }
            else
            {
                freqDict[q] = 1;
            }
        }
    });
}

await Task.WhenAll(tasks);

var sorted = freqDict.Where(kvp => kvp.Value > 2)
    .Select(kvp => (kvp.Value, kvp.Key))
    .OrderByDescending(t => (t.Value, t.Key))
    .Select(kvp =>
    {
        var labels = parser.GetLabels(kvp.Key).Result; // we are fine since all values should be in cache at this point
        return new List<object> { $"[[d:{kvp.Key}]]", kvp.Value, labels.en ?? string.Empty, labels.ru ?? string.Empty };
    }).ToList();

var destPage = new WikiPage(hywiki, "Մասնակից:ԱշոտՏՆՂ/ցանկեր/շատ_օգտագործվող_չթարգմանված_տարրեր")
{
    Content = WikitextUtils.ToWikiTable(sorted, ["Տարր", "Օգտագործման քանակ", "Անգլերեն պիտակ", "Ռուսերեն պիտակ"])
};
await destPage.UpdateContentAsync("թարմացում");