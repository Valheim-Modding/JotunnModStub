using System.Collections.Generic;
using ExampleRecipesMod.Models;
using Jotunn.Utils;

namespace ExampleRecipesMod.Services
{
    internal class ExtendedRecipeManager
    {
        public static List<ExtendedRecipe> LoadRecipesFromJson(string recipesPath)
        {
            var json = AssetUtils.LoadText(recipesPath);
            return SimpleJson.SimpleJson.DeserializeObject<List<ExtendedRecipe>>(json);
        }
    }
}
