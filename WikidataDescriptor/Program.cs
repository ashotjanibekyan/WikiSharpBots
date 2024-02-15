using System.Collections.Concurrent;
using Utils;
using WikiClientLibrary.Wikibase;
using WikiClientLibrary.Wikibase.DataTypes;
using WikidataDescriptor;

var wikidata = await WikiSiteFactory.GetWikidataSite();

var translationProvider = new TranslationProvider();
await translationProvider.Init();

var rand = new Random();
const int maxItemId = 126000000;

var itemsToWorkOn = new BlockingCollection<string>(100);
var tasks = new List<Task>();

var consumer = Task.Run(async () =>
{
    while (!itemsToWorkOn.IsCompleted)
    {
        try
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var q = itemsToWorkOn.Take();
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            Console.WriteLine(elapsedMs);
            var (en, hy, entity) = await GetEnglishAndArmenianDescriptions(q);
            if (en is null || hy is not null)
            {
                continue;
            }

            var translation = translationProvider.Translations.GetTranslation(en);
            if (translation is not null)
            {
                await entity.EditAsync([new EntityEditEntry(nameof(Entity.Descriptions), new WbMonolingualText("hy", translation))], "per [[:hy:User:ԱշոտՏՆՂ/wikidataDescriptions.json]]");
            }

        }
        catch (Exception ex)
        {
        }
    }
});


for (var i = 0; i < Environment.ProcessorCount; i++)
{
    tasks.Add(Task.Run(async () =>
    {
        while (true)
        {
            var id = $"Q{rand.NextInt64(maxItemId)}";
            var (en, hy, _) = await GetEnglishAndArmenianDescriptions(id);
            if (en is not null && hy is null && translationProvider.Translations.Contains(en))
            {
                itemsToWorkOn.Add(id);
            }
        }
    }));
}

await Task.WhenAll(tasks);
itemsToWorkOn.CompleteAdding();
await Task.WhenAll(consumer);

async Task<(string? En, string? Hy, Entity? entity)> GetEnglishAndArmenianDescriptions(string q)
{
    try
    {
        var entity = new Entity(wikidata, q);
        await entity.RefreshAsync(EntityQueryOptions.None);
        if (entity.Exists)
        {
            await entity.RefreshAsync(EntityQueryOptions.FetchDescriptions);
            var en = entity.Descriptions.ContainsLanguage("en") ? entity.Descriptions["en"] : null;
            var hy = entity.Descriptions.ContainsLanguage("hy") ? entity.Descriptions["hy"] : null;

            return (en, hy, entity);
        }
    }
    catch (Exception e)
    {
        return (null, null, null);
    }
    return (null, null, null);
}