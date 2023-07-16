using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.IO;
using System.Reflection;
using System.Security;
using System.Security.Permissions;


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
        internal static bool noWait => vcConfig.noWait.Value;
        internal static bool enableVoice => vcConfig.enableVoice.Value;
        internal static bool enableSe => vcConfig.enableSe.Value;

        private void Awake()
        {
            vcConfig = new(Config);
            voiceChanger.SetServerUrl(vcConfig.serverUrl.Value);
            vcConfig.serverUrl.SettingChanged += (s, e) => voiceChanger.SetServerUrl(vcConfig.serverUrl.Value);
            Instance = this;
            Harmony.CreateAndPatchAll(typeof(Main));
        }

        private void Update()
        {
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

        [HarmonyPatch(typeof(AudioSourceMgr), "LoadFromWf")]
        [HarmonyPrefix]
        public static bool LoadPlayPrefix(ref AudioSourceMgr __instance, ref bool __result, AFileSystemBase fileSystem, string fileName, bool stream)
        {
            // var voiceType = __instance.m_eType;
            var audioClip = voiceChanger.getVoiceClip(fileName, !noWait);
            if (audioClip != null)
            {
                Logger.LogDebug($"replace: {fileName}");
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
            string file = tag_data.GetTagProperty("file").AsString();
            if (!file.IsNullOrWhiteSpace() && enableSe)
            {
                voiceChanger.LoadVoice(file);
                Logger.LogDebug($"@playse file={file}");
            }
        }
    }
}
