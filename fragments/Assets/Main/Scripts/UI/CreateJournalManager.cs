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

        [Header("Binding")]
        public Button hardcoverButton;
        public Button ringsButton;
        public Button staplesButton;

        [Header("Sheet Count")]
        public Slider sheetsSlider;
        public TMP_Text sheetsValueLabel;

        [Header("Navigation")]
        public SceneNavigator sceneNavigator;

        [Header("Visual Feedback")]
        public Color selectedOutlineColor = new Color(0.91f, 0.71f, 0.30f);
        public Color unselectedOutlineColor = new Color(0.3f, 0.3f, 0.3f);

        [Header("Error Message")]
        [Tooltip("Assign a TMP text in the Create Journal scene. Flashes when the library is full.")]
        public TMP_Text errorText;
        public float errorDuration = 3f;
        public int maxJournals = 6;

        string _selectedColor;
        string _selectedPattern = "plain";
        string _selectedBinding = "hardcover";
        int _sheetCount = 8;
        Coroutine _errorRoutine;
        Color _errorBaseColor;

        void Start()
        {
            if (errorText != null)
            {
                _errorBaseColor = errorText.color;
                errorText.gameObject.SetActive(false);
            }

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

            hardcoverButton.onClick.AddListener(() => SelectBinding("hardcover"));
            ringsButton.onClick.AddListener(() => SelectBinding("rings"));
            staplesButton.onClick.AddListener(() => SelectBinding("staples"));

            sheetsSlider.minValue = 3;
            sheetsSlider.maxValue = 16;
            sheetsSlider.wholeNumbers = true;
            sheetsSlider.value = 8;
            sheetsSlider.onValueChanged.AddListener(OnSheetCountChanged);
            OnSheetCountChanged(sheetsSlider.value);

            beginButton.onClick.AddListener(OnBegin);

            if (colorHexValues.Length > 0) SelectColor(0);
            SelectPattern("plain");
            SelectBinding("hardcover");
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

        void SelectBinding(string binding)
        {
            _selectedBinding = binding;

            HighlightBindingButton(hardcoverButton, binding == "hardcover");
            HighlightBindingButton(ringsButton, binding == "rings");
            HighlightBindingButton(staplesButton, binding == "staples");
        }

        void HighlightBindingButton(Button btn, bool active)
        {
            var colors = btn.colors;
            colors.normalColor = active
                ? new Color(0.91f, 0.71f, 0.30f, 1f)
                : new Color(0.2f, 0.2f, 0.2f, 1f);
            btn.colors = colors;
        }

        void OnSheetCountChanged(float value)
        {
            _sheetCount = Mathf.Clamp(Mathf.RoundToInt(value), 3, 16);
            if (sheetsValueLabel != null)
                sheetsValueLabel.text = _sheetCount.ToString();
        }

        void OnBegin()
        {
            if (JournalStore.LoadAll().Count >= maxJournals)
            {
                ShowError();
                return;
            }

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
                binding = _selectedBinding,
                sheetCount = _sheetCount,
                createdAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                lastOpenedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            JournalStore.Save(journal);
            JournalSession.CurrentId = journal.id;
            sceneNavigator.LoadJournaling();
        }

        void ShowError()
        {
            if (errorText == null) return;
            if (_errorRoutine != null)
                StopCoroutine(_errorRoutine);
            _errorRoutine = StartCoroutine(FlashErrorRoutine());
        }

        System.Collections.IEnumerator FlashErrorRoutine()
        {
            errorText.gameObject.SetActive(true);
            Color c = _errorBaseColor;
            c.a = 1f;
            errorText.color = c;

            float hold = Mathf.Max(0f, errorDuration - 1f);
            if (hold > 0f)
                yield return new WaitForSeconds(hold);

            float fade = Mathf.Min(1f, errorDuration);
            float t = 0f;
            while (t < fade)
            {
                t += Time.deltaTime;
                c.a = 1f - Mathf.Clamp01(t / fade);
                errorText.color = c;
                yield return null;
            }

            c.a = 0f;
            errorText.color = c;
            errorText.gameObject.SetActive(false);
            _errorRoutine = null;
        }
    }
}