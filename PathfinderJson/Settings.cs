using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using Newtonsoft.Json;

namespace PathfinderJson
{
    public class Settings
    {

        public static Settings LoadSettings(string filename)
        {
            try
            {
                using StreamReader file = File.OpenText(filename);
                JsonSerializer serializer = new JsonSerializer();

                Settings? ss = (Settings?)serializer.Deserialize(file, typeof(Settings));
                if (ss == null) ss = new Settings();
                return ss;
            }
            catch (JsonReaderException)
            {
                MessageBox.Show("The settings file for PathfinderJson was corrupted. PathfinderJson will continue with default settings.",
                    "Settings Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                Settings sn = new Settings();
                sn.Save(filename); // exception handling not needed for these as calling function handles exceptions
                return sn;
            }
            catch (FileNotFoundException)
            {
                Settings sn = new Settings();
                sn.Save(filename); // exception handling not needed for these as calling function handles exceptions
                return sn;
            }
        }

        public void Save(string filename)
        {
            DirectoryInfo di = Directory.GetParent(filename);

            if (!di.Exists)
            {
                di.Create();
            }

            using StreamWriter file = new StreamWriter(filename, false, new UTF8Encoding(false));
            JsonSerializer serializer = new JsonSerializer();
            serializer.Serialize(file, this);
        }

        [JsonProperty("themeColor")]
        public string ThemeColor { get; set; } = "CD853F";

        [JsonProperty("highContrast")]
        public string HighContrastTheme { get; set; } = App.NO_HIGH_CONTRAST;

        [JsonProperty("pathTitleBar")]
        public bool PathInTitleBar { get; set; } = false;

        [JsonProperty("recentFiles", Order = 50)]
        public List<string> RecentFiles { get; set; } = new List<string>();

        [JsonProperty("startView")]
        public string StartView { get; set; } = "tabs";

        [JsonProperty("showToolbar")]
        public bool ShowToolbar { get; set; } = false;

        [JsonProperty("indentJsonData")]
        public bool IndentJsonData { get; set; } = false;

        [JsonProperty("updateAutoCheck")]
        public bool UpdateAutoCheck { get; set; } = false;

        [JsonProperty("updateLastCheckDate")]
        public string UpdateLastCheckDate { get; set; } = "2019-10-12";

        [JsonProperty("editor.fontFamily")]
        public string EditorFontFamily { get; set; } = "Consolas";

        [JsonProperty("editor.fontSize")]
        public string EditorFontSize { get; set; } = "12";
        
        [JsonProperty("editor.fontWeight")]
        public string EditorFontWeight { get; set; } = "400";

        [JsonProperty("editor.fontStyle")]
        public string EditorFontStyle { get; set; } = "Normal";
    }
}
