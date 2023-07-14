using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
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
        internal static ConfigEntry<bool> voiceChangerConfig;
        internal static VoiceChanger voiceManager = new();

        private void Awake()
        {
            Instance = this;
            voiceChangerConfig = Config.Bind("Section", "Test_Name", false, "Description");
            Harmony.CreateAndPatchAll(typeof(Main));
        }

        [HarmonyPatch(typeof(AudioSourceMgr), "LoadFromWf")]
        [HarmonyPrefix]
        public static bool LoadPlayPrefix(ref AudioSourceMgr __instance, ref bool __result, AFileSystemBase fileSystem, string fileName, bool stream)
        {
            var voiceType = __instance.m_eType;
            // TODO: Filter
            var audioClip = voiceManager.getVoiceClip(fileName);
            if (audioClip != null)
            {
                Logger.LogInfo($"replace: {fileName}");
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
            if (!voice.IsNullOrWhiteSpace())
            {
                voiceManager.LoadVoice(voice);
                Logger.LogDebug($"@talk voice={voice}");
            }
        }

        [HarmonyPatch(typeof(BaseKagManager), "TagPlayVoice")]
        [HarmonyPrefix]
        public static void TagPlayVoicePrefix(KagTagSupport tag_data)
        {
            string voice = tag_data.GetTagProperty("voice").AsString();
            if (!voice.IsNullOrWhiteSpace())
            {
                voiceManager.LoadVoice(voice);
                Logger.LogDebug($"@playvoice voice={voice}");
            }
        }
    }
}
