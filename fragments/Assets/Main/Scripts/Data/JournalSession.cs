namespace Fragments.Data
{
    /// <summary>
    /// Holds the ID of the journal currently being edited.
    /// Set by the Library before loading the Journaling scene.
    /// Read by the Journaling scene on startup.
    /// Static so it survives scene loads.
    /// </summary>
    public static class JournalSession
    {
        public static string CurrentId { get; set; }
    }
}