using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace UneCont.Scraper.Models;

public class Book
{
    [JsonPropertyName("title")]
    [XmlElement("Title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("price")]
    [XmlElement("Price")]
    public decimal Price { get; set; }

    [JsonPropertyName("stars")]
    [XmlElement("Stars")]
    public int Stars { get; set; }

    [JsonPropertyName("category")]
    [XmlElement("Category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    [XmlElement("Url")]
    public string Url { get; set; } = string.Empty;
}

[XmlRoot("Books")]
public class BookList
{
    [XmlElement("Book")]
    public List<Book> Items { get; set; } = new();
}