using System.Collections.Concurrent;
using Utils;
using WikiClientLibrary.Wikibase;
using WikiClientLibrary.Wikibase.DataTypes;
using WikidataDescriptor;

var wikidata = await WikiSiteFactory.GetWikidataSite();

var translationProvider = new TranslationProvider();
await translationProvider.Init();

var rand = new Random();
var tasks = new List<Task>();
var semaphore = new SemaphoreSlim(100, 100);
const int sampleSize = 10000;
const int maxItemId = 125000000;

var itemsToWorkOn = new BlockingCollection<string>(100);

var consumer = Task.Run(async () =>
{
    while (!itemsToWorkOn.IsCompleted)
    {
        try
        {
            var q = itemsToWorkOn.Take();
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
        catch (InvalidOperationException)
        {
        }
    }
});


for (var i = 0; i < sampleSize; i++)
{
    tasks.Add(Task.Run(async () =>
    {
        await semaphore.WaitAsync();
        var id = $"Q{rand.NextInt64(maxItemId)}";
        var (en, hy, _) = await GetEnglishAndArmenianDescriptions(id);
        if (en is not null && hy is null && translationProvider.Translations.Contains(en))
        {
            itemsToWorkOn.Add(id);
        }
        semaphore.Release();
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