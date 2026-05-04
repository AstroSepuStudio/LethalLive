using System;
using UnityEngine;

namespace SimpleVoiceChat {

    /// <summary>
    /// It is used to manage voice data during playback.
    /// </summary>
    public class VoiceBuffer {

        private int _curIndex = 0;
        private int _endIndex = 0;

        private float[] _clipBuffer = new float[Settings.sampleRate];
        private float[] _buffer = new float[320000];
        private int _sampleRate => Settings.sampleRate;

        #region Constructors

        public VoiceBuffer() {
            _curIndex = _endIndex = 0;
            _clipBuffer = new float[Settings.sampleRate];
            _buffer = new float[(Settings.sampleRate * 4) * 10];
        }

        public VoiceBuffer(out AudioClip clip) {
            _curIndex = _endIndex = 0;
            _clipBuffer = new float[Settings.sampleRate];
            _buffer = new float[(Settings.sampleRate * 4) * 10];
            clip = AudioClip.Create("BufferedClip_" + Time.time, Settings.sampleRate * Settings.audioClipDuration, 1,
                Settings.sampleRate, false);
        }

        #endregion

        public void Add(byte[] voiceData) {
            float[] voice = Tools.ByteToFloat(voiceData);
            int dataLeft = voice.Length;

            while (dataLeft > 0) {
                if (_endIndex + dataLeft >= _buffer.Length) {
                    int oversize = (_endIndex + dataLeft) - _buffer.Length;
                    Array.Copy(voice, voice.Length - dataLeft, _buffer, _endIndex, dataLeft - oversize);
                    dataLeft -= oversize;
                    _endIndex = 0;
                }
                else {
                    Array.Copy(voice, voice.Length - dataLeft, _buffer, _endIndex, dataLeft);
                    _endIndex += dataLeft;
                    break;
                }
            }
        }

        public void Clear() {
            _curIndex = _endIndex = 0;
            Array.Clear(_buffer, 0, _buffer.Length);
            Array.Clear(_clipBuffer, 0, _clipBuffer.Length);
        }

        public bool NextVoice_IsReady() {
            return _curIndex != _endIndex;
        }

        public bool NextVoice_TryWrite(ref AudioClip clip, out float playTime) {
            if (TryWriteNextClipBuffer(out playTime)) {
                clip.SetData(_clipBuffer, 0);
                return true;
            }
            else {
                return false;
            }
        }

        public bool NextVoice_GetData(out float[] audio, out float playTime) {
            if (TryWriteNextClipBuffer(out playTime)) {
                audio = _clipBuffer;
                return true;
            }
            else {
                audio = null;
                return false;
            }
        }

        private bool TryWriteNextClipBuffer(out float playTime) {
            if (_curIndex == _endIndex) {
                playTime = 0f;
                return false;
            }

            Array.Clear(_clipBuffer, 0, _clipBuffer.Length);

            int copyIndex = 0;
            int dataLeft = _curIndex < _endIndex ? _endIndex - _curIndex : (_buffer.Length - _curIndex) + _endIndex;
            dataLeft = Mathf.Clamp(dataLeft, 0, _clipBuffer.Length);

            playTime = ((float)dataLeft / (float)_sampleRate);

            while (dataLeft > 0) {
                if (_curIndex + dataLeft >= _buffer.Length) {
                    int oversize = (_curIndex + dataLeft) - _buffer.Length;
                    Array.Copy(_buffer, _curIndex, _clipBuffer, copyIndex, dataLeft - oversize);
                    dataLeft -= oversize;
                    copyIndex += oversize;
                    _curIndex = 0;
                }
                else {
                    Array.Copy(_buffer, _curIndex, _clipBuffer, copyIndex, dataLeft);
                    _curIndex += dataLeft;
                    break;
                }
            }

            return true;
        }
    }
}