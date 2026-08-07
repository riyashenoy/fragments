using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fragments.UI
{
    public class SceneNavigator : MonoBehaviour
    {
        public void LoadTitle() => SceneManager.LoadScene("TitleScene");
        public void LoadLibrary() => SceneManager.LoadScene("LibraryScene");
        public void LoadJournaling() => SceneManager.LoadScene("JournalingScene");
    }
}