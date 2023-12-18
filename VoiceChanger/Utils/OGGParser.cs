using System;
using System.IO;
using UnityEngine;


namespace COM3D2.VoiceChanger.Plugin.Utils
{
    internal static class OGGParser
    {
        private static NVorbis.VorbisReader vorbis;

        public static AudioClip FromVoiceData(Voice_Data voice)
        {
            byte[] data = Convert.FromBase64String(voice.file);
            return FromOggData(data, voice.name);
        }

        public static AudioClip FromOggData(byte[] data, string audioClipName = "default.ogg")
        {
            AudioClip audioClip;
            using (MemoryStream oggstream = new(data))
            {
                vorbis = new NVorbis.VorbisReader(oggstream, false);
                int samplecount = (int)(vorbis.SampleRate * vorbis.TotalTime.TotalSeconds);

                audioClip = AudioClip.Create(audioClipName, samplecount, vorbis.Channels, vorbis.SampleRate, false, OnAudioRead);
            }
            return audioClip;
        }

        private static void OnAudioRead(float[] data)
        {
            var f = new float[data.Length];
            vorbis.ReadSamples(f, 0, data.Length);
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = f[i];
            }
        }

        private static void OnAudioSetPosition(int newPosition)
        {
            vorbis.DecodedTime = new TimeSpan(newPosition);
        }
    }
}
