using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Media;
using PoePricer.Parser;

namespace PoePricer
{
    public class PoeToolTip
    {
        public static readonly PoeToolTip Empty = new PoeToolTip();

        public string ArgText;

        public Color BackColor;
        public int FontSize;

        private string itemClassType;
        public Color TextColor;
        public List<ToolTipFrame> TtFrame = new List<ToolTipFrame>();
        public string ValueText;

        public PoeToolTip()
        {
            ArgText = "Unidentified";
            ValueText = string.Empty;
        }

        public PoeToolTip(Item item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            Initialize(item);
        }

        private void Initialize(Item item)
        {
            if (item.ItemRarity == Item.ItemRarityType.Unique)
            {
                BackColor = Colors.DarkGray;
                TextColor = Colors.SaddleBrown;
                FontSize = 11;
                FillUniqueItemToolTip(item);
                return;
            }

            if (!item.TtTypes.Contains(ToolTipTypes.Unknown))
            {
                foreach (var type in item.TtTypes)
                    TtFrame.Add(FillRareItemToolTip(item, type));

                ArgText = itemClassType + "\n";
                ValueText = "\n";
                foreach (var frame in TtFrame)
                    foreach (var line in frame.TtLines)
                    {
                        ArgText = ArgText + line.Arg + "\n";
                        ValueText = ValueText + line.Value + "\n";
                    }
            }
            if (item.FilterSuccess)
            {
                BackColor = Color.FromArgb(255, 224, 213, 52);
                TextColor = Colors.Black;
                FontSize = 11;
            }

            else
            {
                TextColor = Colors.Black;
                BackColor = Colors.DarkGray;
                FontSize = 11;
            }
        }

        public void Clear()
        {
            if (itemClassType == null)
                return;
            TtFrame.Clear();
            itemClassType = null;
            ArgText = null;
            ValueText = null;
        }

        public void SetTtUnidentified()
        {
            ArgText = "Unidentified";
            ValueText = "";
        }


        private void FillUniqueItemToolTip(Item item)
        {
            ArgText = item.Name + "\n";
            ValueText = "\n";
            if (item.UniqueDescription != "")
            {
                ArgText = ArgText + "------------\n" + item.UniqueDescription + "\n";
                ValueText = ValueText + "------------\n\n\n\n\n";
            }
            ArgText = ArgText + "------------\n" + item.UniqueTextLeft + "\n------------";
            ValueText = ValueText + "------------\n" + item.UniqueTextRight + "\n------------";
        }

        private ToolTipFrame FillRareItemToolTip(Item item, ToolTipTypes ttType)
        {
            string[] EDPSLines = {"EDPS", "EAPS", "ECrit", "ECritDamage"};
            string[] DPSLines =
            {
                "DPS", "PDPS", "EDPS", "Crit", "APS", "CritDamage", "TotalSPD", "SpellCrit",
                "CastSpeed", "FlatSPD"
            };
            string[] SPDLines = {"TotalSPD", "SPD", "FlatSPD", "SpellCrit", "CastSpeed", "SpellCritDamage"};
            string[] PDPSLines = {"PDPS", "PAPS", "PCrit", "PCritDamage"};
            string[] COCLines =
            {
                "COCTotalSPD", "COCSPD", "COCCrit", "COCAPS", "COCSpellCrit", "COCSpellCritDamage",
                "COCFlatSPD", "COCElemDamage"
            };
            string[] armourLines = {"ES", "AR", "EV", "TotalRes"};
            string[] commonLines = {"MaxLife", "Int", "Dex", "Str", "Rarity"};
            string[] bootsLines = {"MoveSpeed"};
            string[] glovesLines = {"IAS", "FlatPhys", "FlatLightning", "FlatFire", "FlatCold"};
            string[] quiverLines = {"TotalCrit", "CritDamage", "IAS", "WED"};
            string[] ringLines =
            {
                "TotalMaxLife", "Str", "Int", "Dex", "Rarity", "WED", "FlatPhys",
                "FlatLightning", "FlatCold", "FlatFire", "CastSpeed", "CritDamage"
            };
            string[] amuletLines =
            {
                "TotalMaxLife", "Str", "Int", "Dex", "Rarity", "WED", "FlatPhys",
                "FlatLightning", "FlatCold", "FlatFire", "CastSpeed", "CritDamage", "TotalCrit", "SPD"
            };
            string[] beltLines = {"WED", "LocalPhys", "TotalMaxLife"};
            string[] spiritShieldLines = {"SPD", "SpellCrit"};
            string[] resLines = {"TotalRes"};
            string[] links = {"Links"};

            var lines = new List<TtLine>();
            var tooltip = new ToolTipFrame();


            //AddSeparator(ref lines);
            itemClassType = item.ClassType.ToString();

            if (TtFrame.Count > 0)
                AddEmptyLines(ref lines);

            if (ttType == ToolTipTypes.LINKS)
            {
                AddFrameSeparator(ref lines);
                foreach (var arg in links)
                    FillToolTipLine(ref lines, item, arg, TtCraftTypes.NoCraft);
                goto endOfToolTip;
            }


            if (item.IsWeapon)
            {
                AddFrameSeparator(ref lines);
                foreach (var arg in DPSLines)
                {
                    FillToolTipLine(ref lines, item, arg, TtCraftTypes.NoCraft);
                }
                if (ttType == ToolTipTypes.Phys)
                {
                    if (!string.IsNullOrEmpty(item.CraftTtPDPS))
                    {
                        AddFrameSeparator(ref lines);
                        AddToolTipLine(ref lines, "", item.CraftTtPDPS);
                        AddToolTipLine(ref lines, "", item.CraftTtPDPSPrice);
                        AddCraftSeparator(ref lines);
                        foreach (var arg in PDPSLines)
                        {
                            FillToolTipLine(ref lines, item, arg, TtCraftTypes.Craft);
                        }

                        if (!string.IsNullOrEmpty(item.MultiTtPDPS) && (item.MultiTtPDPS != item.CraftTtPDPS))
                        {
                            AddFrameSeparator(ref lines);
                            AddToolTipLine(ref lines, "", item.MultiTtPDPS);
                            AddToolTipLine(ref lines, "", item.MultiTtPDPSPrice);
                            AddCraftSeparator(ref lines);
                            foreach (var arg in PDPSLines)
                            {
                                FillToolTipLine(ref lines, item, arg, TtCraftTypes.MultiCraft);
                            }
                        }
                    }
                    goto endOfToolTip;
                }

                if (ttType == ToolTipTypes.COC)
                {
                    if (!string.IsNullOrEmpty(item.CraftTtCOC))
                    {
                        AddFrameSeparator(ref lines);
                        AddToolTipLine(ref lines, item.CraftTtCOC);
                        AddToolTipLine(ref lines, item.CraftTtCOCPrice);
                        AddCraftSeparator(ref lines);
                        foreach (var arg in COCLines)
                        {
                            FillToolTipLine(ref lines, item, arg, TtCraftTypes.Craft);
                        }

                        if (!string.IsNullOrEmpty(item.MultiTtCOC) && (item.MultiTtCOC != item.CraftTtCOC))
                        {
                            AddFrameSeparator(ref lines);
                            AddToolTipLine(ref lines, item.MultiTtCOC);
                            AddToolTipLine(ref lines, item.MultiTtCOCPrice);
                            AddCraftSeparator(ref lines);
                            foreach (var arg in COCLines)
                            {
                                FillToolTipLine(ref lines, item, arg, TtCraftTypes.MultiCraft);
                            }
                        }
                    }
                    goto endOfToolTip;
                }

                if (ttType == ToolTipTypes.Elem)
                {
                    if (!string.IsNullOrEmpty(item.CraftTtEDPS))
                    {
                        AddFrameSeparator(ref lines);
                        AddToolTipLine(ref lines, item.CraftTtEDPS);
                        AddToolTipLine(ref lines, item.CraftTtEDPSPrice);
                        AddCraftSeparator(ref lines);
                        foreach (var arg in EDPSLines)
                        {
                            FillToolTipLine(ref lines, item, arg, TtCraftTypes.Craft);
                        }

                        if (!string.IsNullOrEmpty(item.MultiTtEDPS) && (item.MultiTtEDPS != item.CraftTtEDPS))
                        {
                            AddFrameSeparator(ref lines);
                            AddToolTipLine(ref lines, item.MultiTtEDPS);
                            AddToolTipLine(ref lines, item.MultiTtEDPSPrice);
                            AddSeparator(ref lines);
                            foreach (var arg in EDPSLines)
                            {
                                FillToolTipLine(ref lines, item, arg, TtCraftTypes.MultiCraft);
                            }
                        }
                    }
                    goto endOfToolTip;
                }

                if (ttType == ToolTipTypes.Spell)
                {
                    /*AddSeparator(ref lines);
                    foreach (var arg in SPDLines)
                    {
                        FillRareItemToolTip(ref lines, item, arg, TtCraftTypes.NoCraft);
                    }*/

                    if (!string.IsNullOrEmpty(item.CraftTtSPD))
                    {
                        AddFrameSeparator(ref lines);
                        AddToolTipLine(ref lines, item.CraftTtSPD);
                        AddToolTipLine(ref lines, item.CraftTtSPDPrice);
                        AddCraftSeparator(ref lines);
                        foreach (var arg in SPDLines)
                        {
                            FillToolTipLine(ref lines, item, arg, TtCraftTypes.Craft);
                        }

                        if (!string.IsNullOrEmpty(item.CraftTtSPD) && (item.CraftTtSPD != item.MultiTtSPD))
                        {
                            AddFrameSeparator(ref lines);
                            AddToolTipLine(ref lines, item.MultiTtSPD);
                            AddToolTipLine(ref lines, item.MultiTtSPDPrice);
                            AddCraftSeparator(ref lines);
                            foreach (var arg in SPDLines)
                            {
                                FillToolTipLine(ref lines, item, arg, TtCraftTypes.MultiCraft);
                            }
                        }
                    }
                    goto endOfToolTip;
                }
            }


            if (ttType == ToolTipTypes.Common)
            {
                AddFrameSeparator(ref lines);
                if (item.IsArmour)
                {
                    if (!string.IsNullOrEmpty(item.CraftTtArmour))
                    {
                        AddToolTipLine(ref lines, item.CraftTtArmour, item.CraftTtArmourPrice);
                        //AddToolTipLine(ref lines, item.CraftTtArmourPrice);
                        AddCraftSeparator(ref lines);
                        foreach (var arg in armourLines)
                            FillToolTipLine(ref lines, item, arg, TtCraftTypes.Craft);
                    }
                    else
                        foreach (var arg in armourLines)
                            FillToolTipLine(ref lines, item, arg, TtCraftTypes.NoCraft);
                    foreach (var arg in commonLines)
                    {
                        FillToolTipLine(ref lines, item, arg, TtCraftTypes.NoCraft);
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(item.CraftTtTotalRes))
                    {
                        AddToolTipLine(ref lines, item.CraftTtTotalRes, item.CraftTtTotalResPrice);
                        AddCraftSeparator(ref lines);
                        foreach (var arg in resLines)
                            FillToolTipLine(ref lines, item, arg, TtCraftTypes.Craft);
                    }
                    else
                        foreach (var arg in resLines)
                            FillToolTipLine(ref lines, item, arg, TtCraftTypes.NoCraft);
                }
                string[] typeLines = {};
                switch (item.ClassType)
                {
                    case ItemClassType.Amulet:
                        typeLines = amuletLines;
                        break;
                    case ItemClassType.Ring:
                        typeLines = ringLines;
                        break;
                    case ItemClassType.Quiver:
                        typeLines = quiverLines;
                        break;
                    case ItemClassType.Belt:
                        typeLines = beltLines;
                        break;
                    case ItemClassType.Boots:
                        typeLines = bootsLines;
                        break;
                    case ItemClassType.Gloves:
                        typeLines = glovesLines;
                        break;
                    case ItemClassType.Shield:
                        typeLines = spiritShieldLines;
                        break;
                }
                foreach (var arg in typeLines)
                {
                    FillToolTipLine(ref lines, item, arg, TtCraftTypes.NoCraft);
                }
            }


            endOfToolTip:
            AddAffixesSeparator(ref lines);
            AddToolTipLine(ref lines, item, "FreePref: " + (3 - item.Prefixes), "FreeSuff: " + (3 - item.Suffixes));
            tooltip.TtLines = lines.ToArray();
            return tooltip;
        }


        private void AddToolTipLine(ref List<TtLine> lines, Item item, string firstValue, string secondValue)
        {
            if (string.IsNullOrEmpty(firstValue) && string.IsNullOrEmpty(secondValue)) return;
            lines.Add(new TtLine {Arg = firstValue, Value = secondValue});
        }


        private void AddToolTipLine(ref List<TtLine> lines, string firstValue)
        {
            lines.Add(new TtLine {Arg = firstValue, Value = ""});
        }

        private void AddToolTipLine(ref List<TtLine> lines, string firstValue, string secondValue)
        {
            lines.Add(new TtLine {Arg = firstValue, Value = secondValue});
        }


        private void AddSeparator(ref List<TtLine> lines)
        {
            lines.Add(new TtLine {Arg = "------------", Value = "------------"});
        }

        private void AddEmptyLines(ref List<TtLine> lines)
        {
            lines.Add(new TtLine
            {
                Arg = "\n\n\u2191\u2191\u2191\u2191\u2191\u2191\u2191\n\u2193\u2193\u2193\u2193\u2193\u2193\u2193\n",
                Value = "\n\n\u2191\u2191\u2191\u2191\n\u2193\u2193\u2193\u2193\n"
            });
        }

        private void AddFrameSeparator(ref List<TtLine> lines)
        {
            lines.Add(new TtLine {Arg = "==========", Value = "=========="});
        }

        private void AddAffixesSeparator(ref List<TtLine> lines)
        {
            lines.Add(new TtLine {Arg = "---", Value = "---"});
        }

        private void AddCraftSeparator(ref List<TtLine> lines)
        {
            lines.Add(new TtLine {Arg = "---", Value = "---"});
        }

        private void FillToolTipLine(ref List<TtLine> lines, Item item, string arg, TtCraftTypes craftType)
        {
            string argFirst;
            var lineText = arg;
            var argSecond = arg;

            switch (craftType)
            {
                case TtCraftTypes.Craft:
                    argFirst = "Craft" + arg;
                    break;
                case TtCraftTypes.MultiCraft:
                    argFirst = "Multi" + arg;
                    break;
                case TtCraftTypes.NoCraft:
                    argFirst = "";
                    break;
                default:
                    argFirst = "";
                    break;
            }

            var valueSecond = item.Get(argSecond);
            if (valueSecond > 80)
            {
                valueSecond = (int) valueSecond;
            }

            var line = new TtLine();
            if (!string.IsNullOrEmpty(argFirst))
            {
                var valueFirst = item.Get(argFirst);
                var valueFirstLo = item.Get(argFirst + "Lo");
                if (valueFirst > 80)
                {
                    valueFirst = (int) valueFirst;
                    valueFirstLo = (int) valueFirstLo;
                }

                if (valueFirst > valueSecond)
                {
                    line.Arg = lineText;
                    if (valueFirst > valueFirstLo)
                        line.Value = valueFirstLo + "-" + valueFirst;
                    else
                        line.Value = valueSecond.ToString();
                }
                else if (valueSecond > 0)
                {
                    line.Arg = lineText;
                    line.Value = valueSecond.ToString();
                }
                else
                {
                    return;
                }
            }
            else
            {
                if (valueSecond > 0)
                {
                    line.Arg = lineText;
                    line.Value = item.Get(argSecond).ToString();
                }
                else
                {
                    return;
                }
            }
            lines.Add(line);
        }

        public struct ToolTipFrame
        {
            public TtLine[] TtLines;
        }

        public struct TtLine
        {
            public string Arg;
            public string ArgColor;
            public string Value;
            public string ValueColor;
        }

        private enum TtCraftTypes
        {
            NoCraft,
            Craft,
            MultiCraft
        }
    }
}