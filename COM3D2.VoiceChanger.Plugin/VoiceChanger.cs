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

        public void LoadVoice(string voiceFileName, bool cancelAll = false)
        {
            // TODO: Cancel all before
            string oggFileName = Path.GetFileNameWithoutExtension(voiceFileName).ToLower() + ".ogg";
            if (!voiceWait.Contains(oggFileName) && !cacheAudioClip.ContainsKey(oggFileName))
            {
                inferClient.SendVoice(oggFileName);
                voiceWait.Add(oggFileName);
            }
        }

        public AudioClip getVoiceClip(string voiceFileName)
        {
            string oggFileName = Path.GetFileNameWithoutExtension(voiceFileName).ToLower() + ".ogg";
            if (cacheAudioClip.TryGetValue(oggFileName, out AudioClip audioClip))
            {
                return audioClip;
            }
            else if (voiceWait.Contains(oggFileName))
            {
                while (!cacheAudioClip.ContainsKey(oggFileName))
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
