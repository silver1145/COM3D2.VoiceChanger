using COM3D2.VoiceChanger.Plugin.Utils;
using System;
using System.Collections.Generic;

namespace COM3D2.VoiceChanger.Plugin.Infer
{
    public delegate void VoiceReceiveCallback(Voice_Data data);

    internal class InferClient
    {
        private WSConnection _connection;
        public event VoiceReceiveCallback Callback;

        public InferClient()
        {
            _connection = new WSConnection("");
            _connection.Callback += ReceiveCallback;
        }

        public void SetServerUrl(string url)
        {
            _connection.url = url;
        }

        public void SendVoice(string voice)
        {
            if (GameUty.FileSystem.IsExistentFile(voice))
            {
                var f = GameUty.FileOpen(voice);
                string base64File = Convert.ToBase64String(f.ReadAll());
                Voice_Data voiceData = new(voice, base64File);
                _connection.Send(voiceData);
            }
        }

        public void SendCommand(string command, Dictionary<string, string> data = null)
        {
            if (data == null)
            {
                data = new Dictionary<string, string>();
            }

        }

        private void ReceiveCallback(Voice_Data voice)
        {
            Callback?.Invoke(voice);
        }

    }
}
