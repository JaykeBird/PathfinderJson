using System;
using System.Collections.Generic;
using System.Windows;

namespace PathfinderJson
{
    public class SkillEditorFactory
    {

        public static List<SkillEditor> CreateEditors(PathfinderSheet ps, Window? owner = null)
        {
            List<SkillEditor> eds = new List<SkillEditor>();

            string? skillListFile = null;

            // check sheet settings first for relevant settings
            if (ps.SheetSettings != null)
            {
                // check if a specific skill list file is referenced
                if (ps.SheetSettings.ContainsKey("skillList"))
                {
                    string? loc = ps.SheetSettings["skillList"];

                    if (System.IO.File.Exists(loc ?? ""))
                    {
                        skillListFile = loc;
                    }
                }
            }

            SkillList sl = skillListFile == null ? SkillList.LoadStandardList() : SkillList.LoadFile(skillListFile);

            foreach (var item in sl.SkillEntries)
            {
                SkillEditor se = new SkillEditor();

                se.SkillName = item.DisplayName ?? (item.Name.Substring(0, 1).ToUpperInvariant() + item.Name.Substring(1));
                se.ModifierName = item.Modifier;
                se.OriginalModifierName = item.Modifier;
                se.HasSpecialization = item.HasSpecialization;
                se.InternalSkillName = item.Name;

                if (owner != null) se.OwnerWindow = owner;

                // set skill name to use with d20pfsrd.com
                se.InfoUrl = item.InfoUrl ?? "https://d20pfsrd.com/skills/";
                //string snl = se.SkillName.ToLowerInvariant();

                //if (snl.Contains("knowledge"))
                //{
                //    se.InfoUrl = "https://d20pfsrd.com/skills/knowledge";
                //}
                //else
                //{
                //    se.InfoUrl = "https://d20pfsrd.com/skills/" + snl.Replace(' ', '-');
                //}

                if (ps.Skills != null)
                {
                    try
                    {
                        Skill s = ps.Skills[item.Name] ?? new Skill();
                        se.LoadSkillData(s);
                    }
                    catch (KeyNotFoundException)
                    {
                        Skill s = new Skill();
                        s.Total = "0";
                        s.Ranks = "0";
                        s.ClassSkill = false;
                        se.LoadSkillData(s);
                    }
                }
                else
                {
                    Skill s = new Skill();
                    s.Total = "0";
                    s.Ranks = "0";
                    s.ClassSkill = false;
                    se.LoadSkillData(s);
                }

                eds.Add(se);
            }

            return eds;
        }

        public static Dictionary<string, (string name, string modifier, bool canEdit)> SkillEntries = new Dictionary<string, (string name, string modifier, bool canEdit)>()
        {
            { "acrobatics", ("Acrobatics", "DEX", false) },
            { "appraise", ("Appraise", "INT", false) },
            { "bluff", ("Bluff", "CHA", false) },
            { "climb", ("Climb", "STR", false) },
            { "craft1", ("Craft", "INT", true) },
            { "craft2", ("Craft", "INT", true) },
            { "craft3", ("Craft", "INT", true) },
            { "diplomacy", ("Diplomacy", "CHA", false) },
            { "disableDevice", ("Disable Device", "DEX", false) },
            { "disguise", ("Disguise", "CHA", false) },
            { "escapeArtist", ("Escape Artist", "DEX", false) },
            { "fly", ("Fly", "DEX", false) },
            { "handleAnimal", ("Handle Animal", "CHA", false) },
            { "heal", ("Heal", "WIS", false) },
            { "intimidate", ("Intimidate", "CHA", false) },
            { "knowledgeArcana", ("Knowledge (arcana)", "INT", false) },
            { "knowledgeDungeoneering", ("Knowledge (dungeoneering)", "INT", false) },
            { "knowledgeEngineering", ("Knowledge (engineering)", "INT", false) },
            { "knowledgeHistory", ("Knowledge (history)", "INT", false) },
            { "knowledgeGeography", ("Knowledge (geography)", "INT", false) },
            { "knowledgeLocal", ("Knowledge (local)", "INT", false) },
            { "knowledgeNature", ("Knowledge (nature)", "INT", false) },
            { "knowledgeNobility", ("Knowledge (nobility)", "INT", false) },
            { "knowledgePlanes", ("Knowledge (planes)", "INT", false) },
            { "knowledgeReligion", ("Knowledge (religion)", "INT", false) },
            { "linguistics", ("Linguistics", "INT", false) },
            { "perception", ("Perception", "WIS", false) },
            { "perform1", ("Perform", "CHA", true) },
            { "perform2", ("Perform", "CHA", true) },
            { "profession1", ("Profession", "WIS", true) },
            { "profession2", ("Profession", "WIS", true) },
            { "ride", ("Ride", "DEX", false) },
            { "senseMotive", ("Sense Motive", "WIS", false) },
            { "sleightOfHand", ("Sleight of Hand", "DEX", false) },
            { "spellcraft", ("Spellcraft", "INT", false) },
            { "stealth", ("Stealth", "DEX", false) },
            { "survival", ("Survival", "WIS", false) },
            { "swim", ("Swim", "STR", false) },
            { "useMagicDevice", ("Use Magic Device", "CHA", false) },
        };

    }
}
