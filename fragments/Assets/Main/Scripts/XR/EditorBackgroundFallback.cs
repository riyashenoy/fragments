using UnityEngine;

namespace Fragments.XR
{
    /// <summary>
    /// When running in Editor (no real passthrough available), fills the transparent
    /// camera background with a neutral gray so scenes remain viewable.
    /// On device with passthrough, does nothing.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class EditorBackgroundFallback : MonoBehaviour
    {
        public Color editorFallbackColor = new Color(0.11f, 0.10f, 0.09f, 1f);

        void Awake()
        {
            var cam = GetComponent<Camera>();
#if UNITY_EDITOR
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = editorFallbackColor;
#else
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0, 0, 0, 0);
#endif
        }
    }
}