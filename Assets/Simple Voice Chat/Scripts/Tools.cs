
using System;
using System.Collections.Generic;

namespace SimpleVoiceChat {

    /// <summary>
    /// Some useful methods for working with audio.
    /// </summary>
    public static class Tools {

        /// <summary>
        /// Convert float array of RAW samples into bytes array
        /// </summary>
        public static byte[] FloatToByte(float[] samples) {
            short[] intData = new short[samples.Length];
            byte[] bytesData = new byte[samples.Length * 2];
            for (int i = 0; i < samples.Length; i++) {
                intData[i] = (short)(samples[i] * 32767);
                byte[] byteArr = System.BitConverter.GetBytes(intData[i]);
                byteArr.CopyTo(bytesData, i * 2);
            }
            return bytesData;
        }

        /// <summary>
        /// Convert float array of RAW samples into bytes array
        /// </summary>
        public static byte[] FloatToByte(List<float> samples) {
            short[] intData = new short[samples.Count];
            byte[] bytesData = new byte[samples.Count * 2];
            for (int i = 0; i < samples.Count; i++) {
                intData[i] = (short)(samples[i] * 32767);
                byte[] byteArr = System.BitConverter.GetBytes(intData[i]);
                byteArr.CopyTo(bytesData, i * 2);
            }
            return bytesData;
        }


        /// <summary>
        /// Converts list of bytes to float array by using 32767 rescale factor
        /// </summary>
        public static float[] ByteToFloat(byte[] data) {
            int length = data.Length / 2;
            float[] samples = new float[length];
            for (int i = 0; i < length; i++)
                samples[i] = (float)BitConverter.ToInt16(data, i * 2) / 32767;
            return samples;
        }

        /// <summary>
        /// Converts list of bytes to float array by using 32767 rescale factor
        /// </summary>
        public static float[] ByteToFloat(byte[] data, int startIndex, int dataLenght) {
            int length = dataLenght / 2;
            float[] samples = new float[length];
            for (int i = 0; i < length; i++)
                samples[i] = (float)BitConverter.ToInt16(data, startIndex + i * 2) / 32767;
            return samples;
        }
    }
}
