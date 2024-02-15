using System.Collections.Concurrent;
using Utils;
using WikiClientLibrary.Generators;
using WikiClientLibrary.Pages;

var hywiki = await WikiSiteFactory.GetWikipediaSite("hy");


var gen = new TranscludedInGenerator(hywiki, "Կաղապար:Ծանցանկ")
{
    NamespaceIds = [0]
};

ConcurrentBag<string> data = [];

List<Task> tasks = [];
int i = 0;
SemaphoreSlim semaphoreSlim = new SemaphoreSlim(100, 100);
await foreach (var item in gen.EnumPagesAsync())
{
    tasks.Add(Task.Run(async () =>
    {
        
        var html = await item.GetParsedContent();
        if (item.Title is not null && item.Exists && !html.Contains("<a href=\"#cite_note"))
        {
            data.Add(item.Title);
            Console.WriteLine(data.Count);
        }

        i++;
        if (i % 1000 == 0)
        {
            Console.WriteLine($"{i} is done");
        }
    }));
}
await Task.WhenAll(tasks);

List<string> dataList = data.ToList();
dataList.Sort();


var chunked = ConvertToChunks(dataList,  2000);
for (int k = 0; k < chunked.Count; k++)
{
    var resultPage = new WikiPage(hywiki, "Վիքիպեդիա:Ցանկեր/ծանցանկ ունեցող հոդվածներ առանց ծանոթագրության/" + (k + 1))
        {
            Content = string.Join('\n', chunked[k].Select(x => $"#[[{x}]]"))
        };
    await resultPage.UpdateContentAsync("");
}


return;

static List<List<string>> ConvertToChunks(List<string> originalList, int chunkSize)
{
    var resultList = new List<List<string>>();

    for (var i = 0; i < originalList.Count; i += chunkSize)
    {
        var chunk = originalList.Skip(i).Take(chunkSize).ToList();
        resultList.Add(chunk);
    }

    return resultList;
}
