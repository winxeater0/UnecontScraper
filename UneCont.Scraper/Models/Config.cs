namespace UneCont.Scraper.Models;

public class AppConfig
{
    /// <summary>
    /// Lista de categorias desejadas (por nome legível), ex.: [ "Travel", "Mystery", "Science Fiction" ]
    /// Dica: também aceita CSV via CLI/ENV.
    /// </summary>
    public List<string> Categories { get; set; } = new();

    /// <summary>Preço mínimo (opcional)</summary>
    public decimal? MinPrice { get; set; }

    /// <summary>Preço máximo (opcional)</summary>
    public decimal? MaxPrice { get; set; }

    /// <summary>Número de estrelas exato (1..5). Opcional.</summary>
    public int? Stars { get; set; }

    /// <summary>URL da API</summary>
    public string ApiUrl { get; set; } = default!;

    /// <summary>Diretório de saída (padrão: output)</summary>
    public string OutputDir { get; set; } = default!;

    /// <summary>User-Agent para requisições</summary>
    public string UserAgent { get; set; } = default!;
}