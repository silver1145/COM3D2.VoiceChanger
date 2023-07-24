using COM3D2.VoiceChanger.Plugin.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace COM3D2.VoiceChanger.Plugin.Preloader
{
    internal class LinearPreloader : BasePreloader
    {
        public LinearPreloader(int cacheSize, CacheHashSet<string> wait, PreloadResultCallback callback) : base(cacheSize, wait, callback)
        {
            preloaderType = PreloaderType.LinerPreloader;
        }

        ~LinearPreloader()
        {
            Dispose(false);
        }

        public override void Dispose()
        {
            Dispose(true);
        }

        static List<string> IncrementNumericSuffix(string oggFileName, int addNum = 5)
        {
            string fileName = Path.GetFileNameWithoutExtension(oggFileName);
            List<string> result = new List<string>();
            Match match = Regex.Match(fileName, @"\d+$");
            if (match.Success)
            {
                string numericPart = match.Value;
                int numericValue = int.Parse(numericPart);
                for (int i = 1; i <= addNum; i++)
                {
                    int incrementedValue = numericValue + i;
                    string incrementedPart = incrementedValue.ToString().PadLeft(numericPart.Length, '0');
                    string resultName = fileName.Substring(0, match.Index) + incrementedPart;
                    result.Add(Path.ChangeExtension(resultName, ".ogg"));
                }
            }
            return result;
        }

        protected override List<Base_Data> Predict(string oggFilename)
        {
            List<Base_Data> result = new List<Base_Data>();
            if (!preloadHistory.Contains(oggFilename) && preloadHistory.Any())
            {
                Main.Logger.LogDebug($"Preload Cancelled");
                Command_Data commandd = new("cancel");
                result.Add(commandd);
            }
            foreach(string file in IncrementNumericSuffix(oggFilename))
            {
                if (GameUty.FileSystem.IsExistentFile(file) && !preloadHistory.Contains(file))
                {
                    Voice_Data voiceData = new(file);
                    result.Add(voiceData);
                    Main.Logger.LogDebug($"Preload File: {file}");
                }
            }
            return result;
        }
    }
}
