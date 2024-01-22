using System.Text;

namespace Utils;

public static class WikitextUtils
{
    public static string ToWikiTable(List<List<object>> data, List<string> headers, bool addIndex = false)
    {
        if (data.Count == 0) return string.Empty;
        StringBuilder sb = new();
        sb.AppendLine("""
                      {| class="wikitable sortable"
                      """);
        sb.AppendLine("!" + string.Join("!!", headers));
        var index = 0;
        foreach (var row in data)
        {
            sb.AppendLine("|-");
            if (addIndex)
            {
                index++;
                sb.AppendLine($"|{index}||{string.Join("||", row)}");
            }
            else
            {
                sb.AppendLine("|" + string.Join("||", row));
            }
        }

        sb.AppendLine("|}");
        return sb.ToString();
    }
}