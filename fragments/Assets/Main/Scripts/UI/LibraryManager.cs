using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fragments.Data;

namespace Fragments.UI
{
    public class LibraryManager : MonoBehaviour
    {
        [Header("References")]
        public GameObject slotPrefab;
        public Transform gridParent;
        public SceneNavigator sceneNavigator;

        [Header("Settings")]
        public int maxSlots = 6;

        void Start()
        {
            PopulateSlots();
        }

        void PopulateSlots()
        {
            // Clear any existing children (in case of scene reload)
            foreach (Transform child in gridParent)
                Destroy(child.gameObject);

            List<JournalData> journals = JournalStore.LoadAll();

            // Filled slots
            for (int i = 0; i < journals.Count && i < maxSlots; i++)
            {
                GameObject slot = Instantiate(slotPrefab, gridParent);
                SetupFilledSlot(slot, journals[i]);
            }

            // Remaining empty slots
            int emptyCount = maxSlots - Mathf.Min(journals.Count, maxSlots);
            for (int i = 0; i < emptyCount; i++)
            {
                GameObject slot = Instantiate(slotPrefab, gridParent);
                SetupEmptySlot(slot);
            }
        }

        void SetupFilledSlot(GameObject slot, JournalData journal)
        {
            // Show the name, hide the "+"
            Transform nameText = slot.transform.Find("NameText");
            Transform plusText = slot.transform.Find("PlusText");

            if (nameText != null)
            {
                nameText.gameObject.SetActive(true);
                nameText.GetComponent<TMP_Text>().text = journal.journalName;
            }
            if (plusText != null)
                plusText.gameObject.SetActive(false);

            // Tint the button background to the cover color
            if (ColorUtility.TryParseHtmlString(journal.coverColorHex, out Color col))
                slot.GetComponent<Image>().color = col;

            // Click → open this journal directly
            string id = journal.id; // capture for closure
            slot.GetComponent<Button>().onClick.AddListener(() =>
                sceneNavigator.OpenExistingJournal(id));
        }

        void SetupEmptySlot(GameObject slot)
        {
            // Show the "+", hide the name
            Transform nameText = slot.transform.Find("NameText");
            Transform plusText = slot.transform.Find("PlusText");

            if (nameText != null)
                nameText.gameObject.SetActive(false);
            if (plusText != null)
                plusText.gameObject.SetActive(true);

            // Click → go to creation screen
            slot.GetComponent<Button>().onClick.AddListener(() =>
                sceneNavigator.CreateNewJournal());
        }
    }
}