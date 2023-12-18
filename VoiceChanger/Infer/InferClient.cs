using COM3D2.VoiceChanger.Plugin.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using static PrivateMaidTouchManager;

namespace COM3D2.VoiceChanger.Plugin.Infer
{
    internal delegate void VoiceReceiveCallback(Voice_Data data);

    internal class InferClient
    {
        private WSConnection wsConnection;
        public event VoiceReceiveCallback Callback;
        public bool connected => wsConnection.isConnected;

        public InferClient()
        {
            wsConnection = new WSConnection("");
            wsConnection.Callback += ReceiveCallback;
        }

        public void SetServerUrl(string url)
        {
            wsConnection.url = url;
        }

        public void SendVoice(string voice)
        {
            if (GameUty.FileSystem.IsExistentFile(voice))
            {
                Voice_Data voiceData = new(voice);
                Send(voiceData);
            }
        }

        public void SendCommand(string command, Dictionary<string, string> data = null)
        {
            Command_Data commandData = new Command_Data(command, data);
            Send(commandData);
        }

        public void Send(Base_Data data)
        {
            lock (wsConnection)
            {
                wsConnection.Send(data);
            }
        }

        public void Send(List<Base_Data> data)
        {
            lock (wsConnection)
            {
                wsConnection.Send(data);
            }
        }

        private void ReceiveCallback(Voice_Data voice)
        {
            Callback?.Invoke(voice);
        }
    }
}
