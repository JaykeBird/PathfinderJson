using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace PathfinderJson
{

    //new JsonSerializerSettings
    //{
    //  DefaultValueHandling = DefaultValueHandling.Ignore
    //});

    public class PathfinderSheet
    {
        public static PathfinderSheet LoadJsonFile(string filename)
        {
            void Handler(object sender, Newtonsoft.Json.Serialization.ErrorEventArgs e)
            {
                throw new FileFormatException(new Uri(filename), "This file does not match the format for JSON. Check if it isn't corrupted by opening it in Notepad or another text editor.", e.ErrorContext.Error);
            }

            string csc = "";

            using (StreamReader file = File.OpenText(filename))
            {
                JObject o = (JObject)JToken.ReadFrom(new JsonTextReader(file));

                try
                {
                    JToken a = o["skills"]["conditionalModifiers"];
                    if (a.Value<string>() != null)
                    {
                        Console.WriteLine(a.Value<string>());
                        csc = a.Value<string>();
                    }
                }
                catch (ArgumentNullException) { }
                catch (NullReferenceException) { }

                file.BaseStream.Position = 0;

                JsonSerializer serializer = new JsonSerializer();
                serializer.ContractResolver = new CamelCasePropertyNamesContractResolver(); // apparently this isn't enough to actually make it write the property names in camelCase style
                serializer.Error += Handler;
                PathfinderSheet ps = (PathfinderSheet)serializer.Deserialize(file, typeof(PathfinderSheet));
                ps.SetupSheet();
                if (!string.IsNullOrEmpty(csc)) ps.SkillConditionalModifiers = csc;
                return ps;
            }
        }

        public static PathfinderSheet LoadJsonText(string text)
        {
            PathfinderSheet ps = JsonConvert.DeserializeObject<PathfinderSheet>(text);
            ps.SetupSheet();
            return ps;
        }

        public string SaveJsonText(bool indented = false)
        {
            return JsonConvert.SerializeObject(this, indented ? Formatting.Indented : Formatting.None);
        }

        public void SaveJsonFile(string file, bool indented = false)
        {
            File.WriteAllText(file, SaveJsonText(indented));
        }

        [JsonProperty("_id")]
        public string Id { get; set; }

        // roleplaying characteristics
        public string Name { get; set; }
        public string Alignment { get; set; }
        public string Level { get; set; }
        public string Homeland { get; set; }
        public string Deity { get; set; }
        public string Languages { get; set; }

        [JsonProperty("user")]
        public UserData Player { get; set; }

        public string Notes { get; set; }

        // physical characteristics
        public string Gender { get; set; } = "";
        public string Age { get; set; } = "";
        public string Height { get; set; } = "";
        public string Weight { get; set; } = "";
        public string Race { get; set; } = "";
        public string Size { get; set; } = "M";
        public string Hair { get; set; } = "";
        public string Eyes { get; set; } = "";

        // base abilities
        [JsonIgnore]
        public int Strength { get; set; }
        [JsonIgnore]
        public int Intelligence { get; set; }
        [JsonIgnore]
        public int Charisma { get; set; }
        [JsonIgnore]
        public int Constitution { get; set; }
        [JsonIgnore]
        public int Dexterity { get; set; }
        [JsonIgnore]
        public int Wisdom { get; set; }

        // other numbers

        public CompoundModifier Initiative { get; set; } = new CompoundModifier();
        [JsonProperty("cmb")]
        public CompoundModifier CombatManeuverBonus { get; set; } = new CompoundModifier();
        [JsonProperty("cmd")]
        public CompoundModifier CombatManeuverDefense { get; set; } = new CompoundModifier();
        [JsonProperty("bab")]
        public string BaseAttackBonus { get; set; } = "0";
        public ArmorClass AC { get; set; } = new ArmorClass();

        public string DamageReduction { get; set; } = "";
        public string Resistances { get; set; } = "";

        public List<Feat> Feats { get; set; } = new List<Feat>();
        public List<SpecialAbility> SpecialAbilities { get; set; } = new List<SpecialAbility>();
        public List<SpecialAbility> Traits { get; set; } = new List<SpecialAbility>();

        [JsonProperty("gear")]
        public List<Equipment> Equipment { get; set; } = new List<Equipment>();

        [JsonProperty("ranged")]
        public List<Weapon> RangedWeapons { get; set; } = new List<Weapon>();
        [JsonProperty("melee")]
        public List<Weapon> MeleeWeapons { get; set; } = new List<Weapon>();

        // used to interface with JSON file
        [JsonProperty("abilities")]
        public Dictionary<string, string> RawAbilities { get; set; }
        [JsonProperty(ItemConverterType = typeof(SkillConverter))]
        public Dictionary<string, Skill> Skills { get; set; }
        [JsonIgnore]
        public string SkillConditionalModifiers { get; set; }
        public Dictionary<string, CompoundModifier> Saves { get; set; } = new Dictionary<string, CompoundModifier>();
        public Dictionary<string, string> Money { get; set; }

        public HP HP { get; set; } = new HP();

        private void SetupSheet()
        {
            foreach (KeyValuePair<string, string> item in RawAbilities)
            {
                switch (item.Key)
                {
                    case "wis":
                        try { Wisdom = int.Parse(item.Value); } catch (FormatException) { Wisdom = 0; }
                        break;
                    case "int":
                        try { Intelligence = int.Parse(item.Value); } catch (FormatException) { Intelligence = 0; }
                        break;
                    case "cha":
                        try { Charisma = int.Parse(item.Value); } catch (FormatException) { Charisma = 0; }
                        break;
                    case "str":
                        try { Strength = int.Parse(item.Value); } catch (FormatException) { Strength = 0; }
                        break;
                    case "dex":
                        try { Dexterity = int.Parse(item.Value); } catch (FormatException) { Dexterity = 0; }
                        break;
                    case "con":
                        try { Constitution = int.Parse(item.Value); } catch (FormatException) { Constitution = 0; }
                        break;
                    default:
                        break;
                }
            }

            foreach (KeyValuePair<string, Skill> item in Skills)
            {
                item.Value.Name = item.Key;

                if (item.Key == "conditonalModifiers")
                {
                    SkillConditionalModifiers = item.Value.Specialization;
                }
            }
        }
    }

    public class UserData
    {
        public string Provider { get; set; }
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string Gender { get; set; }
        public List<Email> Emails { get; set; }
        public Name UserName { get; set; }
        public List<Photo> Photos { get; set; }

        public class Name { public string FamilyName { get; set; } public string GivenName { get; set; } }
        public class Email { public string Value { get; set; } public string Type { get; set; } }
        public class Photo { public string Value { get; set; } }
    }

    public class SkillConverter : JsonConverter<Skill>
    {
        public override void WriteJson(JsonWriter writer, Skill value, JsonSerializer serializer)
        {
            if (value.Name == "conditionalModifiers")
            {
                //writer.WritePropertyName("conditionalModifiers");
                writer.WriteValue(value.Specialization);
            }
            else
            {
                serializer.Serialize(writer, value, typeof(Skill));
            }
        }

        public override Skill ReadJson(JsonReader reader, Type objectType, Skill existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.PropertyName)
            {
                string name = (string)reader.Value;
                if (name == "conditionalModifiers")
                {
                    reader.Read();
                    return new Skill("conditionalModifiers", reader.Value.ToString());
                }
                else
                {
                    Skill s = serializer.Deserialize<Skill>(reader);
                    s.Name = name;
                    return s;
                }
            }
            else if (reader.TokenType == JsonToken.String)
            {
                return new Skill("conditionalModifiers", reader.Value.ToString());
            }
            else
            {
                return serializer.Deserialize<Skill>(reader);
            }
        }
    }

    public class Skill
    {
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool ClassSkill { get; set; } = false;
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public string Ranks { get; set; }
        public string Total { get; set; }
        public string Racial { get; set; }
        public string Trait { get; set; }
        public string Misc { get; set; }
        [JsonProperty("name", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Specialization { get; set; }
        [JsonIgnore]
        public string Name { get; set; }

        public Skill()
        {

        }

        public Skill(string name, string s)
        {
            ClassSkill = false;
            Name = name;
            Ranks = "0";
            Total = "0";
            Racial = "0";
            Trait = "0";
            Misc = "0";
            Specialization = s;
        }
    }

    public class Feat
    {
        public string Name { get; set; }
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public string Type { get; set; }
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public string Notes { get; set; }
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public string School { get; set; }
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public string Subschool { get; set; }
    }

    public class CompoundModifier
    {
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public string Base { get; set; } = "0";
        public string Total { get; set; } = "0";
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public string MagicModifier { get; set; } = "0";
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public string MiscModifier { get; set; } = "0";
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public string OtherModifiers { get; set; } = "0";
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public string SizeModifier { get; set; } = "0";

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public string TempModifier { get; set; } = "0";
    }

    public class ArmorClass
    {
        public string Total { get; set; } = "";
        public string Touch { get; set; } = "";
        public string FlatFooted { get; set; } = "";

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public string SizeModifier { get; set; }
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public string MiscModifier { get; set; } = "0";
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public string OtherModifiers { get; set; } = "0";
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public string NaturalArmor { get; set; } = "0";
        [JsonProperty("deflectionModifier", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public string Deflection { get; set; } = "0";
        public string ArmorBonus { get; set; } = "0";
        public string ShieldBonus { get; set; } = "0";

        public List<AcItem> Items { get; set; } = new List<AcItem>();
        public AcItem ItemTotals { get; set; } = new AcItem();
    }

    public class Weapon
    {
        [JsonProperty("weapon")]
        public string Name { get; set; }
        public string Damage { get; set; }
        [JsonProperty("critical")]
        public string CriticalRange { get; set; }
        public string Type { get; set; }
        public string AttackBonus { get; set; }
        public string Notes { get; set; } = "";
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Range { get; set; } = "";
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Ammunition { get; set; }
    }

    public class AcItem
    {
        public string Name { get; set; } = "";
        public string Bonus { get; set; } = "";
        public string Type { get; set; } = "";
        public string ArmorCheckPenalty { get; set; } = "";
        public string SpellFailure { get; set; } = "";
        public string Weight { get; set; } = "";
        public string Properties { get; set; } = "";
    }

    public class Equipment
    {
        public string Name { get; set; }
        public string Location { get; set; }
        public string Type { get; set; }
        public string Quantity { get; set; }
        public string Weight { get; set; }
        public string Notes { get; set; }
    }

    public class SpecialAbility
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Notes { get; set; }
    }

    public class HP
    {
        public string Total { get; set; } = "0";
        public string Wounds { get; set; } = "0";
        public string NonLethal { get; set; } = "0";
    }
}
