using HtmlAgilityPack;
using RecipesWebScraper;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

int pages = 1;
string sort = string.Empty;

Console.WriteLine($"How many pages do you want to receive? (1 by default)");
try
{
    int pagesInput = int.Parse(Console.ReadLine());
    if (pagesInput > pages)
    {
        pages = pagesInput;
    }
}
catch (Exception)
{
    Console.WriteLine(1);
}


Console.WriteLine(@$"
Sort recipes by:
    0 -> Default
    1 -> Newest
    2 -> Popularity");
try
{
    int sortInput = int.Parse(Console.ReadLine());
    if (sortInput == 1)
    {
        sort = "?sort=nd";
    }
    else if (sortInput == 2)
    {
        sort = "?sort=vd";
    }
}
catch (Exception)
{
    Console.WriteLine("Default");

}

var client = new HttpClient();

List<Recipe> recipes = new List<Recipe>();
List<string> recipeUrls = [];

ParallelOptions parallelOptions = new ParallelOptions() { MaxDegreeOfParallelism = 2 };

Parallel.For(0, pages, parallelOptions, (i) =>
{
    Thread.Sleep(3000);
    var result = client.GetAsync($"https://recepti.gotvach.bg/{i}{sort}").Result;
    var html = result.Content.ReadAsStringAsync().Result;

    var htmlDoc = new HtmlDocument();
    htmlDoc.LoadHtml(html);
    var currPageRecipeUrls = htmlDoc.DocumentNode
       .SelectNodes("//a[@class=\"title\"]")
       .Select(node => node.Attributes["href"].Value);
    recipeUrls.AddRange(currPageRecipeUrls);
});

recipeUrls.Insert(0, "https://recepti.gotvach.bg/r-274905-%D0%A1%D0%BF%D0%B0%D0%BD%D0%B0%D1%87%D0%B5%D0%BD%D0%B0_%D1%82%D0%BE%D1%80%D1%82%D0%B0_%D1%81_%D1%8F%D0%B3%D0%BE%D0%B4%D0%B8_%D0%B8_%D0%BC%D0%B0%D1%81%D0%BA%D0%B0%D1%80%D0%BF%D0%BE%D0%BD%D0%B5");

Parallel.ForEach(recipeUrls, parallelOptions, url =>
{
    Thread.Sleep(1000);
    var recipeHtml = (client.GetAsync(url).Result)
        .Content.ReadAsStringAsync().Result;

    var htmlDoc = new HtmlDocument();
    htmlDoc.LoadHtml(recipeHtml);

    var entityDiv = htmlDoc.DocumentNode.SelectSingleNode("//div[@class=\"combocolumn mr\"]");

    var owner = entityDiv.SelectSingleNode("//div[@class=\"aub\"]/a").InnerText;
    var title = entityDiv.SelectSingleNode("//h1").InnerText;
    var createdOn = DateTime.ParseExact(entityDiv.SelectSingleNode("//div[@class=\"date\"]").InnerText, "dd/MM/yyyy", CultureInfo.InvariantCulture);
    var indiSection = entityDiv.SelectSingleNode("//div[@class=\"indi\"]").ChildNodes;

    var prepElements = entityDiv.SelectNodes("//div[@class=\"icb-prep\"]");
    var totalElement = entityDiv.SelectSingleNode("//div[@class=\"icb-tot\"]");
    var portionsElement = entityDiv.SelectSingleNode("//div[@class=\"icb-fak\"]");

    int prepTime = 0;
    int cookingTime = 0;
    int totalTime = 0;
    int portions = 0;

    if (prepElements != null)
    {
        if (prepElements.Count > 0)
        {
            prepTime = GetIndiElementValue(prepElements[0]);
        }
        if (prepElements.Count > 1)
        {
            cookingTime = GetIndiElementValue(prepElements[1]);
        }
    }

    if (totalElement != null)
    {
        totalTime = GetIndiElementValue(totalElement);
    }
    else
    {
        totalTime = prepTime + totalTime;
    }

    if (portionsElement != null)
    {
        portions = GetIndiElementValue(portionsElement);
    }

    var productsQuantities = new Dictionary<string, string?>();
    entityDiv.SelectNodes("//section[@class=\"products new\"]/ul/li")
        .ToList()
        .ForEach(li =>
        {
            if (li.FirstChild.Name == "b")
            {
                var productSplitedItems = li.InnerText.Split("-");
                string name = productSplitedItems[0];
                string? quantity = null;
                if (productSplitedItems.Length > 1)
                {
                    quantity = productSplitedItems[1];
                }

                if (productsQuantities.ContainsKey(name))
                {
                    productsQuantities[name] += $" + {quantity}";
                }
                else
                {
                    productsQuantities.Add(name, quantity);

                }
            }
        });

    var description = entityDiv.SelectNodes("//div[@class=\"text\"]/p[@class=\"desc\"]")
        .Select(node => node.InnerText.TrimEnd())
        .Aggregate((a, b) => $"{a} {b}");

    Recipe recipe = new Recipe(
        owner,
        title,
        createdOn,
        prepTime,
        cookingTime,
        totalTime,
        portions,
        productsQuantities,
        description
        );
    recipes.Add(recipe);

    Console.OutputEncoding = Encoding.UTF8;
    Console.WriteLine(recipe.Title);
});

await using (FileStream stream = File.Create("../../../Recipes.json"))
{
    var jsonOptions = new JsonSerializerOptions
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        WriteIndented = true
    };
    await JsonSerializer.SerializeAsync(stream, recipes, jsonOptions);
}
Console.WriteLine(recipes.Count + " recipes saved.");
static int GetIndiElementValue(HtmlNode el)
{
    var value = el.InnerHtml
    .Split("</div>")[1]
    .Split(" ")[0];
    try
    {
        return int.Parse(value);
    }
    catch (Exception)
    {
        return 0;
    }
}