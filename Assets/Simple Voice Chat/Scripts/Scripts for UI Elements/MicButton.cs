
using UnityEngine;
using UnityEngine.UI;

namespace SimpleVoiceChat {

    /// <summary>
    /// An UI button that controls the microphone and shows its current state (on/off).
    /// </summary>
    public class MicButton : MonoBehaviour {

        [SerializeField] private Image image;
        [SerializeField] private Sprite activeMicSprite;
        [SerializeField] private Sprite notActiveMicSprite;

        void Awake() {
            Recorder.OnRecordingStart += OnRecordingStarted;
            Recorder.OnRecordingEnd += OnRecordingEnded;

            if (Recorder.Instance != null) {
                ChangeImage(Recorder.Instance.IsRecording);
            }
        }

        void OnDestroy() {
            Recorder.OnRecordingStart -= OnRecordingStarted;
            Recorder.OnRecordingEnd -= OnRecordingEnded;
        }

        // Called by some UI element.
        public void TriggerMicrophone() {
            if (Recorder.Instance != null)
                Recorder.Instance.SwitchState();
        }

        void OnRecordingStarted() {
            ChangeImage(true);
        }

        void OnRecordingEnded() {
            ChangeImage(false);
        }

        void ChangeImage(bool isRecordingActive) {
            image.sprite = isRecordingActive ? activeMicSprite : notActiveMicSprite;
        }
    }
}