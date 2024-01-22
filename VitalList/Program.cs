using System.Text;
using Utils;
using VitalList;
using WikiClientLibrary.Pages;

Console.OutputEncoding = Encoding.UTF8;

var hywiki = await WikiSiteFactory.GetWikipediaSite("hy");
var enwiki = await WikiSiteFactory.GetWikipediaSite("en");
var ruwiki = await WikiSiteFactory.GetWikipediaSite("ru");
var wikidata = await WikiSiteFactory.GetWikidataSite();

var topicPagesList = Helper.GetTopicPagesList();

foreach (var topicPages in topicPagesList)
{
    var enPage = new WikiPage(enwiki, topicPages.En);
    var hyLongPage = new WikiPage(hywiki, topicPages.HyLong);
    var hyMidPage = new WikiPage(hywiki, topicPages.HyMid);
    var hyShortPage = new WikiPage(hywiki, topicPages.HyShort);
    var hyMissingPage = new WikiPage(hywiki, topicPages.HyMissing);

    var cat = $"\n[[Կատեգորիա:Վիքիպեդիա։Կարևորագույն հոդվածների ցանկեր|{topicPages.HyLong.Split('/')[1]}]]";
    var catLong = $"\n[[Կատեգորիա:Վիքիպեդիա։Կարևորագույն երկար հոդվածների ցանկեր|{topicPages.HyLong.Split('/')[1]}]]";
    var catMid = $"\n[[Կատեգորիա:Վիքիպեդիա։Կարևորագույն միջին հոդվածների ցանկեր|{topicPages.HyMid.Split('/')[1]}]]";
    var catShort = $"\n[[Կատեգորիա:Վիքիպեդիա։Կարևորագույն կարճ հոդվածների ցանկեր|{topicPages.HyShort.Split('/')[1]}]]";
    var catMissing =
        $"\n[[Կատեգորիա:Վիքիպեդիա։Կարևորագույն բացակայող հոդվածների ցանկեր|{topicPages.HyMissing.Split('/')[1]}]]";

    await enPage.RefreshAsync(PageQueryOptions.FetchContent);
    await hyLongPage.RefreshAsync(PageQueryOptions.FetchContent);
    await hyMidPage.RefreshAsync(PageQueryOptions.FetchContent);
    await hyShortPage.RefreshAsync(PageQueryOptions.FetchContent);
    await hyMissingPage.RefreshAsync(PageQueryOptions.FetchContent);

    var sections = Helper.SplitAndKeepSeparator(enPage.Content, "\n=");

    Helper.GetSigns($"{hyLongPage.Content}\n{hyMidPage.Content}\n{hyShortPage.Content}\n{hyMissingPage.Content}");

    var hyLongContent =
        $"Ցանկում ներառված են Անգլերեն Վիքիպեդիայի [[:en:{topicPages.En}]] էջի այն հոդվածները, որոնց Հայերեն Վիքիպեդիայի համապատասխան հոդվածը 16000+ բայթ է։\n";
    var hyMidContent =
        $"Ցանկում ներառված են Անգլերեն Վիքիպեդիայի [[:en:{topicPages.En}]] էջի այն հոդվածները, որոնց Հայերեն Վիքիպեդիայի համապատասխան հոդվածը 8000-16000 բայթ է։\n";
    var hyShortContent =
        $"Ցանկում ներառված են Անգլերեն Վիքիպեդիայի [[:en:{topicPages.En}]] էջի այն հոդվածները, որոնց Հայերեն Վիքիպեդիայի համապատասխան հոդվածը 8000- բայթ է։\n";
    var hyMissingContent =
        $"Ցանկում ներառված են Անգլերեն Վիքիպեդիայի [[:en:{topicPages.En}]] էջի այն հոդվածները, որոնք Հայերեն Վիքիպեդիայում չկան։\n";
    foreach (var section in sections)
    {
        var result = await SectionConverter.GetSectionPerCategory(section, wikidata, enwiki, ruwiki, hywiki);
        hyLongContent += result.Long;
        hyMidContent += result.Mid;
        hyShortContent += result.Short;
        hyMissingContent += result.Missing;
    }

    hyLongPage.Content = $"{hyLongContent}\n{cat}{catLong}";
    hyMidPage.Content = $"{hyMidContent}\n{cat}{catMid}";
    hyShortPage.Content = $"{hyShortContent}\n{cat}{catShort}";
    hyMissingPage.Content = $"{hyMissingContent}\n{cat}{catMissing}";
    try
    {
        await hyLongPage.UpdateContentAsync("թարմացում");
    }
    catch (Exception e)
    {
        var fileName = Path.GetInvalidFileNameChars()
            .Aggregate(topicPages.HyLong, (current, c) => current.Replace(c, '_'));
        File.WriteAllText(fileName, hyLongPage.Content);
    }

    try
    {
        await hyMidPage.UpdateContentAsync("թարմացում");
    }
    catch (Exception e)
    {
        var fileName = Path.GetInvalidFileNameChars()
            .Aggregate(topicPages.HyMid, (current, c) => current.Replace(c, '_'));
        File.WriteAllText(fileName, hyMidPage.Content);
    }

    try
    {
        await hyShortPage.UpdateContentAsync("թարմացում");
    }
    catch (Exception e)
    {
        var fileName = Path.GetInvalidFileNameChars()
            .Aggregate(topicPages.HyShort, (current, c) => current.Replace(c, '_'));
        File.WriteAllText(fileName, hyShortPage.Content);
    }

    try
    {
        await hyMissingPage.UpdateContentAsync("թարմացում");
    }
    catch (Exception e)
    {
        var fileName = Path.GetInvalidFileNameChars()
            .Aggregate(topicPages.HyMissing, (current, c) => current.Replace(c, '_'));
        File.WriteAllText(fileName, hyMissingPage.Content);
    }
}