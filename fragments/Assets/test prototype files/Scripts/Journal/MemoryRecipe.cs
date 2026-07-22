using System.Collections.Generic;
using UnityEngine;
using Found.Core;

namespace Found.Journal
{
    /// <summary>
    /// The gentle objective from the brief: build a page from one café colour, one
    /// environmental tape, one found-object sticker, one photograph, one handwritten
    /// thought. Subscribes to FoundEvents.OnRecipeItemComplete (categories raised by the
    /// tools) and fires OnComplete when all five are collected.
    /// </summary>
    public class MemoryRecipe : MonoBehaviour
    {
        static readonly string[] Required = { "color", "tape", "sticker", "photo", "write" };

        readonly HashSet<string> _done = new();
        public bool IsComplete => _done.Count >= Required.Length;

        [System.Serializable] public class RecipeEvent : UnityEngine.Events.UnityEvent<string> { }
        public RecipeEvent OnItemComplete;              // category → tick the UI line
        public UnityEngine.Events.UnityEvent OnComplete; // "Your memory of this place is complete."

        void OnEnable() => FoundEvents.OnRecipeItemComplete += Mark;
        void OnDisable() => FoundEvents.OnRecipeItemComplete -= Mark;

        public void Mark(string category)
        {
            bool required = System.Array.IndexOf(Required, category) >= 0;
            if (!required || _done.Contains(category)) return;

            _done.Add(category);
            OnItemComplete?.Invoke(category);

            if (IsComplete)
            {
                FoundEvents.Toast("Your memory of this place is complete.");
                OnComplete?.Invoke();
            }
        }

        public void ResetRecipe()
        {
            _done.Clear();
        }
    }
}
