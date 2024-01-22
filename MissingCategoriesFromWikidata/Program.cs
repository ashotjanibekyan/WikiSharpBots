using System.Collections.Concurrent;
using Utils;
using WikiClientLibrary.Generators;
using WikiClientLibrary.Pages;
using WikiClientLibrary.Wikibase;

var vatItemMap = new Dictionary<string, string>
{
    { "P53", "P910" },
    { "P54", "P6112" },
    { "P59", "P910" },
    { "P69", "P3876" },
    { "P102", "P6365" },
    { "P108", "P4195" },
    { "P412", "P910" },
    { "P413", "P910" },
    { "P881", "P910" },
    { "P915", "P1740" },
    { "P4614", "P1200" }
};

var hywiki = await WikiSiteFactory.GetWikipediaSite("hy");
var wikidata = await WikiSiteFactory.GetWikidataSite();

var gen = new CategoryMembersGenerator(hywiki, "Կատեգորիա:Վիքիդատա։Կատեգորիայի կարիք ունեցող հոդվածներ");

var data = new ConcurrentDictionary<string, int>();
var tasks = new List<Task>();

await foreach (var page in gen.EnumPagesAsync())
{
    tasks.Add(Task.Run(async () =>
    {
        try
        {
            var q = await page.GetQ();
            var entity = new Entity(wikidata, q);
            await entity.RefreshAsync(EntityQueryOptions.FetchClaims);
            foreach (var kvp in vatItemMap)
            {
                if (entity.Claims.ContainsKey(kvp.Key))
                {
                    var claims = entity.Claims[kvp.Key];
                    foreach (var claim in claims)
                    {
                        var val = new Entity(wikidata, claim.MainSnak.DataValue.ToString());
                        await val.RefreshAsync(EntityQueryOptions.FetchClaims);
                        var catQ = val.Claims[kvp.Value].FirstOrDefault();
                        if (catQ is null) continue;
                        var catItem = new Entity(wikidata, catQ.MainSnak.DataValue.ToString());
                        await catItem.RefreshAsync(EntityQueryOptions.FetchSiteLinks);
                        if (catItem.SiteLinks.ContainsKey("hywiki")) continue;
                        await catItem.RefreshAsync(EntityQueryOptions.FetchLabels);
                        var key = catItem.Id!;
                        if (catItem.Labels.ContainsLanguage("en"))
                        {
                            key = catItem.Labels["en"];
                        }

                        if (data.ContainsKey(key))
                        {
                            data[key] += 1;
                        }
                        else
                        {
                            data[key] = 1;
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }));
}

await Task.WhenAll(tasks);

var sorted = data.Select(kvp => new List<object>{kvp.Key, kvp.Value})
    .OrderByDescending(t => (t[1], t[0]))
    .ToList();

var destPage = new WikiPage(hywiki, "Մասնակից:ԱշոտՏՆՂ/ցանկեր/շատ օգտագործվող չթարգմանված տարրեր/կատեգորիա")
{
    Content = WikitextUtils.ToWikiTable(sorted, ["Տարր", "Օգտագործման քանակ"])
};
await destPage.UpdateContentAsync("թարմացում");