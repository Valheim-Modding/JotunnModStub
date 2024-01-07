using System.Collections.Generic;
using System.Linq;
using Jotunn.Configs;
using Jotunn.Entities;

namespace ExampleRecipesMod.Models
{
    public class ExtendedRecipeRequirement
    {
        public string item;
        public int amount;
        public int amountPerLevel;
        public bool recover;

        public static RequirementConfig Convert(ExtendedRecipeRequirement extendedRecipeRequirement)
        {
            return new RequirementConfig()
            {
                Amount = extendedRecipeRequirement.amount,
                Item = extendedRecipeRequirement.item,
                AmountPerLevel = extendedRecipeRequirement.amountPerLevel,
                Recover = extendedRecipeRequirement.recover
            };
        }
    }

    public class ExtendedRecipe
    {
        public string name;
        public string nameValue;
        public string prefabPath;
        public string item;
        public string descriptionToken;
        public string description;
        public int amount;
        public string craftingStation;
        public int minStationLevel;
        public bool enabled;
        public string repairStation;
        public List<ExtendedRecipeRequirement> resources;

        public static CustomRecipe Convert(ExtendedRecipe extendedRecipe)
        {
            return new CustomRecipe(
                new RecipeConfig
                {
                    Amount = extendedRecipe.amount,
                    CraftingStation = extendedRecipe.craftingStation,
                    Enabled = extendedRecipe.enabled,
                    Item = extendedRecipe.item,
                    MinStationLevel = extendedRecipe.minStationLevel,
                    Name = extendedRecipe.name,
                    RepairStation = extendedRecipe.repairStation,
                    Requirements = extendedRecipe.resources.Select(ExtendedRecipeRequirement.Convert).ToArray()
                }
            );
        }
    }
}