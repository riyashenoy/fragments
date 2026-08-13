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

        [Header("Edge Material (the cream paper edge)")]
        public Material pageEdge;

        [Header("Cover Material (base — gets tinted at runtime)")]
        public Material coverBaseMaterial;

        [Header("Navigation")]
        public SceneNavigator sceneNavigator;

        JournalData _data;
        Journal _journal;

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
                Debug.LogError("[Fragments] Journal not found — returning to library.");
                sceneNavigator.LoadLibrary();
                return;
            }

            _data.lastOpenedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            JournalStore.Save(_data);

            SpawnAndStyleBook();

            Debug.Log("[Fragments] Loaded: " + _data.journalName +
                      " | Cover: " + _data.coverColorHex +
                      " | Pattern: " + _data.pagePattern);
        }

        void SpawnAndStyleBook()
        {
            if (journalPrefab == null)
            {
                Debug.LogError("[Fragments] No journal prefab assigned!");
                return;
            }

            // Spawn the prefab
            Vector3 spawnPos = bookSpawnPoint != null ? bookSpawnPoint.position : new Vector3(0f, -0.15f, 0.4f);
            Quaternion spawnRot = bookSpawnPoint != null ? bookSpawnPoint.rotation : Quaternion.Euler(30f, 0f, 0f);

            GameObject bookObj = Instantiate(journalPrefab, spawnPos, spawnRot);
            _journal = bookObj.GetComponent<Journal>();

            if (_journal == null)
            {
                Debug.LogError("[Fragments] Journal prefab is missing a Journal component!");
                return;
            }

            // Apply cover color
            if (coverBaseMaterial != null &&
                ColorUtility.TryParseHtmlString(_data.coverColorHex, out Color coverCol))
            {
                // Runtime copy so we don't modify the asset
                Material coverInstance = new Material(coverBaseMaterial);
                coverInstance.color = coverCol;
                _journal.SetShellMaterial(coverInstance);
            }

            // Apply page pattern
            Material pageMat = _data.pagePattern switch
            {
                "dotted" => pageDotted,
                "striped" => pageStriped,
                _ => pagePlain
            };
            if (pageMat != null)
                _journal.SetPageMaterial(pageMat);

            // Wire book UI controller
            if (bookUIController != null)
                bookUIController.journal = _journal;
        }

        public void GoBack()
        {
            // TODO: save journal contents before leaving (Phase 5)
            sceneNavigator.LoadLibrary();
        }
    }
}