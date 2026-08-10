using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fragments.Data;
using Image = UnityEngine.UI.Image;
using Debug = UnityEngine.Debug;

namespace Fragments.UI
{
    public class JournalingSceneManager : MonoBehaviour
    {
        [Header("Debug Display (temporary — remove in Phase 2)")]
        public TMP_Text journalNameText;
        public Image coverColorPreview;

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

            _data.lastOpenedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            JournalStore.Save(_data);

            if (journalNameText != null)
                journalNameText.text = _data.journalName;

            if (coverColorPreview != null &&
                ColorUtility.TryParseHtmlString(_data.coverColorHex, out Color col))
                coverColorPreview.color = col;

            Debug.Log($"[Fragments] Loaded journal: {_data.journalName} | Cover: {_data.coverColorHex} | Pattern: {_data.pagePattern}");
        }
    }
}