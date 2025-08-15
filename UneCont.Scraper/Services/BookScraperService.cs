using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.RegularExpressions;
using UneCont.Scraper.Models;
using UneCont.Scraper.Utilities;

namespace UneCont.Scraper.Services;

public class BookScraperService
{
    private readonly HttpClient _http;
    private readonly AppConfig _cfg;
    private readonly ILogger<BookScraperService> _logger;
    private readonly Uri _baseUri = new("https://books.toscrape.com/");

    public BookScraperService(HttpClient http, AppConfig cfg, ILogger<BookScraperService> logger)
    {
        _http = http;
        _cfg = cfg;
        _logger = logger;
    }

    public async Task<List<Book>> RunAsync(CancellationToken ct = default)
    {
        var categories = await LoadCategoriesAsync(ct);
        if (categories.Count == 0)
        {
            _logger.LogWarning("Nenhuma categoria encontrada no site.");
            return new List<Book>();
        }

        // normaliza entradas do usuario para comparação - case-insensitive
        var desired = _cfg.Categories
            .Select(c => c.Trim())
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (desired.Count == 0)
        {
            _logger.LogInformation("Nenhuma categoria especificada.");
            desired = new HashSet<string>(new[] { "Travel", "Mystery", "Science Fiction" }, StringComparer.OrdinalIgnoreCase);
        }

        // seleciona ateh 3 categorias
        var selected = categories
            .Where(kvp => desired.Contains(kvp.Key))
            .Take(3)
            .ToList();

        if (selected.Count < 3)
        {
            // completa com qlqr outras disponiveis sem duplicacao
            foreach (var kv in categories)
            {
                if (selected.Count >= 3) break;
                if (selected.Any(s => s.Key.Equals(kv.Key, StringComparison.OrdinalIgnoreCase))) continue;
                selected.Add(kv);
            }
        }

        _logger.LogInformation("Categorias selecionadas: {cats}", string.Join(", ", selected.Select(s => s.Key)));

        var all = new List<Book>();
        foreach (var (categoryName, categoryUrl) in selected)
        {
            _logger.LogInformation("Processando categoria: {cat} ({url})", categoryName, categoryUrl);
            var catBooks = await ScrapeCategoryAsync(categoryName, categoryUrl, ct);
            all.AddRange(catBooks);
            _logger.LogInformation(" -> {count} livros coletados na categoria {cat}", catBooks.Count, categoryName);
        }

        // filtrados
        var filtered = all
            .Where(b => !_cfg.MinPrice.HasValue || b.Price >= _cfg.MinPrice.Value)
            .Where(b => !_cfg.MaxPrice.HasValue || b.Price <= _cfg.MaxPrice.Value)
            .Where(b => !_cfg.Stars.HasValue || b.Stars == _cfg.Stars.Value)
            .ToList();

        _logger.LogInformation("Total coletado: {total} | Após filtros: {filtered}", all.Count, filtered.Count);
        return filtered;
    }

    private async Task<Dictionary<string, Uri>> LoadCategoriesAsync(CancellationToken ct)
    {
        var html = await GetHtmlAsync(_baseUri, ct);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // ul.nav-list > li > ul > li > a
        var dict = new Dictionary<string, Uri>(StringComparer.OrdinalIgnoreCase);
        var nodes = doc.DocumentNode.SelectNodes("//ul[contains(@class,'nav-list')]//li//ul//li/a");
        if (nodes == null) return dict;

        foreach (var a in nodes)
        {
            var name = WebUtility.HtmlDecode(a.InnerText).Trim();
            var href = a.GetAttributeValue("href", "").Trim();
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(href)) continue;
            var abs = new Uri(_baseUri, href);
            dict[name] = abs;
        }
        return dict;
    }

    private async Task<List<Book>> ScrapeCategoryAsync(string categoryName, Uri categoryUrl, CancellationToken ct)
    {
        var results = new List<Book>();
        var pageUrl = categoryUrl;

        while (true)
        {
            var pageHtml = await GetHtmlAsync(pageUrl, ct);
            var doc = new HtmlDocument();
            doc.LoadHtml(pageHtml);

            var productNodes = doc.DocumentNode.SelectNodes("//article[contains(@class,'product_pod')]");
            if (productNodes != null)
            {
                foreach (var n in productNodes)
                {
                    // titulo e link
                    var a = n.SelectSingleNode(".//h3/a");
                    var title = WebUtility.HtmlDecode(a?.GetAttributeValue("title", "").Trim() ?? "");
                    var href = a?.GetAttributeValue("href", "").Trim() ?? "";

                    // preço
                    var priceText = n.SelectSingleNode(".//p[contains(@class,'price_color')]")?.InnerText?.Trim() ?? "";
                    var price = ParsePrice(priceText);

                    // nota
                    var starClass = n.SelectSingleNode(".//p[contains(@class,'star-rating')]")?.GetAttributeValue("class", "");
                    var stars = RatingParser.FromCssClass(starClass);

                    var absUrl = new Uri(pageUrl, href);

                    results.Add(new Book
                    {
                        Title = title,
                        Price = price,
                        Stars = stars,
                        Category = categoryName,
                        Url = absUrl.ToString()
                    });
                }
            }

            // paginacao
            var next = doc.DocumentNode.SelectSingleNode("//li[contains(@class,'next')]/a");
            if (next == null) break;
            var nextHref = next.GetAttributeValue("href", "").Trim();
            if (string.IsNullOrWhiteSpace(nextHref)) break;
            pageUrl = new Uri(pageUrl, nextHref);
        }

        return results;
    }

    private static decimal ParsePrice(string priceText)
    {
        var normalized = Regex.Replace(priceText, "[^0-9\\.,]", "");
        normalized = normalized.Replace(",", ".");
        if (decimal.TryParse(normalized, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var value))
            return value;
        return 0m;
    }

    private async Task<string> GetHtmlAsync(Uri url, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.UserAgent.ParseAdd(_cfg.UserAgent);
        var res = await _http.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadAsStringAsync(ct);
    }
}