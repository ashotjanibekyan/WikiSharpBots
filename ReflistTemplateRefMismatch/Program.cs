using System.Collections.Concurrent;
using Utils;
using WikiClientLibrary.Generators;

var hywiki = await WikiSiteFactory.GetWikipediaSite("hy");


var gen = new TranscludedInGenerator(hywiki, "Կաղապար:Ծանցանկ")
{
    NamespaceIds = [0]
};

ConcurrentBag<string> data = [];

List<Task> tasks = [];
int i = 0;
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

File.WriteAllLines("result.txt", dataList.Select(i => $"#[[{i}]]"));