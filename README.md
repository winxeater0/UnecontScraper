# UneCont.Scraper — Desafio Técnico (C#)

Aplicação de **scraping** do site público [books.toscrape.com](https://books.toscrape.com/) com **filtros configuráveis**, **exportação JSON/XML** e **integração via POST** em API mock (httpbin).

## Requisitos atendidos

- ✅ Implementação em **C#** (.NET 8).
- ✅ **Parsing HTML** com `HtmlAgilityPack`.
- ✅ **Filtros configuráveis** (preço mínimo/máximo e estrelas exatas).
- ✅ **Normalização** de tipos (decimal/int) e **exportação** para `books.json` e `books.xml`.
- ✅ **Integração REST**: envia o JSON para `https://httpbin.org/post` (ou endpoint configurável).
- ✅ **Tratamento de erros** básico e **logging** no console.
- ✅ **Documentação** clara (este README).

---

## Como executar

> Pré-requisitos: **.NET 8 SDK**

```bash
# Restaurar pacotes
dotnet restore

# Build
dotnet build -c Release

# Executar (usa appsettings.json por padrão)
dotnet run --project UneCont.Scraper -c Release
```

Ao final, os arquivos serão gerados em `output/books.json` e `output/books.xml` e o conteúdo JSON será enviado via POST para o endpoint configurado (`https://httpbin.org/post` por padrão).

---

## Configuração

A aplicação lê configurações de **três fontes**, nesta ordem (as últimas sobrepõem as anteriores):
1. `appsettings.json`
2. Variáveis de ambiente (prefixo `BOOKS_`)
3. Argumentos de **linha de comando**

### appsettings.json (exemplo)

```json
{
  "Categories": ["Travel", "Mystery", "Science Fiction"],
  "MinPrice": null,
  "MaxPrice": null,
  "Stars": null,
  "ApiUrl": "https://httpbin.org/post",
  "OutputDir": "output",
  "UserAgent": "UneContScraper/1.0 (+https://unecont.example)"
}
```

> **Categorias** devem ser nomes legíveis exatamente como aparecem no site (ex.: *Travel*, *Mystery*, *Science Fiction*). A aplicação irá selecionar **até 3** categorias: se você informar menos, ela completa com outras disponíveis.

### Variáveis de ambiente

Use o prefixo `BOOKS_`. Para `Categories`, envie uma string CSV:

- `BOOKS_Categories="Travel,Mystery,Science Fiction"`
- `BOOKS_MinPrice=20`
- `BOOKS_MaxPrice=60`
- `BOOKS_Stars=4`
- `BOOKS_ApiUrl=https://httpbin.org/post`
- `BOOKS_OutputDir=out`

### Linha de comando

Os nomes são iguais às chaves. Exemplos:

```bash
# Escolher categorias + filtrar por preço e estrelas = 5
dotnet run --project UneCont.Scraper --   Categories="Travel,Mystery,Science Fiction"   MinPrice=10 MaxPrice=80 Stars=5

# Alterar diretório de saída e endpoint
dotnet run --project UneCont.Scraper -- OutputDir=export ApiUrl=https://httpbin.org/post
```

> **Dica:** Para `Categories` também é aceito CSV quando passado como argumento/ENV.

---

## O que a aplicação faz

1. Carrega e mapeia as categorias do menu lateral do site.
2. Seleciona **até 3** categorias (as informadas e, se faltar, completa com quaisquer outras).
3. Percorre **todas as páginas** de cada categoria, coletando os livros (título, preço, estrelas, categoria, URL).
4. **Normaliza** preço (decimal) e estrelas (int 1..5).
5. **Aplica os filtros** configurados (preço min/max e estrelas exatas).
6. Exporta o resultado para `books.json` (indentado) e `books.xml`.
7. Envia o JSON via **POST** para a API configurada e mostra no console o **status** e um **resumo** (total, categorias, min/média/máx de preço).

---

## Estrutura do projeto

```
UneCont.Scraper/
├─ Models/
│  ├─ Book.cs           # DTOs de saída (Book + BookList p/ XML)
│  └─ Config.cs         # AppConfig (categorias, filtros, api, etc.)
├─ Services/
│  └─ BookScraper.cs    # Lógica de scraping/paginação e parsing HTML
├─ Utilities/
│  └─ RatingParser.cs   # Converte classe CSS (One..Five) -> int (1..5)
├─ Program.cs           # Bootstrap: config/logging, I/O, POST e resumo
├─ appsettings.json     # Configuração padrão (editável)
└─ UneCont.Scraper.csproj
```

---

## Notas técnicas

- **Parser HTML:** `HtmlAgilityPack`. Seletores XPath:
  - Livros na lista: `//article[contains(@class,'product_pod')]`
  - Título/URL: `.//h3/a` (usa `title` e `href`)
  - Preço: `.//p[contains(@class,'price_color')]` (normalização remove `£`)
  - Estrelas: `.//p[contains(@class,'star-rating')]` (mapeado via classe CSS `One..Five`)
  - Próxima página: `//li[contains(@class,'next')]/a`
- **Normalização de preço**: remove caracteres não numéricos e converte usando `InvariantCulture`.
- **Categorias**: são lidas do menu lateral da **home** e comparadas pelo **nome** (case-insensitive).
- **Resiliência**: HTTP com timeout de 30s e `EnsureSuccessStatusCode`. Logging de progresso e erros.

---

## Possíveis melhorias (opcionais)

- Retentativas com `Polly` para falhas transitórias.
- Execução paralela por categoria/página com `Channel`/`Parallel.ForEachAsync`.
- Persistência local de cache para evitar repetir scraping durante desenvolvimento.
- Testes unitários para `ParsePrice` e `RatingParser`.
- Dockerfile para empacotar execução.

---

## Licença

Uso livre para fins do desafio.