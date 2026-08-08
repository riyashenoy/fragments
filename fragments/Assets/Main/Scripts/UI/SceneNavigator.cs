using UnityEngine;
using UnityEngine.SceneManagement;
using Fragments.Data;

namespace Fragments.UI
{
    public class SceneNavigator : MonoBehaviour
    {
        // ---- scene loads ----

        public void LoadTitle() => SceneManager.LoadScene("TitleScene");
        public void LoadLibrary() => SceneManager.LoadScene("LibraryScene");
        public void LoadCreateJournal() => SceneManager.LoadScene("CreateJournalScene");
        public void LoadJournaling() => SceneManager.LoadScene("JournalingScene");

        // ---- slot routing (called by LibraryManager) ----

        /// <summary>
        /// Filled slot: set the session ID and jump straight to journaling.
        /// </summary>
        public void OpenExistingJournal(string journalId)
        {
            JournalSession.CurrentId = journalId;
            LoadJournaling();
        }

        /// <summary>
        /// Empty slot: go to the creation screen.
        /// </summary>
        public void CreateNewJournal()
        {
            LoadCreateJournal();
        }
    }
}