
namespace SimpleVoiceChat {

    /// <summary>
    /// Global settings for Simple Voice Chat.
    /// Change all settings in this file.
    /// </summary>
    public static class Settings {

        /// <summary>
        /// If TRUE, than the recorder will only send data when the voice level is high enough. If FALSE, it will send data constantly.
        /// </summary>
        public const bool autoVoiceDetection = false;

        /// <summary>
        /// Threshold for auto voice detection.
        /// </summary>
        public const float voiceDetectionThreshold = 0.02f;

        /// <summary>
        /// Should we compress audio stream before sending via network?
        /// This value should be the same for the listener and speaker.
        /// </summary>
        public const bool compression = true;

        /// <summary>
        /// Piece time (milliseconds)
        /// </summary>
        public const int pieceDuration = 150;

        /// <summary>
        /// The sampling rate used for audio recording and playback (8000, 16000, 32000).
        /// Make this value smaller when you have troubles sending big values via network.
        /// </summary>
        public const int sampleRate = 16000;

        /// <summary>
        /// Size of data which is sent via network.
        /// </summary>
        public const int pieceSize = (int)(sampleRate * ((float)pieceDuration / 1000f)); // send with interval ~ pieceDuration ms

        /// <summary>
        /// What is size of audio clip, used by microphone (seconds). Audio clip is looped and rewritten from beginning when overflowed.
        /// </summary>
        public const int audioClipDuration = 1;
    }
}