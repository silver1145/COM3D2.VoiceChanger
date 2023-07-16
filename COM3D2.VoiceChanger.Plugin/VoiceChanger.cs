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
        private CacheHashSet<string> voiceWait;
        private InferClient inferClient;

        public VoiceChanger()
        {
            cacheAudioClip = new CacheDictionary<string, AudioClip>(cacheSize);
            voiceWait = new CacheHashSet<string>();
            inferClient = new InferClient();
            inferClient.Callback += HandleVoice;
        }

        public bool CheckPrepared(string voice)
        {
            string oggFileName = Path.ChangeExtension(voice, ".ogg").ToLower();
            return cacheAudioClip.ContainsKey(oggFileName);
        }

        public void Clear()
        {
            ClearWait();
            ClearCache();
        }

        public void ClearWait()
        {
            voiceWait.Clear();
        }

        public void ClearCache()
        {
            cacheAudioClip.Clear();
        }

        public void SetServerUrl(string url)
        {
            inferClient.SetServerUrl(url);
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
            if (voiceWait.Remove(inferVoice.name))
            {
                cacheAudioClip.Add(inferVoice.name, OGGParser.FromVoiceData(inferVoice));
            }
        }
    }
}
