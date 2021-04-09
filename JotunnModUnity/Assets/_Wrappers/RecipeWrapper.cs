using UnityEngine;

namespace ValheimMod.UnityWrappers
{
    /// <summary>
    /// A wrapper for Valheim's <see cref="Recipe" />.
    /// </summary>
    [UnityEngine.CreateAssetMenu]
    public class RecipeWrapper : Recipe
    {
        public bool includeInRelease = false;
    }
}