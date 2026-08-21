using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fragments.Data;
using Image = UnityEngine.UI.Image;

namespace Fragments.UI
{
    public class LibraryManager : MonoBehaviour
    {
        [Header("Drag your 6 slots here from the Hierarchy, in order")]
        public GameObject[] slots = new GameObject[6];

        [Header("Navigation")]
        public SceneNavigator sceneNavigator;

        [Header("Filled Slot Prefab (optional — or reuse the same slot)")]
        [Tooltip("If null, the script reuses the existing slot and just swaps text/color.")]
        public GameObject filledSlotPrefab;

        [Header("Error Message")]
        [Tooltip("Assign a TMP text in the Library scene. It flashes when you try to create in an occupied / full slot.")]
        public TMP_Text errorText;
        [Tooltip("How long the error stays visible before fully fading out.")]
        public float errorDuration = 3f;

        Color[] _emptySlotColors;
        GameObject[] _filledInstances;
        Coroutine _errorRoutine;
        Color _errorBaseColor;

        void Start()
        {
            CacheEmptySlotColors();
            if (errorText != null)
            {
                _errorBaseColor = errorText.color;
                errorText.gameObject.SetActive(false);
            }
            PopulateSlots();
        }

        public void Refresh()
        {
            PopulateSlots();
        }

        void CacheEmptySlotColors()
        {
            _emptySlotColors = new Color[slots.Length];
            for (int i = 0; i < slots.Length; i++)
            {
                var img = slots[i] != null ? slots[i].GetComponent<Image>() : null;
                _emptySlotColors[i] = img != null ? img.color : Color.white;
            }
        }

        void PopulateSlots()
        {
            ClearFilledInstances();

            List<JournalData> journals = JournalStore.LoadAll();
            if (_filledInstances == null || _filledInstances.Length != slots.Length)
                _filledInstances = new GameObject[slots.Length];

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) continue;

                if (i < journals.Count)
                    SetupFilledSlot(i, journals[i]);
                else
                    SetupEmptySlot(slots[i], i);
            }
        }

        void ClearFilledInstances()
        {
            if (_filledInstances == null) return;

            for (int i = 0; i < _filledInstances.Length; i++)
            {
                if (_filledInstances[i] != null)
                    DestroyImmediate(_filledInstances[i]);
                _filledInstances[i] = null;
            }

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null)
                    slots[i].SetActive(true);
            }
        }

        void SetupFilledSlot(int index, JournalData journal)
        {
            GameObject slot = slots[index];

            if (filledSlotPrefab != null)
            {
                GameObject filled = Instantiate(filledSlotPrefab, slot.transform.parent);
                filled.transform.SetSiblingIndex(slot.transform.GetSiblingIndex());
                filled.transform.localPosition = slot.transform.localPosition;
                filled.transform.localScale = slot.transform.localScale;

                RectTransform original = slot.GetComponent<RectTransform>();
                RectTransform replacement = filled.GetComponent<RectTransform>();
                if (original != null && replacement != null)
                {
                    replacement.anchorMin = original.anchorMin;
                    replacement.anchorMax = original.anchorMax;
                    replacement.anchoredPosition = original.anchoredPosition;
                    replacement.sizeDelta = original.sizeDelta;
                    replacement.pivot = original.pivot;
                }

                slot.SetActive(false);
                _filledInstances[index] = filled;
                slot = filled;
            }

            TMP_Text[] texts = slot.GetComponentsInChildren<TMP_Text>(true);

            foreach (TMP_Text t in texts)
            {
                if (t.text.Trim() == "+")
                    t.gameObject.SetActive(false);
                else
                {
                    t.gameObject.SetActive(true);
                    t.text = journal.journalName;
                }
            }

            bool hasNameLabel = false;
            foreach (TMP_Text t in texts)
            {
                if (t.gameObject.activeSelf && t.text == journal.journalName)
                {
                    hasNameLabel = true;
                    break;
                }
            }
            if (!hasNameLabel && texts.Length > 0)
            {
                texts[0].gameObject.SetActive(true);
                texts[0].text = journal.journalName;
            }

            if (ColorUtility.TryParseHtmlString(journal.coverColorHex, out Color col))
            {
                Image img = slot.GetComponent<Image>();
                if (img != null) img.color = col;
            }

            Button btn = slot.GetComponent<Button>();
            if (btn != null)
            {
                // Wipe persistent (inspector) listeners too — RemoveAllListeners only clears runtime ones.
                btn.onClick = new Button.ButtonClickedEvent();
                string id = journal.id;
                btn.onClick.AddListener(() => sceneNavigator.OpenExistingJournal(id));
            }
        }

        void SetupEmptySlot(GameObject slot, int index)
        {
            slot.SetActive(true);

            TMP_Text[] texts = slot.GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text t in texts)
            {
                if (t.text.Trim() == "+")
                    t.gameObject.SetActive(true);
                else
                    t.gameObject.SetActive(false);
            }

            Image img = slot.GetComponent<Image>();
            if (img != null && _emptySlotColors != null && index < _emptySlotColors.Length)
                img.color = _emptySlotColors[index];

            Button btn = slot.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick = new Button.ButtonClickedEvent();
                int slotIndex = index;
                btn.onClick.AddListener(() => TryCreateInSlot(slotIndex));
            }
        }

        void TryCreateInSlot(int slotIndex)
        {
            List<JournalData> journals = JournalStore.LoadAll();

            // Occupied slot → open that journal in the Journaling scene (never Create Journal).
            if (slotIndex < journals.Count)
            {
                sceneNavigator.OpenExistingJournal(journals[slotIndex].id);
                return;
            }

            if (journals.Count >= slots.Length)
            {
                ShowError();
                return;
            }

            sceneNavigator.CreateNewJournal();
        }

        public void ShowError()
        {
            if (errorText == null) return;

            if (_errorRoutine != null)
                StopCoroutine(_errorRoutine);
            _errorRoutine = StartCoroutine(FlashErrorRoutine());
        }

        IEnumerator FlashErrorRoutine()
        {
            errorText.gameObject.SetActive(true);
            Color c = _errorBaseColor;
            c.a = 1f;
            errorText.color = c;

            // Hold fully visible for most of the duration, then fade out.
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
