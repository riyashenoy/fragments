using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fragments.Data;
using Found.Journal3D;
using Image = UnityEngine.UI.Image;
using Debug = UnityEngine.Debug;
using Application = UnityEngine.Application;

namespace Fragments.UI
{
    public class JournalingSceneManager : MonoBehaviour
    {
        [Header("Journal")]
        public JournalBuilder journalBuilder;
        public BookUIController bookUIController;

        [Header("Page Pattern Materials (assign all 3)")]
        public Material pagePlain;
        public Material pageDotted;
        public Material pageStriped;

        [Header("Navigation")]
        public SceneNavigator sceneNavigator;

        JournalData _data;

        void Start()
        {
            if (string.IsNullOrEmpty(JournalSession.CurrentId))
            {
                Debug.LogError("[Fragments] No journal ID set — returning to library.");
                sceneNavigator.LoadLibrary();
                return;
            }

            _data = JournalStore.Load(JournalSession.CurrentId);

            if (_data == null)
            {
                Debug.LogError("[Fragments] Journal not found on disk — returning to library.");
                sceneNavigator.LoadLibrary();
                return;
            }

            // Update last opened time
            _data.lastOpenedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            JournalStore.Save(_data);

            // Style and build the book
            BuildJournal();

            Debug.Log($"[Fragments] Loaded: {_data.journalName} | Cover: {_data.coverColorHex} | Pattern: {_data.pagePattern}");
        }

        void BuildJournal()
        {
            if (journalBuilder == null)
            {
                Debug.LogError("[Fragments] JournalBuilder not assigned!");
                return;
            }

            // Set cover color
            if (journalBuilder.shellMaterial != null &&
                ColorUtility.TryParseHtmlString(_data.coverColorHex, out Color coverCol))
            {
                // Create a runtime copy so we don't permanently modify the asset
                journalBuilder.shellMaterial = new Material(journalBuilder.shellMaterial);
                journalBuilder.shellMaterial.color = coverCol;
            }

            // Set page pattern
            Material pageMat = _data.pagePattern switch
            {
                "dotted" => pageDotted,
                "striped" => pageStriped,
                _ => pagePlain
            };
            if (pageMat != null)
                journalBuilder.pageMaterial = pageMat;

            // Build the book
            journalBuilder.Build();

            // Wire the book UI controller to the newly created Journal component
            Journal journal = journalBuilder.GetComponent<Journal>();
            if (bookUIController != null && journal != null)
                bookUIController.journal = journal;
        }

        public void GoBack()
        {
            // TODO: save journal contents before leaving (Phase 5)
            sceneNavigator.LoadLibrary();
        }
    }
}