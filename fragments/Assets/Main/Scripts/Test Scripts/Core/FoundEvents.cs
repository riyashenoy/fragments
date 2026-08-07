using System;
using Found.Scrap;

namespace Found.Core
{
    /// <summary>
    /// Tiny global event bus so tools, UI, and the memory recipe stay decoupled — the
    /// same role the web prototype's toast()/markRecipe() calls played. Subscribe your
    /// world-space toast panel to OnToast and your recipe tracker to OnFragmentCreated.
    /// </summary>
    public static class FoundEvents
    {
        public static event Action<string> OnToast;
        public static event Action<ScrapFragment> OnFragmentCreated;
        public static event Action<string> OnRecipeItemComplete; // category string

        public static void Toast(string message) => OnToast?.Invoke(message);
        public static void RaiseFragmentCreated(ScrapFragment f) => OnFragmentCreated?.Invoke(f);
        public static void RaiseRecipeItem(string category) => OnRecipeItemComplete?.Invoke(category);
    }
}
