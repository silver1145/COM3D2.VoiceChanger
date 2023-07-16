using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace COM3D2.VoiceChanger.Plugin
{
    internal class VoiceChangerConfig
    {
        internal ConfigEntry<string> serverUrl;
        internal ConfigEntry<int> inferTimeout;
        internal ConfigEntry<bool> noWait;
        internal ConfigEntry<bool> enableVoice;
        internal ConfigEntry<bool> enableSe;

        public VoiceChangerConfig(ConfigFile Config)
        {
            serverUrl = Config.Bind("Connection Setting", "ServerUrl", "ws://127.0.0.1:11451/infer/ws", "Infer Server Url");
            inferTimeout = Config.Bind("Infer Setting", "Timeout", 100000, "Infer Timeout (ms)");
            noWait = Config.Bind("Infer Setting", "NoWait", false, "Ignore Unreceived Voice");
            // TODO: Preloader
            enableVoice = Config.Bind("Type Filter", "Voice", true, "Enable Voice Infer");
            enableSe = Config.Bind("Type Filter", "Se", true, "Enable Se Infer");
        }
    }
}
