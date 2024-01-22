using System.Collections.Concurrent;
using Newtonsoft.Json.Linq;
using WikiClientLibrary.Client;
using WikiClientLibrary.Sites;

namespace MissingLabels;

public class Parser
{
    private readonly ConcurrentDictionary<string, (string?, string?, string?)> _labelMap = new();
    private readonly WikiSite _wiki;
    private readonly WikiSite _wikiData;


    public Parser(WikiSite wiki, WikiSite wikiData)
    {
        _wiki = wiki;
        _wikiData = wikiData;
    }

    public async Task<(string? hy, string? en, string? ru)> GetLabels(string q)
    {
        try
        {
            if (_labelMap.ContainsKey(q))
            {
                return _labelMap[q];
            }

            var request = new MediaWikiFormRequestMessage(new Dictionary<string, string>
            {
                { "action", "wbgetentities" },
                { "format", "json" },
                { "ids", q },
                { "props", "labels" },
                { "languages", "hy|en|ru" },
                { "formatversion", "2" }
            });

            var result = await _wikiData.InvokeMediaWikiApiAsync(request, new CancellationToken());
            var labels = result["entities"][q]["labels"];
            _labelMap[q] = (labels["hy"]?["value"].ToString(), labels["en"]?["value"].ToString(),
                labels["ru"]?["value"].ToString());
            return _labelMap[q];
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return (null, null, null);
        }
    }

    public async Task<(List<string>, string)> GetMissingQs(string title)
    {
        var map = await GetQLabelAspectsMap(title);
        var result = new List<string>();

        foreach (var (q, aspects) in map)
        {
            if (!aspects.Contains("L.hy"))
            {
                continue;
            }

            var labels = await GetLabels(q);
            if (labels.hy is null)
            {
                result.Add(q);
            }
        }

        return (result, title);
    }

    private async Task<Dictionary<string, List<string>>> GetQLabelAspectsMap(string title)
    {
        var map = new Dictionary<string, List<string>>();
        try
        {
            var request = new MediaWikiFormRequestMessage(new Dictionary<string, string>
            {
                { "action", "query" },
                { "prop", "wbentityusage" },
                { "wbeulimit", "max" },
                { "formatversion", "2" },
                { "titles", title }
            });

            var result = await _wiki.InvokeMediaWikiApiAsync(request, new CancellationToken());

            var query = result["query"];
            var pages = query["pages"] as JArray;
            var wbentityusage = (JObject)pages[0]["wbentityusage"];

            foreach (KeyValuePair<string, JToken> keyValuePair in wbentityusage)
            {
                try
                {
                    map[keyValuePair.Key] = wbentityusage[keyValuePair.Key]["aspects"].ToObject<List<string>>();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return map;
    }
}