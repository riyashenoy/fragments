using UnityEngine;

namespace Fragments.DesignSystem
{
    public class FloatBob : MonoBehaviour
    {
        public float amplitude = 2f;   // pixels
        public float period = 3f;      // seconds
        public float phaseOffset = 0f;

        Vector3 basePos;

        void Start() { basePos = transform.localPosition; }

        void Update()
        {
            float y = Mathf.Sin((Time.time + phaseOffset) * Mathf.PI * 2f / period) * amplitude;
            transform.localPosition = basePos + new Vector3(0f, y, 0f);
        }
    }
}
