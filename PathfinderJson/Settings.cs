using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace PathfinderJson
{
    public class Settings
    {
        public static Settings LoadSettings(string filename)
        {
            using (StreamReader file = File.OpenText(filename))
            {
                JsonSerializer serializer = new JsonSerializer();
                Settings ss = (Settings)serializer.Deserialize(file, typeof(Settings));
                return ss;
            }
        }

        public void Save(string filename)
        {
            using (StreamWriter file = new StreamWriter(filename))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, this);
            }
        }

        [JsonProperty("themeColor")]
        public string ThemeColor { get; set; } = "CD853F";

        [JsonProperty("pathTitleBar")]
        public bool PathInTitleBar { get; set; } = false;

        [JsonProperty("recentFiles")]
        public List<string> RecentFiles { get; set; } = new List<string>();

        [JsonProperty("startView")]
        public string StartView { get; set; } = "tabs";

        [JsonProperty("indentJsonData")]
        public bool IndentJsonData { get; set; } = false;
    }
}
