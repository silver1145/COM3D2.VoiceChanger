using COM3D2.VoiceChanger.Plugin.Infer;
using COM3D2.VoiceChanger.Plugin.Preloader;
using COM3D2.VoiceChanger.Plugin.Utils;
using System;
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
        private BasePreloader preloader;
        private object preloaderLock;
        public int cacheCount => cacheAudioClip.Count();
        public bool connected => inferClient.connected;

        public VoiceChanger()
        {
            cacheAudioClip = new CacheDictionary<string, AudioClip>(cacheSize);
            voiceWait = new CacheHashSet<string>();
            inferClient = new InferClient();
            inferClient.Callback += HandleVoice;
            preloader = new LinerPreloader(cacheSize, voiceWait, inferClient.Send);
            preloaderLock = new object();
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

        public void SetPreloaderType(PreloaderType preloaderType)
        {
            if (preloader == null && preloaderType == PreloaderType.NonePreloader)
            {
                return;
            }
            else if (preloader != null && preloaderType == preloader.preloaderType)
            {
                return;
            }
            else if (preloader != null)
            {
                lock (preloaderLock)
                {
                    preloader.Dispose();
                    preloader = null;
                }
            }
            lock (preloaderLock)
            {
                    
                switch (preloaderType)
                {
                    case PreloaderType.NonePreloader:
                        break;
                    case PreloaderType.LinerPreloader:
                        preloader = new LinerPreloader(cacheSize, voiceWait, inferClient.Send);
                        break;
                    // TODO: Other Preloader
                }
            }
        }

        public void LoadVoice(string voice)
        {
            string oggFileName = Path.ChangeExtension(voice, ".ogg").ToLower();
            if (!voiceWait.Contains(oggFileName) && !cacheAudioClip.ContainsKey(oggFileName))
            {
                inferClient.SendVoice(oggFileName);
                voiceWait.Add(oggFileName);
            }
            lock (preloaderLock)
            {
                if (preloader != null)
                {
                    preloader.Preload(oggFileName);
                }
            }
        }

        public AudioClip getVoiceClip(string voice, bool wait = true, int timeout = 0)
        {
            string oggFileName = Path.GetFileNameWithoutExtension(voice).ToLower() + ".ogg";
            if (cacheAudioClip.TryGetValue(oggFileName, out AudioClip audioClip))
            {
                return audioClip;
            }
            else if (voiceWait.Contains(oggFileName))
            {
                int lastTime = 0;
                while (!cacheAudioClip.ContainsKey(oggFileName) && wait)
                {
                    Thread.Sleep(100);
                    lastTime += 100;
                    if (timeout > 0 && lastTime > timeout)
                    {
                        return null;
                    }
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
