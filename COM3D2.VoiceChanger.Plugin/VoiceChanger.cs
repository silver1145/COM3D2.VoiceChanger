using COM3D2.VoiceChanger.Plugin.Infer;
using COM3D2.VoiceChanger.Plugin.Utils;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;

namespace COM3D2.VoiceChanger.Plugin
{
    internal class VoiceChanger
    {
        private const int cacheSize = 128;
        private CacheDictionary<string, AudioClip> cacheAudioClip;
        private HashSet<string> voiceWait;
        private InferClient inferClient;

        public VoiceChanger()
        {
            cacheAudioClip = new CacheDictionary<string, AudioClip>(cacheSize);
            voiceWait = new HashSet<string>();
            inferClient = new InferClient();
            inferClient.Callback += HandleVoice;
        }

        public bool CheckPrepared(string voice)
        {
            string oggFileName = Path.ChangeExtension(voice, ".ogg").ToLower();
            return cacheAudioClip.ContainsKey(oggFileName);
        }

        public void LoadVoice(string voice, bool cancelAll = false)
        {
            // TODO: Cancel all before
            string oggFileName = Path.ChangeExtension(voice, ".ogg").ToLower();
            if (!voiceWait.Contains(oggFileName) && !cacheAudioClip.ContainsKey(oggFileName))
            {
                inferClient.SendVoice(oggFileName);
                voiceWait.Add(oggFileName);
            }
        }

        public AudioClip getVoiceClip(string voice, bool wait = true)
        {
            string oggFileName = Path.GetFileNameWithoutExtension(voice).ToLower() + ".ogg";
            if (cacheAudioClip.TryGetValue(oggFileName, out AudioClip audioClip))
            {
                return audioClip;
            }
            else if (voiceWait.Contains(oggFileName))
            {
                while (!cacheAudioClip.ContainsKey(oggFileName) && wait)
                {
                    Thread.Sleep(100);
                }
                return cacheAudioClip.Get(oggFileName);
            }
            return null;
        }

        private void HandleVoice(Voice_Data inferVoice)
        {
            voiceWait.Remove(inferVoice.name);
            cacheAudioClip.Add(inferVoice.name, OGGParser.FromVoiceData(inferVoice));
        }
    }
}
