using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Fragments.DesignSystem
{
    public class InteractiveElement : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler
    {
        public FragmentsTheme theme;
        public bool scaleOnHover = true;
        public bool useShadow = true;
        public bool useGlow = false;
        [Tooltip("Optional tooltip text shown on hover")]
        public string tooltip = "";

        Vector3 baseScale;
        Shadow shadow;
        Outline glow;
        Coroutine anim;
        bool hovered;

        void Awake()
        {
            baseScale = transform.localScale;
            if (useShadow)
            {
                shadow = GetComponent<Shadow>();
                if (shadow == null) shadow = gameObject.AddComponent<Shadow>();
                shadow.effectColor = theme != null ? theme.shadowSoft : new Color(0,0,0,0.28f);
                shadow.effectDistance = new Vector2(0f, theme != null ? -theme.shadowDistance : -6f);
            }
            if (useGlow)
            {
                glow = GetComponent<Outline>();
                if (glow == null) glow = gameObject.AddComponent<Outline>();
                glow.effectColor = theme != null ? theme.hoverGlow : new Color(1f,0.95f,0.75f,0.35f);
                glow.effectDistance = new Vector2(theme != null ? theme.hoverGlowRadius : 8f, theme != null ? -theme.hoverGlowRadius : -8f);
                glow.enabled = false;
            }
        }

        public void OnPointerEnter(PointerEventData e)
        {
            hovered = true;
            if (scaleOnHover) AnimateScale(baseScale * theme.hoverScale, theme.durFast);
            if (glow) glow.enabled = true;
            if (!string.IsNullOrEmpty(tooltip)) TooltipController.Show(tooltip, transform.position);
        }
        public void OnPointerExit(PointerEventData e)
        {
            hovered = false;
            if (scaleOnHover) AnimateScale(baseScale, theme.durFast);
            if (glow) glow.enabled = false;
            TooltipController.Hide();
        }
        public void OnPointerDown(PointerEventData e)
        {
            if (scaleOnHover) AnimateScale(baseScale * theme.pressScale, theme.durFast * 0.5f);
        }
        public void OnPointerUp(PointerEventData e)
        {
            if (scaleOnHover) AnimateScale(hovered ? baseScale * theme.hoverScale : baseScale, theme.durFast);
        }

        void AnimateScale(Vector3 target, float dur)
        {
            if (anim != null) StopCoroutine(anim);
            anim = StartCoroutine(ScaleTo(target, dur));
        }

        System.Collections.IEnumerator ScaleTo(Vector3 target, float dur)
        {
            var start = transform.localScale;
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float k = theme.easeOut.Evaluate(Mathf.Clamp01(t / dur));
                transform.localScale = Vector3.LerpUnclamped(start, target, k);
                yield return null;
            }
            transform.localScale = target;
        }
    }
}
