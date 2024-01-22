using WikiClientLibrary.Client;
using WikiClientLibrary.Sites;

namespace Utils;

public static class WikiSiteFactory
{
    private static (string Username, string Password)? _usernamePassword;

    private static (string Username, string Password) UsernamePassword
    {
        get
        {
            _usernamePassword ??= CredentialHelper.GetCredentials();
            return _usernamePassword.Value;
        }
    }
    
    public static async Task<WikiSite> GetWikipediaSite(string lang)
    {
        var site = new WikiSite(new WikiClient(), new SiteOptions
        {
            ApiEndpoint = $"https://{lang}.wikipedia.org/w/api.php"
        }, UsernamePassword.Username, UsernamePassword.Password);

        await site.Initialization;
        return site;
    }

    public static async Task<WikiSite> GetWikidataSite()
    {
        var site = new WikiSite(new WikiClient(), new SiteOptions
        {
            ApiEndpoint = $"https://www.wikidata.org/w/api.php"
        }, UsernamePassword.Username, UsernamePassword.Password);

        await site.Initialization;
        return site;
    }
}