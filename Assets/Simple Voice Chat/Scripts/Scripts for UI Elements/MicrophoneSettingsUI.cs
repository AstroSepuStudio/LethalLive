using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SimpleVoiceChat {

    /// <summary>
    /// Simple out-of-the-box UI panel containing audio and microphone settings.
    /// </summary>
    public class MicrophoneSettingsUI : MonoBehaviour {

        [SerializeField] private string currentMicrophone;

        [Header("Links")]
        [SerializeField] private TMP_Dropdown audioDevicesDropDown;
        [SerializeField] private TMP_Text stateLabel;

        [Header("Test Microphone")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip audioCLip;

        void OnEnable() {
            RefreshPermissionStatusLabel();
            RefreshMicrophones();

            StopAllCoroutines();
            StartCoroutine(RefreshingPermissionLabel());
        }

        // Called by some UI element.
        public void OnAudioDeviceDropDownChanged() {
            Debug.Log($"[Mic Settings] OnAudioDeviceDropDownChanged: {audioDevicesDropDown.value}", gameObject);
            currentMicrophone = Microphone.devices[audioDevicesDropDown.value];
            if (Recorder.Instance != null)
                Recorder.Instance.SetMicrophone(currentMicrophone);
            else 
                Debug.Log($"[Audio Settings UI] Can't find active 'Recorder' to setup microphone device.");
        }

        IEnumerator RefreshingPermissionLabel() {
            yield return new WaitForSeconds(1f);
            RefreshPermissionStatusLabel();
        }

        void RefreshPermissionStatusLabel() {
            stateLabel.text = HasMicrophonePermission()
                ? "Microphone permission: GRANTED"
                : "Microphone permission: DENIED";
        }

        // Called by some UI element.
        public void RequestMicrophonePermission() {
            if (!HasMicrophonePermission()) {
#if UNITY_IOS
                Application.RequestUserAuthorization(UserAuthorization.Microphone);
#elif UNITY_ANDROID
                UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Microphone);
#endif
            }
        }

        public bool HasMicrophonePermission() {
#if UNITY_IOS
            return Application.HasUserAuthorization(UserAuthorization.Microphone);
#elif UNITY_ANDROID
			return UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone);
#else
            return true;
#endif
        }

        // Called by some UI element.
        public void RefreshMicrophones() {
            // Setup drop microphones list.
            Debug.Log($"[Mic Settings] Setup audio devices list.", gameObject);
            List<TMP_Dropdown.OptionData> audioOptions = new List<TMP_Dropdown.OptionData>(Microphone.devices.Length);
            for (int i = 0; i < Microphone.devices.Length; i++) {
                Debug.Log($"{i} device: {Microphone.devices[i]}", gameObject);
                audioOptions.Add(new TMP_Dropdown.OptionData(Microphone.devices[i]));
            }

            audioDevicesDropDown.options = audioOptions;
            audioDevicesDropDown.RefreshShownValue();
        }

        public void StartRec() {
            Debug.Log($"[Mic Settings] Start recording");
            audioCLip = Microphone.Start(currentMicrophone, false, 5, 44100);
        }

        public void StopRec() {
            Debug.Log($"[Mic Settings] Stop recording");
            Microphone.End(currentMicrophone);
            if (audioCLip != null) {
                audioSource.clip = audioCLip;
                audioSource.Play();
            }
        }
    }
}