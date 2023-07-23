using COM3D2.VoiceChanger.Plugin.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using UniverseLib.Utility;
using WebSocketSharp;

namespace COM3D2.VoiceChanger.Plugin.Infer
{
    internal delegate void VoiceDataCallback(Voice_Data data);

    internal class WSConnection : IDisposable
    {
        private WebSocket ws;
        private Timer reconnectTimer;
        private string _url;
        private bool hasConnected = false;
        private bool disposedValue = false;
        public event VoiceDataCallback Callback;
        private bool _isConnected => (!ws.IsNullOrDestroyed() && ws.IsAlive);
        public bool isConnected { get; private set; } = false;
        public string url
        {
            get { return _url; }
            set
            {
                if (_url != value)
                {
                    _url = value;
                    if (_isConnected)
                    {
                        ws.Close();
                    }
                }
            }
        }

        public WSConnection(string url)
        {
            _url = url;
            reconnectTimer = new Timer(state =>
            {
                if (!isConnected)
                {
                    Connect(_url);
                }
            }, null, 0, 3000);
        }

        ~WSConnection()
        {
            Dispose(false);
        }

        private void Connect(string url)
        {
            if (!url.ToLower().StartsWith("ws://"))
            {
                return;
            }
            ws = new WebSocket(url);
            ws.OnOpen += (sender, e) =>
            {
                Main.Logger.LogDebug("WSConnection Opened");
                isConnected = true;
                hasConnected = true;
            };
            ws.OnMessage += (sender, e) =>
            {
                if (e.Data == "ping")
                {
                    Send("pong");
                }
                else
                {
                    try
                    {
                        var json = JsonConvert.DeserializeObject<List<Voice_Data>>(e.Data);
                        HandleReceivedMessage(json);
                    }
                    catch (JsonReaderException)
                    {
                        Main.Logger.LogError("JSON Parse Error");
                    }
                }
            };
            ws.OnError += (sender, e) =>
            {
                Main.Logger.LogError("WSConnection Error: " + e.Message);
                ws.Close();
            };
            ws.OnClose += (sender, e) =>
            {
                if (hasConnected)
                {
                    Main.Logger.LogDebug("WSConnection Closed");
                }
                isConnected = false;
                hasConnected = false;
            };
            ws.Log.Output = (_, __) => { };
            ws.Connect();
        }

        private void HandleReceivedMessage(List<Voice_Data> message)
        {
            foreach (Voice_Data voice in message)
            {
                Callback?.Invoke(voice);
            }
        }

        private void Send(string message)
        {
            if (_isConnected)
            {
                ws.Send(message);
            }
        }

        public void Send(Base_Data message)
        {
            if (_isConnected)
            {
                List<Base_Data> data = new() { message };
                string json = JsonConvert.SerializeObject(data);
                ws.Send(json);
            }
        }

        public void Send(List<Base_Data> message)
        {
            if (_isConnected)
            {
                string json = JsonConvert.SerializeObject(message);
                ws.Send(json);
            }
        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    reconnectTimer.Dispose();
                }
                disposedValue = true;
                ws.Close();
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
