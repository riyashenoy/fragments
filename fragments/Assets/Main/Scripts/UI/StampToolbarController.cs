using UnityEngine;
using UnityEngine.UI;
using Fragments.Book;
using Image = UnityEngine.UI.Image;

namespace Fragments.UI
{
    public class StampToolbarController : MonoBehaviour
    {
        [Header("References")]
        public BookDragInput bookInput;

        [Header("Tool Buttons")]
        public Button stickerBtn;
        public Button tapeBtn;
        public Button photoBtn;
        public Outline stickerOutline;
        public Outline tapeOutline;
        public Outline photoOutline;

        [Header("Draw/Text Buttons")]
        public Button drawBtn;
        public Button textBtn;
        public Outline drawOutline;
        public Outline textOutline;

        [Header("Color Swatches")]
        public Button[] colorSwatches;
        public string[] colorHexValues;

        void Start()
        {
            stickerBtn.onClick.AddListener(() => SetTool("sticker"));
            tapeBtn.onClick.AddListener(() => SetTool("tape"));
            photoBtn.onClick.AddListener(() => SetTool("photo"));
            drawBtn.onClick.AddListener(() => SetTool("draw"));
            textBtn.onClick.AddListener(() => SetTool("text"));

            for (int i = 0; i < colorSwatches.Length; i++)
            {
                int idx = i;
                colorSwatches[i].onClick.AddListener(() => SetColor(colorHexValues[idx], idx));
            }

            SetTool("sticker");
            if (colorHexValues.Length > 0) SetColor(colorHexValues[0], 0);
        }

        void SetTool(string type)
        {
            bookInput.activeStampType = type;
            bookInput.drawModeActive = (type == "draw");
            bookInput.textModeActive = (type == "text");

            if (stickerOutline) stickerOutline.enabled = type == "sticker";
            if (tapeOutline) tapeOutline.enabled = type == "tape";
            if (photoOutline) photoOutline.enabled = type == "photo";
            if (drawOutline) drawOutline.enabled = type == "draw";
            if (textOutline) textOutline.enabled = type == "text";
        }

        void SetColor(string hex, int idx)
        {
            bookInput.activeStampColorHex = hex;
            for (int i = 0; i < colorSwatches.Length; i++)
                colorSwatches[i].transform.localScale = Vector3.one * (i == idx ? 1.3f : 1f);
        }
    }
}