using UnityEngine;

namespace Fragments.DesignSystem
{
    [RequireComponent(typeof(CanvasGroup))]
    public class AnimatedPanel : MonoBehaviour
    {
        public FragmentsTheme theme;
        public bool scaleIn = true;
        public float startScale = 0.96f;

        CanvasGroup group;

        void Awake() { group = GetComponent<CanvasGroup>(); }

        void OnEnable()
        {
            group.alpha = 0f;
            if (scaleIn) transform.localScale = Vector3.one * startScale;
            StartCoroutine(FadeIn());
        }

        System.Collections.IEnumerator FadeIn()
        {
            float t = 0f, dur = theme != null ? theme.durMed : 0.28f;
            var startScl = transform.localScale;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float k = theme != null ? theme.easeOut.Evaluate(t / dur) : (t / dur);
                group.alpha = k;
                if (scaleIn) transform.localScale = Vector3.LerpUnclamped(startScl, Vector3.one, k);
                yield return null;
            }
            group.alpha = 1f;
            transform.localScale = Vector3.one;
        }
    }
}
