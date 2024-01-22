using System.Collections.Concurrent;
using CommonEnglishDescriptions;
using Newtonsoft.Json;
using Utils;
using WikiClientLibrary.Pages;
using WikiClientLibrary.Wikibase;

const int minUsage = 10;
const int sampleSize = 10000;
const int maxItemId = 125000000;

var hywiki = await WikiSiteFactory.GetWikipediaSite("hy");
var wikidata = await WikiSiteFactory.GetWikidataSite();

var translationPage = new WikiPage(hywiki, "Մասնակից:ԱշոտՏՆՂ/wikidataDescriptions.json");
await translationPage.RefreshAsync(PageQueryOptions.FetchContent);

var translations = JsonConvert.DeserializeObject<TranslationRecord>(translationPage.Content);

var rand = new Random();
var data = new ConcurrentDictionary<string, int>();

var tasks = new List<Task>();
var semaphore = new SemaphoreSlim(100, 100);

for (var i = 0; i < sampleSize; i++)
{
    tasks.Add(Task.Run(async () =>
    {
        try
        {
            await semaphore.WaitAsync();
            var id = $"Q{rand.NextInt64(maxItemId)}";
            var entity = new Entity(wikidata, id);
            await entity.RefreshAsync(EntityQueryOptions.None);
            if (entity.Exists)
            {
                await entity.RefreshAsync(EntityQueryOptions.FetchDescriptions);

                if (entity.Descriptions.ContainsLanguage("en") && !entity.Descriptions.ContainsLanguage("hy"))
                {
                    var enDescription = entity.Descriptions["en"];

                    if (enDescription is null || translations.Contains(enDescription)) return;
                    if (!data.TryAdd(enDescription, 1)) data[enDescription] += 1;
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        finally
        {
            semaphore.Release();
        }
    }));
}

await Task.WhenAll(tasks);

var sorted = data.Where(kvp => kvp.Value > minUsage)
    .Select(kvp => new List<object>{kvp.Key, kvp.Value})
    .OrderByDescending(t => (t[1], t[0]))
    .ToList();

var destPage = new WikiPage(wikidata, "User:ԱշոտՏՆՂ/descriptions")
{
    Content = WikitextUtils.ToWikiTable(sorted, ["Նկարագրություն", "Քանակ"])
};
await destPage.UpdateContentAsync("թարմացում");