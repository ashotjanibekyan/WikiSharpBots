using Newtonsoft.Json;
using Utils;
using WikiClientLibrary.Pages;
using WikiClientLibrary.Sites;

namespace WikidataDescriptor;

public sealed class TranslationProvider
{
    public TranslationRecord Translations { get; set; }

    private Task? _updater;
    private WikiSite _site;
    
    public TranslationProvider()
    {
        _updater = Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromHours(1));
                    var translationPage = new WikiPage(_site, "Մասնակից:ԱշոտՏՆՂ/wikidataDescriptions.json");
                    await translationPage.RefreshAsync(PageQueryOptions.FetchContent);
                    Translations = JsonConvert.DeserializeObject<TranslationRecord>(translationPage.Content);
                }
                catch (Exception e)
                {
                    Task.Delay(TimeSpan.FromMinutes(10));
                }
            }
        });
    }

    public async Task Init()
    {
        _site = await WikiSiteFactory.GetWikipediaSite("hy");
        var translationPage = new WikiPage(_site, "Մասնակից:ԱշոտՏՆՂ/wikidataDescriptions.json");
        await translationPage.RefreshAsync(PageQueryOptions.FetchContent);
        Translations = JsonConvert.DeserializeObject<TranslationRecord>(translationPage.Content);
    }
}
