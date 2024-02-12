using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using static RootMotion.FinalIK.InteractionObject;

namespace COM3D2.VoiceChanger.Plugin.Utils
{
    public abstract class Base_Data
    {
        public string type;
    }

    public class Voice_Data: Base_Data
    {
        public string name { get; set; }
        public string file { get; set; }

        public Voice_Data() { }

        public Voice_Data(string name, string file)
        {
            type = "voice";
            this.name = name;
            this.file = file;
        }

        public Voice_Data(string name)
        {
            type = "voice";
            this.name = name;
            using (var f = GameUty.FileOpen(name))
            {
                string base64File = Convert.ToBase64String(f.ReadAll());
                this.file = base64File;
            }
        }
    }

    public class Command_Data: Base_Data
    {
        public string name { get; set; }
        public string data { get; set; }

        public Command_Data() { }

        public Command_Data(string name, string data)
        {
            type = "cmd";
            this.name = name;
            this.data = data;
        }

        public Command_Data(string name, Dictionary<string, string> data = null)
        {
            type = "cmd";
            this.name = name;
            if (data == null)
            {
                data = new Dictionary<string, string>();
            }
            string dataString = JsonConvert.SerializeObject(data);
            this.data = dataString;
        }
    }
}
