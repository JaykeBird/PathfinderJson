using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PathfinderJson
{
    public class SkillList
    {

        public static SkillList LoadStandardList()
        {
            //var ss = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceNames();

            return Load(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("PathfinderJson.SkillList.pathfinder.json"), throwOnNull: false);
            //return Load(Application.GetResourceStream(new Uri("pack://application:,,,/SkillLists/pathfinder.json")).Stream);
        }

        public static SkillList LoadPsionicsList()
        {
            //var ss = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceNames();

            return Load(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("PathfinderJson.SkillList.pathfinder-psionics.json"), throwOnNull: false);
            //return Load(Application.GetResourceStream(new Uri("pack://application:,,,/SkillLists/pathfinder.json")).Stream);
        }

        public static SkillList Load(Stream? s, string filename = "", bool throwOnNull = true)
        {
            if (s == null)
            {
                if (throwOnNull) throw new ArgumentNullException(nameof(s), "Stream cannot be null. Function can be set to return an empty list if \"throwOnNull\" is set to false.");
                else return new SkillList();
            }

            try
            {
                using StreamReader file = new StreamReader(s);
                JsonSerializer serializer = new JsonSerializer();

                SkillList? ss = serializer.Deserialize<SkillList>(new JsonTextReader(file));
                if (ss == null) ss = new SkillList();
                return ss;
            }
            catch (JsonReaderException e)
            {
                throw new FormatException("This file does not match the format of a skill list JSON file.", e);
                //MessageBox.Show("The SkillList file for SentinelsJson was corrupted. SentinelsJson will continue with default SkillList.",
                //    "SkillList Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                //SkillList sn = new SkillList();
                //sn.Save(filename); // exception handling not needed for these as calling function handles exceptions
                //return sn;
            }
            catch (FileNotFoundException)
            {
                throw;
                //SkillList sn = new SkillList();
                //sn.Save(filename); // exception handling not needed for these as calling function handles exceptions
                //return sn;
            }
        }

        public static SkillList LoadFile(string filename)
        {
            return Load(File.OpenRead(filename));
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

        public string Name { get; set; } = "Unnamed";

        public string GameSystem { get; set; } = "Pathfinder1e";

        [JsonProperty("skills")]
        public List<SkillListEntry> SkillEntries { get; private set; } = new List<SkillListEntry>();

        public void AddSkill(SkillListEntry sle)
        {
            SkillEntries.Add(sle);
        }
    }

    public class SkillListEntry
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public string Name { get; set; } = "Unnamed";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string? DisplayName { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public string Modifier { get; set; } = "INT";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string? InfoUrl { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool HasSpecialization { get; set; } = false;
    }
}
