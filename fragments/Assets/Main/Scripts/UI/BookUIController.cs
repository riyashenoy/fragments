using UnityEngine;
using UnityEngine.UI;
using Found.Journal3D;
using Debug = UnityEngine.Debug;

namespace Fragments.UI
{
    public class BookUIController : MonoBehaviour
    {
        [Header("References")]
        public Journal journal;
        public Button openButton;
        public Button prevButton;
        public Button nextButton;

        void Start()
        {
            if (openButton != null)
                openButton.onClick.AddListener(OnOpen);
            if (prevButton != null)
                prevButton.onClick.AddListener(OnPrev);
            if (nextButton != null)
                nextButton.onClick.AddListener(OnNext);
        }

        void OnOpen()
        {
            if (journal != null && journal.IsClosed)
                journal.Open();
        }

        void OnPrev()
        {
            if (journal != null)
                journal.Prev();
        }

        void OnNext()
        {
            if (journal != null)
                journal.Next();
        }
    }
}