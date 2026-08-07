// ─────────────────────────────────────────────────────────────────────────────
//  JournalFragmentBridge.cs
//
//  OPTIONAL — needs the FOUND tool framework (Found.Scrap.FragmentFactory) from the
//  earlier package. It keeps FragmentFactory.placementParent pointed at whichever page
//  is currently face-up, so every scrap the tools create lands on the page you're
//  looking at and turns with it. If you're only using the journal on its own, skip this
//  file.
//
//  Wire it up: on the Journal object, add this component, drag in the Journal and your
//  FragmentFactory. Then hook Journal.onActiveSurfaceChanged → this.SetSurface in the
//  inspector (or it self-subscribes in Awake).
// ─────────────────────────────────────────────────────────────────────────────
using UnityEngine;
using Found.Scrap;

namespace Found.Journal3D
{
    public class JournalFragmentBridge : MonoBehaviour
    {
        public Journal journal;
        public FragmentFactory factory;

        void Awake()
        {
            if (journal != null)
                journal.onActiveSurfaceChanged.AddListener(SetSurface);
        }

        void Start()
        {
            if (journal != null) SetSurface(journal.ActiveSurface);
        }

        public void SetSurface(Transform surface)
        {
            if (factory != null && surface != null)
                factory.placementParent = surface;
        }
    }
}
