
using UnityEngine;

namespace SimpleVoiceChat {

    /// <summary>
    /// It is used for play voice audio from other people.
    /// In usual you need 1 'speaker' for 1 person.
    /// Direct audio stream from the network to this script.
    /// </summary>
    public class Speaker : MonoBehaviour {

        private VoiceBuffer _buffer;
        private AudioSource _source;
        private AudioClip _voiceClip;
        private float _testDelay;

        void Awake() {
            Initialize();
        }

        void Update() {
            if (_testDelay == 0f) {
                if (_buffer.NextVoice_IsReady() && _buffer.NextVoice_TryWrite(ref _voiceClip, out _testDelay)) {
                    _source.Play();
                }
            }
            else if ((_testDelay -= Time.deltaTime) <= 0f) {
                _source.Stop();
                _testDelay = 0f;
            }
        }

        public Speaker Initialize() {
            gameObject.name = "Voice speaker";
            if (_source == null)
                _source = GetComponent<AudioSource>();
            if (_source == null)
                _source = gameObject.AddComponent<AudioSource>();
            _buffer = new VoiceBuffer(out var clip);
            _source.clip = _voiceClip = clip;
            return this;
        }

        /// <summary>
        /// Direct audio stream from the network to this method.
        /// </summary>
        public void ProcessVoiceData(byte[] voiceData) {
            _buffer.Add(Settings.compression ? AudioCompressor.Decompress(voiceData) : voiceData);
        }
    }
}