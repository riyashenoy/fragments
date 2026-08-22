using Fragments.Data;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

namespace Fragments.UI
{
    public class JournalSlotView : MonoBehaviour
    {
        [Header("References")]
        public Image sketchyBorder;
        public GameObject emptyPlus;
        public Image filledJournal;
        public Button button;

        [Header("Style")]
        public float tiltRange = 8f;   // degrees each way for variety

        JournalData _data;
        UnityAction<JournalData> _onFilledClick;
        UnityAction _onEmptyClick;

        public void BindEmpty(UnityAction onClick)
        {
            _data = null;
            _onEmptyClick = onClick;
            emptyPlus.SetActive(true);
            filledJournal.gameObject.SetActive(false);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => _onEmptyClick?.Invoke());
        }

        public void BindFilled(JournalData data, UnityAction<JournalData> onClick)
        {
            _data = data;
            _onFilledClick = onClick;
            emptyPlus.SetActive(false);
            filledJournal.gameObject.SetActive(true);

            if (ColorUtility.TryParseHtmlString(data.coverColorHex, out Color c))
                filledJournal.color = c;

            // Deterministic tilt per journal so it doesn't change on rebuild
            int seed = data.id.GetHashCode();
            UnityEngine.Random.InitState(seed);
            float tilt = UnityEngine.Random.Range(-tiltRange, tiltRange);
            filledJournal.transform.localRotation = Quaternion.Euler(0f, 0f, tilt);

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => _onFilledClick?.Invoke(_data));
        }
    }
}