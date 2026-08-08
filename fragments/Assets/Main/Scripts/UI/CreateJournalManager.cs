using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fragments.Data;
using Image = UnityEngine.UI.Image; // resolves the ambiguity globally

namespace Fragments.UI
{
    public class CreateJournalManager : MonoBehaviour
    {
        [Header("Form Fields")]
        public TMP_InputField nameInput;
        public Button beginButton;

        [Header("Color Swatches")]
        public Button[] colorButtons;
        public string[] colorHexValues;

        [Header("Pattern Buttons")]
        public Button plainButton;
        public Button dottedButton;
        public Button stripedButton;

        [Header("Navigation")]
        public SceneNavigator sceneNavigator;

        [Header("Visual Feedback")]
        public Color selectedOutlineColor = new Color(0.91f, 0.71f, 0.30f);
        public Color unselectedOutlineColor = new Color(0.3f, 0.3f, 0.3f);

        string _selectedColor;
        string _selectedPattern = "plain";

        void Start()
        {
            for (int i = 0; i < colorButtons.Length; i++)
            {
                int index = i;
                colorButtons[i].onClick.AddListener(() => SelectColor(index));

                if (i < colorHexValues.Length &&
                    ColorUtility.TryParseHtmlString(colorHexValues[i], out Color col))
                    colorButtons[i].GetComponent<Image>().color = col;
            }

            plainButton.onClick.AddListener(() => SelectPattern("plain"));
            dottedButton.onClick.AddListener(() => SelectPattern("dotted"));
            stripedButton.onClick.AddListener(() => SelectPattern("striped"));

            beginButton.onClick.AddListener(OnBegin);

            if (colorHexValues.Length > 0) SelectColor(0);
            SelectPattern("plain");
        }

        void SelectColor(int index)
        {
            _selectedColor = colorHexValues[index];

            for (int i = 0; i < colorButtons.Length; i++)
            {
                var outline = colorButtons[i].GetComponent<Outline>();
                if (outline == null)
                    outline = colorButtons[i].gameObject.AddComponent<Outline>();
                outline.effectColor = (i == index) ? selectedOutlineColor : unselectedOutlineColor;
                outline.effectDistance = (i == index) ? new Vector2(3, 3) : new Vector2(0, 0);
            }
        }

        void SelectPattern(string pattern)
        {
            _selectedPattern = pattern;

            HighlightPatternButton(plainButton, pattern == "plain");
            HighlightPatternButton(dottedButton, pattern == "dotted");
            HighlightPatternButton(stripedButton, pattern == "striped");
        }

        void HighlightPatternButton(Button btn, bool active)
        {
            var colors = btn.colors;
            colors.normalColor = active
                ? new Color(0.91f, 0.71f, 0.30f, 1f)
                : new Color(0.2f, 0.2f, 0.2f, 1f);
            btn.colors = colors;
        }

        void OnBegin()
        {
            string journalName = nameInput.text.Trim();
            if (string.IsNullOrEmpty(journalName))
                journalName = "untitled journal";

            if (string.IsNullOrEmpty(_selectedColor) && colorHexValues.Length > 0)
                _selectedColor = colorHexValues[0];

            var journal = new JournalData
            {
                id = Guid.NewGuid().ToString(),
                journalName = journalName,
                coverColorHex = _selectedColor,
                pagePattern = _selectedPattern,
                createdAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                lastOpenedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            JournalStore.Save(journal);
            JournalSession.CurrentId = journal.id;
            sceneNavigator.LoadJournaling();
        }
    }
}