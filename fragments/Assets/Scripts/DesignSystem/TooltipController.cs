using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace Fragments.DesignSystem
{
    public class TooltipController : MonoBehaviour
    {
        static TooltipController _instance;
        public FragmentsTheme theme;
        public CanvasGroup group;
        public TMP_Text label;
        public RectTransform container;

        void Awake()
        {
            _instance = this;
            if (group) group.alpha = 0f;
        }

        public static void Show(string text, Vector3 worldPos)
        {
            if (_instance == null) return;
            _instance.label.text = text;
            _instance.container.position = worldPos + new Vector3(0f, 40f, 0f);
            _instance.StopAllCoroutines();
            _instance.StartCoroutine(_instance.FadeTo(1f, _instance.theme.durFast));
        }

        public static void Hide()
        {
            if (_instance == null) return;
            _instance.StopAllCoroutines();
            _instance.StartCoroutine(_instance.FadeTo(0f, _instance.theme.durFast));
        }

        System.Collections.IEnumerator FadeTo(float target, float dur)
        {
            float start = group.alpha, t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(start, target, t / dur);
                yield return null;
            }
            group.alpha = target;
        }
    }
}
