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

        void Start()
        {
            PopulateSlots();
        }

        void PopulateSlots()
        {
            List<JournalData> journals = JournalStore.LoadAll();

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) continue;

                if (i < journals.Count)
                {
                    SetupFilledSlot(slots[i], journals[i]);
                }
                else
                {
                    SetupEmptySlot(slots[i]);
                }
            }
        }

        void SetupFilledSlot(GameObject slot, JournalData journal)
        {
            // If you have a separate filled prefab, swap it in
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
                slot = filled;
            }

            // Find child texts — looks for ANY TMP_Text children
            TMP_Text[] texts = slot.GetComponentsInChildren<TMP_Text>(true);

            foreach (TMP_Text t in texts)
            {
                if (t.text.Trim() == "+")
                {
                    // This is the plus sign — hide it
                    t.gameObject.SetActive(false);
                }
                else
                {
                    // This is the name label — show it with the journal name
                    t.gameObject.SetActive(true);
                    t.text = journal.journalName;
                }
            }

            // If there's no existing name label (all texts were "+"),
            // just repurpose the "+" text
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

            // Tint the slot background to the cover color
            if (ColorUtility.TryParseHtmlString(journal.coverColorHex, out Color col))
            {
                Image img = slot.GetComponent<Image>();
                if (img != null) img.color = col;
            }

            // Wire the click — open this journal directly
            Button btn = slot.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                string id = journal.id;
                btn.onClick.AddListener(() => sceneNavigator.OpenExistingJournal(id));
            }
        }

        void SetupEmptySlot(GameObject slot)
        {
            // Make sure the "+" is visible
            TMP_Text[] texts = slot.GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text t in texts)
            {
                if (t.text.Trim() == "+")
                    t.gameObject.SetActive(true);
                else
                    t.gameObject.SetActive(false);
            }

            // Wire the click — go to creation screen
            Button btn = slot.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => sceneNavigator.CreateNewJournal());
            }
        }
    }
}