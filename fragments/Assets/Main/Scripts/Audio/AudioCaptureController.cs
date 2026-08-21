using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fragments.Book;
using Debug = UnityEngine.Debug;

namespace Fragments.Audio
{
    public class AudioCaptureController : MonoBehaviour
    {
        public BookDragInput bookInput;
        public Button recordButton;
        public TMP_Text recordLabel; // shows "Record" / "Stop (5s)" / "Recording..."

        AudioClip _currentClip;
        bool _isRecording;
        string _micName;
        float _recordStartTime;

        void Awake()
        {
            recordButton.onClick.AddListener(ToggleRecord);
            if (Microphone.devices.Length > 0)
                _micName = Microphone.devices[0];
        }

        void Update()
        {
            if (_isRecording)
            {
                var elapsed = Time.time - _recordStartTime;
                if (recordLabel != null)
                    recordLabel.text = $"Stop ({elapsed:F1}s)";
            }
        }

        void ToggleRecord()
        {
            if (_isRecording) StopRecording();
            else StartRecording();
        }

        void StartRecording()
        {
            if (string.IsNullOrEmpty(_micName))
            {
                Debug.LogWarning("No microphone available");
                return;
            }
            _currentClip = Microphone.Start(_micName, false, 60, 44100);
            _isRecording = true;
            _recordStartTime = Time.time;
            if (recordLabel != null)
                recordLabel.text = "Recording...";
        }

        void StopRecording()
        {
            if (!_isRecording) return;

            int position = Microphone.GetPosition(_micName);
            Microphone.End(_micName);
            _isRecording = false;
            if (recordLabel != null)
                recordLabel.text = "Record";

            // Trim to actual recorded length
            if (_currentClip != null && position > 0)
            {
                float[] samples = new float[position * _currentClip.channels];
                _currentClip.GetData(samples, 0);
                var trimmed = AudioClip.Create("recording", position, _currentClip.channels,
                    _currentClip.frequency, false);
                trimmed.SetData(samples, 0);
                _currentClip = trimmed;
            }

            // Next page click places this clip
            if (bookInput != null)
            {
                bookInput.pendingAudioClip = _currentClip;
                bookInput.activeStampType = "audio";
                bookInput.drawModeActive = false;
                bookInput.textModeActive = false;
            }
        }
    }
}
