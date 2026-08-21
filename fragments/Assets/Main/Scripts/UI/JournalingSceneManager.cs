using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fragments.Data;
using Fragments.Book;
using Found.Journal3D;
using Image = UnityEngine.UI.Image;
using Debug = UnityEngine.Debug;
using Application = UnityEngine.Application;

namespace Fragments.UI
{
    public class JournalingSceneManager : MonoBehaviour
    {
        [Header("Journal Prefab (drag your saved book prefab here)")]
        public GameObject journalPrefab;

        [Header("Where to spawn the book in the scene")]
        public Transform bookSpawnPoint;

        [Header("Book UI")]
        public BookUIController bookUIController;

        [Header("Page Pattern Materials (assign all 3)")]
        public Material pagePlain;
        public Material pageDotted;
        public Material pageStriped;

        [Header("Edge Material")]
        public Material pageEdge;

        [Header("Cover Material (base ù gets tinted at runtime)")]
        public Material coverBaseMaterial;

        [Header("Debug Display (temporary ù remove later)")]
        public TMP_Text journalNameText;
        public Image coverColorPreview;

        [Header("Navigation")]
        public SceneNavigator sceneNavigator;

        [Header("Book")]
        public Fragments.Book.Book book;

        JournalData _data;
        Journal _journal;

        void Start()
        {
            if (string.IsNullOrEmpty(JournalSession.CurrentId))
            {
                Debug.LogError("[Fragments] No journal ID set ù returning to library.");
                sceneNavigator.LoadLibrary();
                return;
            }

            _data = JournalStore.Load(JournalSession.CurrentId);

            if (_data == null)
            {
                Debug.LogError("[Fragments] Journal not found ù returning to library.");
                sceneNavigator.LoadLibrary();
                return;
            }

            ApplyDataToBook();

            _data.lastOpenedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            JournalStore.Save(_data);

            SpawnAndStyleBook();
            UpdateDebugDisplay();

            Debug.Log("[Fragments] Loaded: " + _data.journalName +
                      " | Cover: " + _data.coverColorHex +
                      " | Pattern: " + _data.pagePattern);
        }

        void ApplyDataToBook()
        {
            if (book == null || book.settings == null) return;

            // Runtime copy so we don't dirty the shared BookSettings asset
            book.settings = Instantiate(book.settings);

            // Binding type
            book.settings.binding = _data.binding switch
            {
                "rings" => Fragments.Book.BindingType.Rings,
                "staples" => Fragments.Book.BindingType.Staples,
                _ => Fragments.Book.BindingType.Hardcover
            };

            // Sheet count
            book.settings.sheetCount = Mathf.Clamp(_data.sheetCount, 3, 16);

            // Cover color
            if (book.coverMaterial != null &&
                ColorUtility.TryParseHtmlString(_data.coverColorHex, out Color coverCol))
            {
                // Create runtime copy so we don't modify the asset
                book.coverMaterial = new Material(book.coverMaterial);
                book.coverMaterial.color = coverCol;
            }

            // Rebuild with new settings
            book.Build();
        }

        void SpawnAndStyleBook()
        {
            if (journalPrefab == null)
            {
                Debug.LogError("[Fragments] No journal prefab assigned!");
                return;
            }

            // Spawn the prefab at the spawn point
            Vector3 spawnPos = bookSpawnPoint != null
                ? bookSpawnPoint.position
                : new Vector3(0f, -0.15f, 0.4f);
            Quaternion spawnRot = bookSpawnPoint != null
                ? bookSpawnPoint.rotation
                : Quaternion.Euler(30f, 0f, 0f);

            GameObject bookObj = Instantiate(journalPrefab, spawnPos, spawnRot);

            // Get the builder so we can set materials and rebuild at runtime
            JournalBuilder builder = bookObj.GetComponent<JournalBuilder>();

            if (builder == null)
            {
                Debug.LogError("[Fragments] Journal prefab has no JournalBuilder component!");
                return;
            }

            // Set cover color ù create a runtime copy so we don't modify the asset
            if (coverBaseMaterial != null &&
                ColorUtility.TryParseHtmlString(_data.coverColorHex, out Color coverCol))
            {
                Material coverInstance = new Material(coverBaseMaterial);
                coverInstance.color = coverCol;
                builder.shellMaterial = coverInstance;
            }

            // Set page pattern
            Material pageMat = _data.pagePattern switch
            {
                "dotted" => pageDotted,
                "striped" => pageStriped,
                _ => pagePlain
            };
            if (pageMat != null)
                builder.pageMaterial = pageMat;

            // Set edge material
            if (pageEdge != null)
                builder.edgeMaterial = pageEdge;

            // Rebuild at runtime ù this regenerates all meshes and initializes
            // the runtime state (curl data, physics joints) that doesn't survive
            // prefab serialization. The prefab still controls positioning and
            // dimensions; we just refresh the internals.
            builder.Build();

            // Now grab the fresh Journal component (Build() recreates it)
            _journal = bookObj.GetComponent<Journal>();

            if (_journal == null)
            {
                Debug.LogError("[Fragments] Journal component missing after Build!");
                return;
            }

            // Wire the book UI controller
            if (bookUIController != null)
                bookUIController.journal = _journal;
        }

        void UpdateDebugDisplay()
        {
            if (journalNameText != null)
                journalNameText.text = _data.journalName;

            if (coverColorPreview != null &&
                ColorUtility.TryParseHtmlString(_data.coverColorHex, out Color col))
                coverColorPreview.color = col;
        }

        public void AddPage()
        {
            if (book == null || _data == null) return;
            if (book.Busy) return;  // don't rebuild mid-turn
            if (_data.sheetCount >= 16) return;

            int spread = book.TurnedCount;

            _data.sheetCount = Mathf.Min(16, _data.sheetCount + 1);
            JournalStore.Save(_data);

            book.settings.sheetCount = _data.sheetCount;
            book.Build();
            book.RestoreSpread(spread);
        }

        public void ClearVisiblePage()
        {
            if (book == null || book.Pages == null) return;

            if (book.TurnedCount > 0 && book.TurnedCount - 1 < book.Sheets.Count)
            {
                var left = book.Sheets[book.TurnedCount - 1];
                if (left.BackPage != null) { left.BackPage.Clear(); PageRenderer.Render(left.BackPage); }
            }
            if (book.TurnedCount < book.Sheets.Count)
            {
                var right = book.Sheets[book.TurnedCount];
                if (right.FrontPage != null) { right.FrontPage.Clear(); PageRenderer.Render(right.FrontPage); }
            }
        }

        public void GoBack()
        {
            // TODO: save journal contents before leaving (Phase 5)
            sceneNavigator.LoadLibrary();
        }
    }
}