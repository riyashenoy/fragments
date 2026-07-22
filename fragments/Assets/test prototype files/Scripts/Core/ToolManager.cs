using System;
using System.Collections.Generic;
using UnityEngine;

namespace Found.Core
{
    /// <summary>
    /// The equivalent of the web prototype's setTool()/active-tool state. Holds the
    /// registered tools, tracks the active one, routes the pinch-selection gesture to
    /// tools that want it, and raises an event the UI toolbar can subscribe to for
    /// highlighting the active button.
    /// </summary>
    public class ToolManager : MonoBehaviour
    {
        public static ToolManager Instance { get; private set; }

        [Tooltip("Optional: the tool selected on start.")]
        public ToolId startTool = ToolId.Move;

        public event Action<ToolId> ActiveToolChanged;

        readonly Dictionary<ToolId, ITool> _tools = new();
        ITool _active;

        public ITool Active => _active;
        public ToolId ActiveId => _active != null ? _active.Id : ToolId.Move;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;

            // Auto-register any ITool components found on this object or its children.
            foreach (var mb in GetComponentsInChildren<MonoBehaviour>(true))
                if (mb is ITool t) _tools[t.Id] = t;
        }

        void Start() => SetTool(startTool);

        public void SetTool(ToolId id)
        {
            if (!_tools.TryGetValue(id, out var next))
            {
                Debug.LogWarning($"[FOUND] No tool registered for {id}");
                return;
            }
            if (_active == next) return;

            _active?.OnDeactivate();
            _active = next;
            _active.OnActivate();
            ActiveToolChanged?.Invoke(id);
        }

        /// <summary>Called by PinchSelection when a frame is completed.</summary>
        public void DispatchSelection(in EnvironmentSelection selection)
        {
            if (_active != null && _active.UsesEnvironmentSelection)
                _active.OnSelectionComplete(selection);
        }

        /// <summary>True when the active tool should arm the pinch-selection gesture.</summary>
        public bool WantsSelection => _active != null && _active.UsesEnvironmentSelection;
    }
}
