using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Image = UnityEngine.UI.Image;
using Text = UnityEngine.UI.Text;

namespace Fragments.Book
{
    /// <summary>
    /// Connects the Book's page state and color settings to UI elements.
    /// Syncs bi-directionally: UI updates book, book state updates UI.
    /// </summary>
    public class BookUIBridge : MonoBehaviour
    {
        [SerializeField] private Book book;

        [Header("UI References")]
        [SerializeField] private Image coverImage;
        [SerializeField] private Text pageIndicator; // "Page 4 of 12"
        [SerializeField] private Button[] colorButtons;

        private Color currentCoverColor = Color.white;

        private void OnEnable()
        {
            if (book == null)
                book = GetComponentInParent<Book>();

            // Wire color buttons
            for (int i = 0; i < colorButtons.Length; i++)
            {
                int colorIndex = i;
                colorButtons[i].onClick.AddListener(() => SelectCoverColor(colorIndex));
            }
        }

        private void Update()
        {
            // Sync page indicator
            if (pageIndicator != null && book != null)
            {
                int currentPage = book.TurnedCount + 1; // 1-indexed for display
                int totalPages = book.Sheets.Count;
                pageIndicator.text = $"Page {currentPage} of {totalPages}";
            }
        }

        public void SelectCoverColor(int buttonIndex)
        {
            Color newColor = colorButtons[buttonIndex].image.color;
            SetCoverColor(newColor);
        }

        public void SetCoverColor(Color color)
        {
            currentCoverColor = color;

            // Update book cover material
            if (book != null && book.coverMaterial != null)
            {
                book.coverMaterial.color = color;
            }

            // Update UI indicator
            if (coverImage != null)
                coverImage.color = color;

            Debug.Log($"Cover color changed to {color}");
        }

        public Color GetCoverColor() => currentCoverColor;
    }
}