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

        Color[] _emptySlotColors;
        GameObject[] _filledInstances;

        void Start()
        {
            CacheEmptySlotColors();
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
                btn.onClick.RemoveAllListeners();
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
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => sceneNavigator.CreateNewJournal());
            }
        }
    }
}
