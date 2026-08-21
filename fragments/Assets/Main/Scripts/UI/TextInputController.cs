using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

namespace Fragments.UI
{
    public class TextInputController : MonoBehaviour
    {
        public GameObject panel;
        public TMP_InputField inputField;
        public Button okButton;
        public Button cancelButton;

        UnityAction<string> _onSubmit;
        UnityAction _onCancel;

        void Awake()
        {
            panel.SetActive(false);
            okButton.onClick.AddListener(Submit);
            cancelButton.onClick.AddListener(Cancel);
        }

        public void Prompt(UnityAction<string> onSubmit, UnityAction onCancel = null)
        {
            _onSubmit = onSubmit;
            _onCancel = onCancel;
            inputField.text = "";
            panel.SetActive(true);
            inputField.Select();
            inputField.ActivateInputField();
        }

        void Submit()
        {
            var t = inputField.text.Trim();
            if (!string.IsNullOrEmpty(t)) _onSubmit?.Invoke(t);
            _onSubmit = null;
            _onCancel = null;
            panel.SetActive(false);
        }

        void Cancel()
        {
            _onCancel?.Invoke();
            _onSubmit = null;
            _onCancel = null;
            panel.SetActive(false);
        }
    }
}
