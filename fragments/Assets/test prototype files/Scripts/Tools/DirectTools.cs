using System;
using System.Threading.Tasks;
using UnityEngine;
using Found.Core;
using Found.Scrap;

namespace Found.Tools
{
    /// <summary>
    /// Move / Pen / Eraser act directly on the journal, not on the environment, so they
    /// don't use the pinch-selection gesture. They're thin because the heavy lifting
    /// lives on ScrapFragment (grab/resize/rotate) and DrawSurface (strokes).
    /// </summary>
    public class MoveTool : MonoBehaviour, ITool
    {
        public ToolId Id => ToolId.Move;
        public bool UsesEnvironmentSelection => false;
        public void OnActivate() => FragmentEditGizmos.SetEnabled(true);
        public void OnDeactivate() => FragmentEditGizmos.SetEnabled(false);
        public void OnSelectionComplete(in EnvironmentSelection s) { }
    }

    public class PenTool : MonoBehaviour, ITool
    {
        public DrawSurface[] surfaces;    // one per page/cover; ink lands on the nearest
        public Color inkColor = new(0.18f, 0.14f, 0.10f);
        [Range(0.0005f, 0.01f)] public float width = 0.0025f;

        public ToolId Id => ToolId.Pen;
        public bool UsesEnvironmentSelection => false;
        public void OnActivate()
        {
            foreach (var s in surfaces) if (s) s.SetPen(inkColor, width, true);
            FoundEvents.Toast("Write or draw straight onto the page.");
        }
        public void OnDeactivate() { foreach (var s in surfaces) if (s) s.SetPen(inkColor, width, false); }
        public void OnSelectionComplete(in EnvironmentSelection s) { }
    }

    public class EraserTool : MonoBehaviour, ITool
    {
        public DrawSurface[] surfaces;
        public ToolId Id => ToolId.Eraser;
        public bool UsesEnvironmentSelection => false;
        public void OnActivate()
        {
            foreach (var s in surfaces) if (s) s.SetEraseMode(true);
            FoundEvents.Toast("Rub out strokes, or point at a scrap and pinch to remove it.");
        }
        public void OnDeactivate() { foreach (var s in surfaces) if (s) s.SetEraseMode(false); }
        public void OnSelectionComplete(in EnvironmentSelection s) { }
    }

    // ---- small collaborators the tools reference. Flesh these out to taste; they're
    //      deliberately minimal so the framework compiles and runs today. --------------

    /// <summary>Tints the active journal page or cover (color sampler target).</summary>
    public class JournalPageColor : MonoBehaviour
    {
        public Renderer[] pages;      // assign page/cover renderers; index 0 = cover
        [HideInInspector] public int activeIndex = 1;
        public void ApplyToActive(Color c)
        {
            if (pages == null || pages.Length == 0) return;
            int i = Mathf.Clamp(activeIndex, 0, pages.Length - 1);
            if (pages[i]) pages[i].material.color = c;
        }
    }

    /// <summary>A drawable render-texture surface on a page. Hook to your stylus/finger raycast.</summary>
    public class DrawSurface : MonoBehaviour
    {
        public void SetPen(Color c, float w, bool active) { /* set brush + enable input */ }
        public void SetEraseMode(bool on) { /* toggle erase brush */ }
    }

    /// <summary>Enables/disables the per-fragment edit handles when Move is active.</summary>
    public static class FragmentEditGizmos
    {
        public static bool Enabled { get; private set; }
        public static void SetEnabled(bool v) => Enabled = v;
    }

    /// <summary>World-space panel that asks Polaroid / Borderless / Torn + caption.</summary>
    public class PhotoStyleChooser : MonoBehaviour
    {
        public void Show(Texture2D preview, Action<FragmentFactory.PhotoStyle, string> onChoose)
        {
            // Wire buttons to call onChoose(style, captionField.text). Fallback auto-picks Polaroid.
            onChoose?.Invoke(FragmentFactory.PhotoStyle.Polaroid, "");
        }
    }

    /// <summary>World-space panel that asks die-cut / none / holo / paper edge.</summary>
    public class StickerEdgeChooser : MonoBehaviour
    {
        public void Show(Texture2D preview, Action<StickerEdge> onChoose)
            => onChoose?.Invoke(StickerEdge.DieCut);
    }

    /// <summary>Plays the "lift from reality" peel before a sticker drops in.</summary>
    public class PeelAnimation : MonoBehaviour
    {
        public float seconds = 0.8f;
        public async Task Play(Pose at, Texture2D tex)
        {
            // Spawn a temporary quad at `at`, tween scale/rotation, then destroy.
            // Await the duration so the tool waits before committing the sticker.
            await Task.Delay(Mathf.RoundToInt(seconds * 1000));
        }
    }
}
