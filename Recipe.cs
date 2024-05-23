namespace RecipesWebScraper
{
    public class Recipe
    {
        public Recipe(
            string owner,
            string title,
            DateTime createdOn,
            int preparationTime,
            int? cookingTime,
            int totalTime,
            int portions,
            IDictionary<string, string?> products,
            string description
            )
        {
            Id = Guid.NewGuid().ToString();
            Owner = owner;
            Title = title;
            CreatedOn = createdOn;
            PreparationTime = preparationTime;
            CookingTime = cookingTime;
            TotalTime = totalTime;
            Portions = portions;
            Products = products;
            Description = description;

        }

        public string Id { get; set; }
        public string Owner { get; set; }

        public string Title { get; set; }
        public DateTime CreatedOn { get; set; }
        public int PreparationTime { get; set; }
        public int? CookingTime { get; set; }
        public int TotalTime { get; set; }
        public int Portions { get; set; }
        public IDictionary<string, string?> Products { get; set; }
        public string Description { get; set; }

    }
}
