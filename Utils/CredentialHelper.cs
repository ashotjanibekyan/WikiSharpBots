using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace Utils;

public static class CredentialHelper
{
    public static (string Username, string Password) GetCredentials()
    {
        using var reader = new StreamReader("pass.txt");
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.CurrentCulture)
        {
            Delimiter = ","
        });
        var result = csv.GetRecords<dynamic>().ToList();
        return (result[0].Username, result[0].Password);
    }
}