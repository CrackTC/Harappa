#:package CsvHelper@33.1.0
using System.Globalization;
using System.IO.Compression;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;

if (args.Length != 2)
{
    Console.WriteLine($"Usage: {AppDomain.CurrentDomain.FriendlyName} <token> <outputDir>");
    return;
}

var token = args[0];
var outputDir = args[1];
var client = new HttpClient { BaseAddress = new Uri("https://paratranz.cn/api/") };
client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

using var zip = await ZipArchive.CreateAsync(
    await client.GetStreamAsync("projects/18245/artifacts/download"),
    ZipArchiveMode.Read,
    leaveOpen: false,
    Encoding.UTF8
);

var config = new CsvConfiguration(CultureInfo.InvariantCulture)
{
    NewLine = "\n",
    Encoding = Encoding.UTF8,
};
var utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

zip.Entries.ToList()
    .ForEach(entry =>
    {
        var stream = entry.Open();
        stream.ReadByte();
        stream.ReadByte();
        stream.ReadByte();
        using var csv = new CsvReader(new StreamReader(stream), config, leaveOpen: false);
        using var writer = new StreamWriter(
            Path.Combine(outputDir, Path.GetFileNameWithoutExtension(entry.Name)),
            append: false,
            encoding: utf8WithoutBom
        );

        bool isFirstRow = true;
        while (csv.Read())
        {
            if (isFirstRow)
                isFirstRow = false;
            else
                writer.Write('|');

            var key = csv.GetField<string>(0);
            var value = csv.GetField<string>(1);
            if (csv.TryGetField<string>(2, out var translation))
                value = translation;
            writer.Write($"{key}^{value}");
        }
    });
