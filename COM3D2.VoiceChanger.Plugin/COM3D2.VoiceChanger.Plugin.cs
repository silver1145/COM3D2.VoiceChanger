using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.IO;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using UnityEngine;
using UnityEngine.UI;

[assembly: AssemblyVersion(COM3D2.VoiceChanger.Plugin.PluginInfo.PLUGIN_VERSION + ".*")]
[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]


namespace COM3D2.VoiceChanger.Plugin
{
    public static class PluginInfo
    {
        public const string PLUGIN_GUID = "COM3D2.VoiceChanger.Plugin";
        public const string PLUGIN_NAME = "VoiceChanger.Plugin";
        public const string PLUGIN_VERSION = "1.0";
    }

    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public sealed class Main : BaseUnityPlugin
    {
        public static Main Instance { get; private set; }
        private ManualLogSource _Logger => base.Logger;
        internal static new ManualLogSource Logger => Instance?._Logger;
        internal static VoiceChanger voiceChanger = new();

        // current wait
        internal static string curVoice = null;
        internal static BaseKagManager.ExecWaitData curWaitData = null;
        internal static bool curPrepared => voiceChanger.CheckPrepared(curVoice);
        internal static bool isWait => (
            curVoice != null &&
            curWaitData != null &&
            (!curWaitData.use || curWaitData.wait_time <= GameMain.tick_count - curWaitData.start_tick_count)
        );

        //config
        internal static VoiceChangerConfig vcConfig;
        internal static int timeout => vcConfig.inferTimeout.Value;
        internal static bool noWait => vcConfig.noWait.Value || Input.GetKey(KeyCode.LeftControl) || !voiceChanger.connected;
        internal static bool enableVoice => vcConfig.enableVoice.Value;
        internal static bool enableSe => vcConfig.enableSe.Value;
        internal static KeyCode guiKey => vcConfig.guiKey.Value;
        
        // UI
        private static bool showGUI = false;
        private GUIStyle colorStyle;
        private const int WIDTH = 200;
        private const int HEIGHT = 150;
        private const int MARGIN_X = 5;
        private const int MARGIN_TOP = 20;
        private const int MARGIN_BOTTOM = 5;
        // Temp Switch
        private static bool pluginEnabled = true;

        private void Awake()
        {
            vcConfig = new VoiceChangerConfig(Config);
            voiceChanger.SetServerUrl(vcConfig.serverUrl.Value);
            voiceChanger.SetPreloaderType(vcConfig.preloaderT.Value);
            vcConfig.serverUrl.SettingChanged += (s, e) => voiceChanger.SetServerUrl(vcConfig.serverUrl.Value);
            vcConfig.preloaderT.SettingChanged += (s, e) => voiceChanger.SetPreloaderType(vcConfig.preloaderT.Value);
            Instance = this;
            Harmony.CreateAndPatchAll(typeof(Main));
        }

        private void Update()
        {
            if (Input.GetKeyDown(guiKey))
            {
                showGUI = !showGUI;
            }
            // Wait Check per 100 ms, Cannot Work lower than 10 FPS
            if (!noWait && isWait)
            {
                int left_time = curWaitData.wait_time - GameMain.tick_count + curWaitData.start_tick_count;
                if (left_time < 150)
                {
                    if (curPrepared || (timeout > 0 && curWaitData.wait_time > timeout))
                    {
                        curWaitData.wait_time = 0;
                        curWaitData.use = false;
                        curVoice = null;
                    }
                    else
                    {
                        curWaitData.wait_time += 100;
                    }
                }
            }
        }

        private void OnGUI()
        {
            if (!showGUI)
            {
                return;
            }
            if (colorStyle == null)
            {
                colorStyle = new GUIStyle();
            }


            void Window(int id)
            {
                GUILayout.BeginArea(new Rect(MARGIN_X, MARGIN_TOP, WIDTH - MARGIN_X * 2, HEIGHT - MARGIN_TOP - MARGIN_BOTTOM));
                {
                    GUILayout.BeginVertical();
                    {
                        GUILayout.Label("");
                        if (voiceChanger.connected)
                        {
                            colorStyle.normal.textColor = Color.green;
                            GUILayout.Label("Service Connected", colorStyle);
                        }
                        else
                        {
                            colorStyle.normal.textColor = Color.red;
                            GUILayout.Label("Service Disconnected", colorStyle);
                        }
                        pluginEnabled = GUILayout.Toggle(pluginEnabled, "Enabled");
                        GUILayout.Label($"Cache Num: {voiceChanger.cacheCount}");

                        if (GUILayout.Button("Clean Cache"))
                        {
                            voiceChanger.ClearCache();
                        }

                        GUI.enabled = true;
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndArea();
            }

            GUI.Window(11451, new Rect(0, (Screen.height - HEIGHT) / 2f, WIDTH, HEIGHT), Window, "VoiceChanger Status");
        }

        [HarmonyPatch(typeof(AudioSourceMgr), "LoadFromWf")]
        [HarmonyPrefix]
        public static bool LoadPlayPrefix(ref AudioSourceMgr __instance, ref bool __result, AFileSystemBase fileSystem, string fileName, bool stream)
        {
            if (!pluginEnabled)
            {
                return true;
            }
            // var voiceType = __instance.m_eType;

            var audioClip = voiceChanger.getVoiceClip(fileName, !noWait, timeout);
            if (audioClip != null)
            {
                Logger.LogDebug($"Replace: {fileName}");
                __instance.Clear();
                __instance.FileName = fileName;
                __instance.isLastPlayCompatibilityMode = __instance.m_gcSoundMgr.compatibilityMode;
                __instance.audiosource.clip = audioClip;
                __instance.Fase = 3;
                __result = true;
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(ADVKagManager), "TagTalk")]
        [HarmonyPrefix]
        public static void TagTalkPrefix(KagTagSupport tag_data)
        {
            if (!pluginEnabled)
            {
                return;
            }
            string voice = tag_data.GetTagProperty("voice").AsString();
            if (!voice.IsNullOrWhiteSpace() && enableVoice)
            {
                voiceChanger.LoadVoice(voice);
                Logger.LogDebug($"@talk voice={voice}");
            }
        }

        [HarmonyPatch(typeof(BaseKagManager.ExecWaitData), "Check")]
        [HarmonyPrefix]
        public static bool CheckPrefix(BaseKagManager.ExecWaitData __instance, ref bool __result)
        {
            if (object.ReferenceEquals(__instance, curWaitData))
            {
                __result = !curWaitData.use;
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(ADVKagManager), "TagTalk")]
        [HarmonyPostfix]
        public static void TagTalkPostfix(KagTagSupport tag_data, ref ADVKagManager __instance, ref bool __result)
        {
            if (!pluginEnabled)
            {
                return;
            }
            string voice = tag_data.GetTagProperty("voice").AsString();
            if (!voice.IsNullOrWhiteSpace() && enableVoice && !noWait)
            {
                string oggFileName = Path.GetFileNameWithoutExtension(voice).ToLower() + ".ogg";
                if (GameUty.FileSystem.IsExistentFile(oggFileName))
                {
                    __result = __instance.SetWait(200, false);
                    curWaitData = __instance.exec_wait_data_;
                    curVoice = voice;
                }
            }
        }

        [HarmonyPatch(typeof(BaseKagManager), "TagPlayVoice")]
        [HarmonyPrefix]
        public static void TagPlayVoicePrefix(KagTagSupport tag_data)
        {
            if (!pluginEnabled)
            {
                return;
            }
            string voice = tag_data.GetTagProperty("voice").AsString();
            if (!voice.IsNullOrWhiteSpace() && enableVoice)
            {
                voiceChanger.LoadVoice(voice);
                Logger.LogDebug($"@playvoice voice={voice}");
            }
        }

        [HarmonyPatch(typeof(BaseKagManager), "TagPlaySe")]
        [HarmonyPrefix]
        public static void TagPlaySePrefix(KagTagSupport tag_data)
        {
            if (!pluginEnabled)
            {
                return;
            }
            string file = tag_data.GetTagProperty("file").AsString();
            if (!file.IsNullOrWhiteSpace() && enableSe)
            {
                voiceChanger.LoadVoice(file);
                Logger.LogDebug($"@playse file={file}");
            }
        }
    }
}
