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

        public Voice_Data(string name, string file)
        {
            type = "voice";
            this.name = name;
            this.file = file;
        }
    }

    public class Command_Data: Base_Data
    {
        public string name { get; set; }
        public string data { get; set; }

        public Command_Data(string name, string data)
        {
            type = "cmd";
            this.name = name;
            this.data = data;
        }
    }
}
