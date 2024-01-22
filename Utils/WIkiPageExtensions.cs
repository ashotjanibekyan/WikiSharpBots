using WikiClientLibrary.Client;
using WikiClientLibrary.Pages;
using WikiClientLibrary.Sites;
using WikiClientLibrary.Wikibase;

namespace Utils;

public static class WIkiPageExtensions
{
    public static async Task<string?> GetQ(this WikiPage wikiPage)
    {
        try
        {
            var request = new MediaWikiFormRequestMessage(new Dictionary<string, string>
            {
                { "action", "query" },
                { "prop", "pageprops" },
                { "formatversion", "2" },
                { "titles", wikiPage.Title }
            });
            var result = await wikiPage.Site.InvokeMediaWikiApiAsync(request, new CancellationToken());
            return result["query"]["pages"][0]["pageprops"]["wikibase_item"].ToString();
        }
        catch (Exception e)
        {
            return null;
        }
    }

    public static async Task<WikiPage?> ConvertTo(this Entity item, WikiSite site)
    {
        await item.RefreshAsync(EntityQueryOptions.FetchSiteLinks);
        var wikiId = site.SiteInfo.ExtensionData["wikiid"].ToString();
        return item.SiteLinks.ContainsKey(wikiId) ? new WikiPage(site, item.SiteLinks[wikiId].Title) : null;
    }
}