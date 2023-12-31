﻿using BepInEx.Configuration;
using COM3D2.VoiceChanger.Plugin.Preloader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace COM3D2.VoiceChanger.Plugin
{
    internal class VoiceChangerConfig
    {
        internal ConfigEntry<string> serverUrl;
        internal ConfigEntry<int> inferTimeout;
        internal ConfigEntry<bool> noWait;
        internal ConfigEntry<PreloaderType> preloaderT;
        internal ConfigEntry<KeyCode> guiKey;
        internal ConfigEntry<bool> enableNormal;
        internal ConfigEntry<bool> enableYotogi;

        public VoiceChangerConfig(ConfigFile Config)
        {
            serverUrl = Config.Bind("Connection Setting", "ServerUrl", "ws://127.0.0.1:11451/infer/ws", "Infer Server Url");
            inferTimeout = Config.Bind("Infer Setting", "Timeout", 10000, "Infer Timeout (ms)");
            noWait = Config.Bind("Infer Setting", "NoWait", false, "Ignore Unreceived Voice");
            preloaderT = Config.Bind("Infer Setting", "Preloader Type", PreloaderType.LinerPreloader, "Preloader Type");
            guiKey = Config.Bind("Infer Setting", "GUI Key", KeyCode.V, "Key to Enable GUI");
            enableNormal = Config.Bind("Type Filter", "Normal", true, "Enable Normal Voice Infer");
            enableYotogi = Config.Bind("Type Filter", "Yotogi", true, "Enable Yotogi Voice Infer");
        }
    }
}
