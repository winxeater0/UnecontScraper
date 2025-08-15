using System.Text;
using System.Text.Json;
using System.Xml.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UneCont.Scraper.Models;
using UneCont.Scraper.Services;

var builder = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables(prefix: "BOOKS_")
    .AddCommandLine(args);

var configuration = builder.Build();

var cfg = new AppConfig();
configuration.Bind(cfg);

// log
using var loggerFactory = LoggerFactory.Create(logging =>
{
    logging.AddSimpleConsole(o =>
    {
        o.SingleLine = true;
        o.TimestampFormat = "HH:mm:ss ";
    });
    logging.SetMinimumLevel(LogLevel.Information);
});

var logger = loggerFactory.CreateLogger("Main");

try
{
    using var http = new HttpClient(new HttpClientHandler
    {
        AutomaticDecompression = System.Net.DecompressionMethods.All
    })
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    var scraper = new BookScraperService(http, cfg, loggerFactory.CreateLogger<BookScraperService>());
    var books = await scraper.RunAsync();

    // ponta o diretorio de saida
    var outDir = Path.GetFullPath(cfg.OutputDir);
    Directory.CreateDirectory(outDir);

    // monta o json
    var jsonPath = Path.Combine(outDir, "books.json");
    var jsonOpts = new JsonSerializerOptions { WriteIndented = true };
    var json = JsonSerializer.Serialize(books, jsonOpts);
    await File.WriteAllTextAsync(jsonPath, json, Encoding.UTF8);
    logger.LogInformation("Arquivo JSON gerado: {path}", jsonPath);

    // monta o xml
    var xmlPath = Path.Combine(outDir, "books.xml");
    var wrapper = new BookList { Items = books };
    var serializer = new XmlSerializer(typeof(BookList));
    await using (var fs = File.Create(xmlPath))
    {
        serializer.Serialize(fs, wrapper);
    }
    logger.LogInformation("Arquivo XML gerado: {path}", xmlPath);

    // post
    var apiUrl = cfg.ApiUrl ?? string.Empty;
    using var content = new StringContent(json, Encoding.UTF8, "application/json");
    var response = await http.PostAsync(apiUrl, content);
    var ok = response.IsSuccessStatusCode;
    var status = (int)response.StatusCode;
    logger.LogInformation("POST {url} -> Status {status} ({ok})", apiUrl, status, ok);

    decimal min = books.Count > 0 ? books.Min(b => b.Price) : 0m;
    decimal max = books.Count > 0 ? books.Max(b => b.Price) : 0m;
    decimal avg = books.Count > 0 ? Math.Round(books.Average(b => b.Price), 2) : 0m;
    var byCategory = books.GroupBy(b => b.Category).ToDictionary(g => g.Key, g => g.Count());

    Console.WriteLine();
    Console.WriteLine("===== RESUMO DO ENVIO =====");
    Console.WriteLine($"Itens enviados: {books.Count}");
    Console.WriteLine($"Categorias: {string.Join(", ", byCategory.Select(kv => $"{kv.Key}:{kv.Value}"))}");
    Console.WriteLine($"Preço (min/média/máx): {min} / {avg} / {max}");
    Console.WriteLine($"Status HTTP: {status} ({(ok ? "Sucesso" : "Falha")})");
    Console.WriteLine($"===========================");
}
catch (Exception ex)
{
    logger.LogError(ex, $"Erro na execução do scraper.");
    Environment.ExitCode = 2;
}