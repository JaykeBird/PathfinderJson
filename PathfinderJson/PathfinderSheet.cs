using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using PathfinderJson.Ild;

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

            string csc = "";

            using (StreamReader file = File.OpenText(filename))
            {
                try
                {
                    JObject o = (JObject)JToken.ReadFrom(new JsonTextReader(file));

                    try
                    {
                        if (o.ContainsKey("skills"))
                        {
                            JToken a = o["skills"]!["conditionalModifiers"]!;
                            if (a.Value<string>() != null)
                            {
                                Console.WriteLine(a.Value<string>());
                                csc = a.Value<string>() ?? "";
                            }
                        }
                    }
                    catch (ArgumentNullException) { }
                    catch (NullReferenceException) { }

                    file.BaseStream.Position = 0;

                    JsonSerializer serializer = new JsonSerializer();
                    serializer.DefaultValueHandling = DefaultValueHandling.Ignore;
                    serializer.NullValueHandling = NullValueHandling.Ignore;
                    serializer.ContractResolver = new CamelCasePropertyNamesContractResolver();
                    serializer.MaxDepth = 64;
                    serializer.Error += (object? sender, Newtonsoft.Json.Serialization.ErrorEventArgs e) => ErrorHandler(sender, e, filename);
                    PathfinderSheet ps = (PathfinderSheet)serializer.Deserialize(file, typeof(PathfinderSheet))!;
                    ps.SetupSheet();
                    if (!string.IsNullOrEmpty(csc)) ps.SkillConditionalModifiers = csc;
                    return ps;
                }
                catch (JsonReaderException e)
                {
                    throw new InvalidDataException("This file does not match the format for JSON. Check if it isn't corrupted by opening it in Notepad or another text editor.", e);
                    //throw new FileFormatException(new Uri(filename), , e);
                }
            }
        }

        private static void ErrorHandler(object? _, Newtonsoft.Json.Serialization.ErrorEventArgs e, string filename)
        {
            throw new InvalidDataException("This file \"" + filename + "\" does not match the format for JSON. Check if it isn't corrupted by opening it in Notepad or another text editor.", e.ErrorContext.Error);
        }

        public static PathfinderSheet LoadJsonText(string text)
        {
            PathfinderSheet? ps = JsonConvert.DeserializeObject<PathfinderSheet>(text);
            if (ps != null)
            {
                ps.SetupSheet();
                return ps;
            }
            else
            {
                throw new FormatException("The provided text is not parseable as JSON.");
            }
        }

        public static PathfinderSheet CreateNewSheet(string name, string level, UserData? userdata = null)
        {
            string newjson = "{\"_id\":\"-1\"," +
                "\"user\":{\"provider\":\"local\",\"id\":\"null\",\"displayName\":\"-\"," +
                "\"username\":\"\",\"profileUrl\":\"\",\"emails\":[]},\"spells\":[{},{},{},{},{},{},{},{},{},{}]," +
                "\"name\":\"" + name + "\",\"modified\":\"" + string.Concat(DateTime.UtcNow.ToString("s"), ".000Z") + "\",\"level\":\"" + level + "\"}";

            PathfinderSheet ps = LoadJsonText(newjson);
            if (userdata != null) ps.Player = userdata;

            return ps;
        }

        public string SaveJsonText(bool indented = false, string file = "StoredText", bool updateModified = false)
        {
            if (updateModified) Modified = string.Concat(DateTime.UtcNow.ToString("s"), ".000Z");

            JsonSerializerSettings jss = new JsonSerializerSettings();
            jss.DefaultValueHandling = DefaultValueHandling.Ignore;
            jss.NullValueHandling = NullValueHandling.Ignore;
            jss.ContractResolver = new CamelCasePropertyNamesContractResolver();
            jss.Error += (object? sender, Newtonsoft.Json.Serialization.ErrorEventArgs e) => ErrorHandler(sender, e, file);
            return JsonConvert.SerializeObject(this, indented ? Formatting.Indented : Formatting.None, jss);
        }

        public void SaveJsonFile(string file, bool indented = false)
        {
            File.WriteAllText(file, SaveJsonText(indented, file, true));
        }

        [JsonProperty("_id", Order = -7)]
        public string? Id { get; set; }
        [JsonProperty(Order = -2, DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore)]
        public string? Modified { get; set; } = null; // ISO 8601 datetime (with UTC mark)
        [JsonProperty(Order = -6, DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore)]
        public string? Version { get; set; } = null;

        // roleplaying characteristics
        [JsonProperty(Order = -3)]
        public string Name { get; set; } = "";
        public string? Alignment { get; set; }
        public string Level { get; set; } = "";
        public string? Homeland { get; set; }
        public string? Deity { get; set; }
        public string? Languages { get; set; }

        [JsonProperty("user", Order = -5)]
        public UserData? Player { get; set; }// = new UserData(false);

        [JsonProperty(Order = 49, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public bool NotesMarkdown { get; set; } = false;

        [JsonProperty(Order = 50, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public string Notes { get; set; } = "";

        // physical characteristics
        public string? Gender { get; set; }
        public string? Age { get; set; }
        public string? Height { get; set; }
        public string? Weight { get; set; }
        public string? Race { get; set; }
        public string? Size { get; set; }
        public string? Hair { get; set; }
        public string? Eyes { get; set; }

        public Speed? Speed { get; set; }

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

        /// <summary>Used to determine if the abilities structure in JSON was present or not</summary>
        [JsonIgnore]
        public bool AbilitiesPresent { get; set; }

        // sheet settings

        [JsonProperty("sheetSettings", DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, string?>? SheetSettings { get; set; }

        // other numbers

        public CompoundModifier Initiative { get; set; } = new CompoundModifier();
        [JsonProperty("cmb")]
        public CompoundModifier CombatManeuverBonus { get; set; } = new CompoundModifier();
        [JsonProperty("cmd")]
        public CompoundModifier CombatManeuverDefense { get; set; } = new CompoundModifier();
        [JsonProperty("bab")]
        public string BaseAttackBonus { get; set; } = "0";
        public ArmorClass AC { get; set; } = new ArmorClass();
        public Dictionary<string, CompoundModifier> Saves { get; set; } = new Dictionary<string, CompoundModifier>();

        [JsonProperty("ranged")]
        public List<Weapon> RangedWeapons { get; set; } = new List<Weapon>();
        [JsonProperty("melee")]
        public List<Weapon> MeleeWeapons { get; set; } = new List<Weapon>();

        [JsonProperty(Order = 19)]
        public string? DamageReduction { get; set; }
        [JsonProperty(Order = 20)]
        public string? Resistances { get; set; }

        public List<Feat> Feats { get; set; } = new List<Feat>();
        [JsonProperty(Order = 22)]
        public List<SpecialAbility> SpecialAbilities { get; set; } = new List<SpecialAbility>();
        [JsonProperty(Order = 26)]
        public List<SpecialAbility> Traits { get; set; } = new List<SpecialAbility>();

        // equipment

        [JsonProperty("gear")]
        public List<Equipment> Equipment { get; set; } = new List<Equipment>();
        [JsonProperty(Order = 21)]
        public Dictionary<string, string?>? Money { get; set; }
        public Dictionary<string, string?>? Xp { get; set; }

        // skills

        [JsonProperty(ItemConverterType = typeof(SkillConverter), NullValueHandling = NullValueHandling.Ignore, Order = 15)]
        public Dictionary<string, Skill> Skills { get; set; } = new Dictionary<string, Skill>();
        [JsonIgnore]
        public string? SkillConditionalModifiers { get; set; }

        public HP HP { get; set; } = new HP();

        // spells

        [JsonProperty(Order = -4)]
        public List<SpellLevel> Spells { get; set; } = new List<SpellLevel>(10);
        [JsonProperty(Order = 23)]
        public string? SpellsConditionalModifiers { get; set; }
        [JsonProperty(Order = 24)]
        public string? SpellsSpeciality { get; set; }
        [JsonProperty("spellLikes", Order = 25)]
        public List<Spell> SpellLikeAbilities { get; set; } = new List<Spell>();

        // raw data, used to interface with JSON file

        [JsonProperty("abilities")]
        public Dictionary<string, string> RawAbilities { get; set; } = new Dictionary<string, string>();

        private void SetupSheet()
        {
            if (RawAbilities != null)
            {
                if (RawAbilities.Count == 0)
                {
                    DefaultLoad();
                }
                else
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
                    AbilitiesPresent = true;
                }
            }
            else
            {
                DefaultLoad();
            }

            void DefaultLoad()
            {
                // the user didn't fill in the base ability scores for the character at all
                // so just set everything to 0
                Wisdom = 0;
                Intelligence = 0;
                Charisma = 0;
                Strength = 0;
                Dexterity = 0;
                Constitution = 0;
                AbilitiesPresent = false;
            }

            foreach (KeyValuePair<string, Skill> item in Skills)
            {
                Skill s = item.Value ?? new Skill();
                s.Name = item.Key;

                if (item.Key == "conditonalModifiers")
                {
                    SkillConditionalModifiers = s.Specialization;
                }
            }
        }
    }

    public class SkillConverter : JsonConverter<Skill>
    {
        public override bool CanRead => true;
        public override bool CanWrite => true;

        public override void WriteJson(JsonWriter writer, Skill? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                if (serializer.DefaultValueHandling != DefaultValueHandling.Ignore || serializer.NullValueHandling != NullValueHandling.Ignore)
                {
                    writer.WriteNull();
                }
            }
            else
            {
                if (value is Skill s)
                {
                    if (s == null)
                    {
                        //if (serializer.DefaultValueHandling != DefaultValueHandling.Ignore || serializer.NullValueHandling != NullValueHandling.Ignore)
                        //{
                        //    writer.WriteNull();
                        //}
                    }
                    else if (s.Name == "conditionalModifiers")
                    {
                        //writer.WritePropertyName("conditionalModifiers");
                        writer.WriteValue(s.Specialization);
                    }
                    else
                    {
                        serializer.Serialize(writer, value, typeof(Skill));
                    }
                }
            }

        }

        public override Skill ReadJson(JsonReader reader, Type objectType, Skill? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.PropertyName)
            {
                object? o = reader.Value;
                if (o is string name)
                {
                    if (name == "conditionalModifiers")
                    {
                        reader.Read();
                        return new Skill("conditionalModifiers", (reader.Value ?? "").ToString());
                    }
                    else
                    {
                        Skill s = serializer.Deserialize<Skill>(reader)!;
                        s.Name = name;
                        return s;
                    }
                }
                else
                {
                    Skill s = serializer.Deserialize<Skill>(reader)!;
                    s.Name = "";
                    return s;
                }
            }
            else if (reader.TokenType == JsonToken.String)
            {
                return new Skill("conditionalModifiers", reader.Value?.ToString() ?? "");
            }
            else
            {
                return serializer.Deserialize<Skill>(reader)!;
            }
        }
    }

    public class Skill : IEquatable<Skill>
    {

        public bool ClassSkill { get; set; } = false;
        public string? Ranks { get; set; }
        public string? Total { get; set; }
        public string? Racial { get; set; }
        public string? Trait { get; set; }
        public string? Misc { get; set; }
        [JsonProperty("name")]
        public string? Specialization { get; set; }
        [JsonIgnore]
        public string Name { get; set; } = "";

        public Skill()
        {
        }

        public Skill(string name, string? s)
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

        public override string ToString()
        {
            return (Name ?? "0") + " " + (Total ?? "0") + " " + (Specialization == null ? "" : Specialization + " ")
                + "(" + (Ranks ?? "0") + " + " + (Racial ?? "0") + " + " + (Trait ?? "0") + (Misc ?? "0") + ")";
        }

        public bool Equals(Skill? other)
        {
            return other is not null
                    && other.Name == Name && other.Ranks == Ranks && other.Total == Total && other.Racial == Racial &&
                    other.Trait == Trait && other.Misc == Misc && other.Specialization == Specialization && other.ClassSkill == ClassSkill;
        }

        public override bool Equals(object? obj)
        {
            return obj is CompoundModifier f ? Equals(f) : false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name?.GetHashCode() ?? 0, Ranks?.GetHashCode() ?? 0, Total?.GetHashCode() ?? 0, Racial?.GetHashCode() ?? 0,
                Trait?.GetHashCode() ?? 0, Misc?.GetHashCode() ?? 0, Specialization?.GetHashCode() ?? 0, ClassSkill.GetHashCode());
        }

        public static bool operator ==(Skill? left, Skill? right)
        {
            return left is null ? right is null : left.Equals(right);
        }

        public static bool operator !=(Skill? left, Skill? right)
        {
            return left is null ? right is not null : !left.Equals(right);
        }
    }

    public class UserData
    {
        public string Provider { get; set; } = "Local/Unknown";
        public string? Id { get; set; }
        public string? DisplayName { get; set; }
        //public string Gender { get; set; }
        public List<Email> Emails { get; set; }
        //public Name UserName { get; set; }
        public List<Photo> Photos { get; set; }
        [JsonProperty("profileUrl")]
        public string? ProfileUrl { get; set; }

        //public class Name { public string FamilyName { get; set; } public string GivenName { get; set; } }
        public class Email
        {
            public string Value { get; set; } = "";
            [JsonProperty("type")]
            public string? Type { get; set; }
        }

        public class Photo { public string? Value { get; set; } }

        public UserData()
        {
            Emails = new List<Email>();
            Photos = new List<Photo>();
        }

        public UserData(bool preset)
        {
            if (preset)
            {
                Provider = "local";
                Id = "null";
                DisplayName = "";
                ProfileUrl = "";
            }

            Emails = new List<Email>();
            Photos = new List<Photo>();
        }
    }

    public class Feat : IEquatable<Feat>
    {
        [IldDisplay(Name = "Name")]
        public string Name { get; set; } = "";

        [IldDisplay(Name = "Type")]
        public string? Type { get; set; }

        [IldDisplay(Name = "Notes")]
        public string? Notes { get; set; }

        [IldDisplay(Name = "School")]
        public string? School { get; set; }

        [IldDisplay(Name = "Subschool")]
        public string? Subschool { get; set; }

        public override string ToString()
        {
            return Name + (Type == null ? "" : "(" + Type + ")");
        }

        public bool Equals(Feat? other)
        {
            return other is not null
                    && other.Name == Name && other.Type == Type && other.School == School && other.Subschool == Subschool
                    && other.Notes == Notes;
        }

        public override bool Equals(object? obj)
        {
            return obj is Feat f ? Equals(f) : false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name.GetHashCode(), Type?.GetHashCode() ?? 0, Notes?.GetHashCode() ?? 0, School?.GetHashCode() ?? 0, Subschool?.GetHashCode() ?? 0);
        }
        
        public static bool operator ==(Feat? left, Feat? right)
        {
            return left is null ? right is null : left.Equals(right);
        }

        public static bool operator !=(Feat? left, Feat? right)
        {
            return left is null ? right is not null : !left.Equals(right);
        }
    }

    public class CompoundModifier : IEquatable<CompoundModifier>
    {
        [IldDisplay(Name = "Base")]
        public string? Base { get; set; }

        [IldDisplay(Name = "Total")]
        public string? Total { get; set; }

        [IldDisplay(Name = "MagicModifier")]
        public string? MagicModifier { get; set; }

        [IldDisplay(Name = "MiscModifier")]
        public string? MiscModifier { get; set; }

        [IldDisplay(Name = "OtherModifiers")]
        public string? OtherModifiers { get; set; }

        [IldDisplay(Name = "SizeModifier")]
        public string? SizeModifier { get; set; }

        [IldDisplay(Name = "TempModifier")]
        public string? TempModifier { get; set; }

        public int Calculate(int? externalBase = null, params int[]? modValues)
        {
            int totalCount = (externalBase ?? CoreUtils.ParseStringAsInt(Base, 0)) + CoreUtils.ParseStringAsInt(MagicModifier, 0) +
                CoreUtils.ParseStringAsInt(MiscModifier, 0) + CoreUtils.ParseStringAsInt(SizeModifier, 0) + CoreUtils.ParseStringAsInt(TempModifier, 0);

            if (modValues != null)
            {
                foreach (int item in modValues)
                {
                    totalCount += item;
                }
            }

            Total = totalCount.ToString();
            return totalCount;
        }

        public override string ToString()
        {
            return (Total ?? "0") + "(" + (Base ?? "0") + " + " + (MagicModifier ?? "0") + " + " + (MiscModifier ?? "0")
                + " + " + (OtherModifiers ?? "0") + " + " + (SizeModifier ?? "0") + " + " + (TempModifier ?? "0") + ")";
        }

        public bool Equals(CompoundModifier? other)
        {
            return other is not null
                    && other.Total == Total && other.Base == Base && other.MagicModifier == MagicModifier && other.MiscModifier == MiscModifier && 
                    other.OtherModifiers == OtherModifiers && other.SizeModifier == SizeModifier && other.TempModifier == TempModifier;
        }

        public override bool Equals(object? obj)
        {
            return obj is CompoundModifier f ? Equals(f) : false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Total?.GetHashCode() ?? 0, Base?.GetHashCode() ?? 0, MagicModifier?.GetHashCode() ?? 0, 
                MiscModifier?.GetHashCode() ?? 0, OtherModifiers?.GetHashCode() ?? 0, SizeModifier?.GetHashCode() ?? 0, TempModifier?.GetHashCode() ?? 0);
        }

        public static bool operator ==(CompoundModifier? left, CompoundModifier? right)
        {
            return left is null ? right is null : left.Equals(right);
        }

        public static bool operator !=(CompoundModifier? left, CompoundModifier? right)
        {
            return left is null ? right is not null : !left.Equals(right);
        }
    }

    public class Speed : IEquatable<Speed>
    {
        public string? Base { get; set; }
        public string? WithArmor { get; set; }
        public string? Fly { get; set; }
        public string? Swim { get; set; }
        public string? Climb { get; set; }
        public string? Burrow { get; set; }

        [JsonProperty("tempModifiers")]
        public string? TempModifier { get; set; }

        public override string ToString()
        {
            return "SPEED: " + (Base ?? "undefined") + (TempModifier == null ? "" : " (" + TempModifier + ")");
        }

        public bool Equals(Speed? other)
        {
            return other is not null
                    && other.Base == Base && other.WithArmor == WithArmor && other.Fly == Fly && other.Swim == Swim &&
                    other.Climb == Climb && other.Burrow == Burrow && other.TempModifier == TempModifier;
        }

        public override bool Equals(object? obj)
        {
            return obj is Speed f ? Equals(f) : false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Base?.GetHashCode() ?? 0, WithArmor?.GetHashCode() ?? 0, Fly?.GetHashCode() ?? 0,
                Swim?.GetHashCode() ?? 0, Climb?.GetHashCode() ?? 0, Burrow?.GetHashCode() ?? 0, TempModifier?.GetHashCode() ?? 0);
        }

        public static bool operator ==(Speed? left, Speed? right)
        {
            return left is null ? right is null : left.Equals(right);
        }

        public static bool operator !=(Speed? left, Speed? right)
        {
            return left is null ? right is not null : !left.Equals(right);
        }
    }

    public class ArmorClass
    {
        public string Total { get; set; } = "";
        public string Touch { get; set; } = "";
        public string FlatFooted { get; set; } = "";

        public string? SizeModifier { get; set; }
        public string? MiscModifier { get; set; }
        public string? OtherModifiers { get; set; }
        public string? NaturalArmor { get; set; }
        [JsonProperty("deflectionModifier")]
        public string? Deflection { get; set; }
        public string? ArmorBonus { get; set; }
        public string? ShieldBonus { get; set; }

        public List<AcItem> Items { get; set; } = new List<AcItem>();
        public AcItem ItemTotals { get; set; } = new AcItem();
    }

    public class Weapon : IEquatable<Weapon>
    {
        [JsonProperty("weapon"), IldDisplay(Name = "Name")]
        public string Name { get; set; } = "";

        [IldDisplay(Name = "Damage")]
        public string? Damage { get; set; }

        [JsonProperty("critical"), IldDisplay(Name = "Critical")]
        public string? CriticalRange { get; set; }

        [IldDisplay(Name = "Type")]
        public string? Type { get; set; }

        [IldDisplay(Name = "AttackBonus")]
        public string? AttackBonus { get; set; }

        [IldDisplay(Name = "Notes")]
        public string? Notes { get; set; }

        [IldDisplay(Name = "Range")]
        public string? Range { get; set; }

        [IldDisplay(Name = "Ammunition")]
        public string? Ammunition { get; set; }

        public override string ToString()
        {
            return string.IsNullOrEmpty(Name) ? "unnamed weapon" : Name;
        }

        public bool Equals(Weapon? other)
        {
            return other is not null
                    && other.Name == Name && other.Damage == Damage && other.CriticalRange == CriticalRange && other.Type == Type
                    && other.AttackBonus == AttackBonus && other.Notes == Notes && other.Range == Range && other.Ammunition == Ammunition;
        }

        public override bool Equals(object? obj)
        {
            return obj is Weapon f ? Equals(f) : false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name?.GetHashCode() ?? 0, Damage?.GetHashCode() ?? 0, CriticalRange?.GetHashCode() ?? 0, Type?.GetHashCode() ?? 0,
                    AttackBonus?.GetHashCode() ?? 0, Notes?.GetHashCode() ?? 0, Range?.GetHashCode() ?? 0, Ammunition?.GetHashCode() ?? 0);
        }

        public static bool operator ==(Weapon? left, Weapon? right)
        {
            return left is null ? right is null : left.Equals(right);
        }

        public static bool operator !=(Weapon? left, Weapon? right)
        {
            return left is null ? right is not null : !left.Equals(right);
        }
    }

    public class AcItem : IEquatable<AcItem>
    {
        [IldDisplay(Name = "Name")]
        public string? Name { get; set; } = "";

        [IldDisplay(Name = "Bonus")]
        public string? Bonus { get; set; } = "";

        [IldDisplay(Name = "Type")]
        public string? Type { get; set; } = "";

        [IldDisplay(Name = "ArmorCheckPenalty")]
        public string? ArmorCheckPenalty { get; set; } = "";

        [IldDisplay(Name = "SpellFailure")]
        public string? SpellFailure { get; set; } = "";

        [IldDisplay(Name = "Weight")]
        public string? Weight { get; set; } = "";

        [IldDisplay(Name = "Properties")]
        public string? Properties { get; set; } = "";

        public override string ToString()
        {
            return string.IsNullOrEmpty(Name) ? "unnamed" : Name;
        }

        public bool Equals(AcItem? other)
        {
            return other is not null
                    && other.Name == Name && other.Bonus == Bonus && other.Type == Type && other.ArmorCheckPenalty == ArmorCheckPenalty
                    && other.SpellFailure == SpellFailure && other.Weight == Weight && other.Properties == Properties;
        }

        public override bool Equals(object? obj)
        {
            return obj is AcItem f ? Equals(f) : false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name?.GetHashCode() ?? 0, Bonus?.GetHashCode() ?? 0, Type?.GetHashCode() ?? 0, ArmorCheckPenalty?.GetHashCode() ?? 0,
                    SpellFailure?.GetHashCode() ?? 0, Weight?.GetHashCode() ?? 0, Properties?.GetHashCode() ?? 0);
        }

        public static bool operator ==(AcItem? left, AcItem? right)
        {
            return left is null ? right is null : left.Equals(right);
        }

        public static bool operator !=(AcItem? left, AcItem? right)
        {
            return left is null ? right is not null : !left.Equals(right);
        }
    }

    public class Equipment : IEquatable<Equipment>
    {
        [IldDisplay(Name = "Name")]
        public string? Name { get; set; }

        [IldDisplay(Name = "Location")]
        public string? Location { get; set; }

        [IldDisplay(Name = "Type")]
        public string? Type { get; set; }

        [IldDisplay(Name = "Quantity")]
        public string? Quantity { get; set; } = "1";

        [IldDisplay(Name = "Weight")]
        public string? Weight { get; set; }

        [IldDisplay(Name = "Notes")]
        public string? Notes { get; set; }

        [IldDisplay(Name = "Equippable")]
        public bool Equippable { get; set; } = false;

        [IldDisplay(Name = "Equipped")]
        public bool Equipped { get; set; } = false;

        public override string ToString()
        {
            return (Name ?? "unnamed item") + (Quantity == null ? "" : " (" + Quantity + ")") + (Equipped ? " (equipped)" : "");
        }

        public bool Equals(Equipment? other)
        {
            return other is not null
                    && other.Name == Name && other.Location == Location && other.Type == Type && other.Quantity == Quantity &&
                    other.Weight == Weight && other.Notes == Notes && other.Equippable == Equippable && other.Equipped == Equipped;
        }

        public override bool Equals(object? obj)
        {
            return obj is Equipment f ? Equals(f) : false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name?.GetHashCode() ?? 0, Location?.GetHashCode() ?? 0, Type?.GetHashCode() ?? 0,
                Quantity?.GetHashCode() ?? 0, Weight?.GetHashCode() ?? 0, Notes?.GetHashCode() ?? 0, Equippable.GetHashCode(), Equipped.GetHashCode());
        }

        public static bool operator ==(Equipment? left, Equipment? right)
        {
            return left is null ? right is null : left.Equals(right);
        }

        public static bool operator !=(Equipment? left, Equipment? right)
        {
            return left is null ? right is not null : !left.Equals(right);
        }
    }

    public class SpecialAbility : IEquatable<SpecialAbility>
    {

        [IldDisplay(Name = "Name")]
        public string Name { get; set; } = "";

        [IldDisplay(Name = "Type")]
        public string? Type { get; set; }

        [IldDisplay(Name = "Notes")]
        public string? Notes { get; set; }

        public override string ToString()
        {
            return string.IsNullOrEmpty(Name) ? "unnamed" : Name;
        }

        public bool Equals(SpecialAbility? other)
        {
            return other is not null
                    && other.Name == Name && other.Type == Type && other.Notes == Notes;
        }

        public override bool Equals(object? obj)
        {
            return obj is SpecialAbility f ? Equals(f) : false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name?.GetHashCode() ?? 0, Type?.GetHashCode() ?? 0, Notes?.GetHashCode() ?? 0);
        }

        public static bool operator ==(SpecialAbility? left, SpecialAbility? right)
        {
            return left is null ? right is null : left.Equals(right);
        }

        public static bool operator !=(SpecialAbility? left, SpecialAbility? right)
        {
            return left is null ? right is not null : !left.Equals(right);
        }
    }

    public class HP
    {
        public string? Total { get; set; } = "0";
        public string? Wounds { get; set; } = "0";
        public string? NonLethal { get; set; }
    }

    public class SpellLevel : IEquatable<SpellLevel>
    {
        public string? TotalPerDay { get; set; }
        [JsonProperty("dc")]
        public string? SaveDC { get; set; }
        public string? TotalKnown { get; set; }
        public string? BonusSpells { get; set; }

        [JsonProperty("slotted")]
        public List<Spell>? Spells { get; set; }

        public bool Equals(SpellLevel? other)
        {
            bool equalsSoFar = other is not null
                    && other.TotalPerDay == TotalPerDay && other.SaveDC == SaveDC && other.TotalKnown == TotalKnown && other.BonusSpells == BonusSpells;

            if (other?.Spells == null && this.Spells == null)
            {
                return equalsSoFar && true;
            }
            else if (other?.Spells != null && this.Spells == null)
            {
                return false;
            }
            else if (other?.Spells == null && this.Spells != null)
            {
                return false;
            }
            else
            {
                return equalsSoFar && this.Spells!.SequenceEqual(other?.Spells ?? new List<Spell>());
            }
        }

        public override string ToString()
        {
            return (TotalPerDay ?? "0") + "+" + BonusSpells + ", DC " + SaveDC + ", " + (Spells?.Count ?? 0) + " spells";
        }

        public override bool Equals(object? obj)
        {
            return obj is SpellLevel sl && Equals(sl);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(TotalPerDay, TotalKnown, BonusSpells, SaveDC, Spells?.Count ?? 0);
        }

        public static bool operator ==(SpellLevel? left, SpellLevel? right)
        {
            return left is null ? right is null : left.Equals(right);
        }

        public static bool operator !=(SpellLevel? left, SpellLevel? right)
        {
            return left is null ? right is not null : !left.Equals(right);
        }
    }

    public class Spell : IEquatable<Spell>
    {
        public int Level { get; set; } = 0;
        public int Prepared { get; set; } = 0;
        public int Cast { get; set; } = 0;
        public string Name { get; set; } = "";
        public string School { get; set; } = "";
        public string Subschool { get; set; } = "";
        public string Notes { get; set; } = "";
        public bool AtWill { get; set; } = false;

        /// <summary>
        /// Get or set if this spell is being specially marked by the player. Marked spells may be spells that are "prepared" or
        /// that have other certain purposes; its purpose is intentionally vague, to allow players to use it for whatever purpose they want.
        /// </summary>
        public bool Marked { get; set; } = false;

        public override string ToString()
        {
            return (string.IsNullOrEmpty(Name) ? "unnamed spell" : Name) + " " + Level;
        }

        public bool Equals(Spell? other)
        {
            return other is not null
                    && other.Level == Level && other.Prepared == Prepared && other.Cast == Cast && other.Name == Name && other.School == School
                    && other.Subschool == Subschool && other.Notes == Notes && other.AtWill == AtWill && other.Marked == Marked;
        }

        public override bool Equals(object? obj)
        {
            return obj is Spell f ? Equals(f) : false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Level.GetHashCode(), Prepared.GetHashCode(), Cast.GetHashCode(), Name.GetHashCode(), School.GetHashCode(),
                Subschool.GetHashCode(), Notes.GetHashCode(), AtWill.GetHashCode());
        }

        public static bool operator ==(Spell? left, Spell? right)
        {
            return left is null ? right is null : left.Equals(right);
        }

        public static bool operator !=(Spell? left, Spell? right)
        {
            return left is null ? right is not null : !left.Equals(right);
        }
    }
}
