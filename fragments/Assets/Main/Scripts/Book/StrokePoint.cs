namespace Fragments.Book
{
    [System.Serializable]
    public class StrokePoint
    {
        public float u;
        public float v;
        public float pressure = 1f;  // 0-1, thickness multiplier
    }
}
