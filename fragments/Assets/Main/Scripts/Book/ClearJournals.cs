using UnityEngine;
using UnityEngine.UI;
using Fragments.Data;
using Fragments.UI;

/// <summary>
/// Drop this on a UI Button. Clicking it deletes every saved journal
/// and resets library slots so they can be created again.
/// </summary>
public class ClearJournals : MonoBehaviour
{
    [SerializeField] LibraryManager libraryManager;

    void Awake()
    {
        var button = GetComponent<Button>();
        if (button != null)
            button.onClick.AddListener(ClearAll);
    }

    public void ClearAll()
    {
        JournalStore.DeleteAll();
        JournalSession.CurrentId = null;

        var library = libraryManager != null
            ? libraryManager
            : FindFirstObjectByType<LibraryManager>();

        if (library != null)
            library.Refresh();

        Debug.Log("[Fragments] All journals cleared.");
    }
}
