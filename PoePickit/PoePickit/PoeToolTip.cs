using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Media;
using PoePricer.Parser;

namespace PoePricer
{
    public class PoeToolTip
    {
        private const int MaxSuffixCount = 3;
        private const int MaxPrefixCount = 3;
        private const int MaxFlatValue = 80;

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

        internal PoeToolTip(Item item)
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


            //AddSeparator(lines);
            itemClassType = item.ClassType.ToString();

            if (TtFrame.Count > 0)
                AddEmptyLines(lines);

            if (ttType == ToolTipTypes.LINKS)
            {
                AddFrameSeparator(lines);
                foreach (var arg in links)
                    FillToolTipLine(lines, item, arg, TtCraftTypes.NoCraft);
                goto endOfToolTip;
            }


            if (item.IsWeapon)
            {
                AddFrameSeparator(lines);
                foreach (var arg in DPSLines)
                {
                    FillToolTipLine(lines, item, arg, TtCraftTypes.NoCraft);
                }
                if (ttType == ToolTipTypes.Phys)
                {
                    if (!string.IsNullOrEmpty(item.CraftTtPDPS))
                    {
                        AddFrameSeparator(lines);
                        AddToolTipLine(lines, "", item.CraftTtPDPS);
                        AddToolTipLine(lines, "", item.CraftTtPDPSPrice);
                        AddCraftSeparator(lines);
                        foreach (var arg in PDPSLines)
                        {
                            FillToolTipLine(lines, item, arg, TtCraftTypes.Craft);
                        }

                        if (!string.IsNullOrEmpty(item.MultiTtPDPS) && (item.MultiTtPDPS != item.CraftTtPDPS))
                        {
                            AddFrameSeparator(lines);
                            AddToolTipLine(lines, "", item.MultiTtPDPS);
                            AddToolTipLine(lines, "", item.MultiTtPDPSPrice);
                            AddCraftSeparator(lines);
                            foreach (var arg in PDPSLines)
                            {
                                FillToolTipLine(lines, item, arg, TtCraftTypes.MultiCraft);
                            }
                        }
                    }
                    goto endOfToolTip;
                }

                if (ttType == ToolTipTypes.COC)
                {
                    if (!string.IsNullOrEmpty(item.CraftTtCOC))
                    {
                        AddFrameSeparator(lines);
                        AddToolTipLine(lines, item.CraftTtCOC);
                        AddToolTipLine(lines, item.CraftTtCOCPrice);
                        AddCraftSeparator(lines);
                        foreach (var arg in COCLines)
                        {
                            FillToolTipLine(lines, item, arg, TtCraftTypes.Craft);
                        }

                        if (!string.IsNullOrEmpty(item.MultiTtCOC) && (item.MultiTtCOC != item.CraftTtCOC))
                        {
                            AddFrameSeparator(lines);
                            AddToolTipLine(lines, item.MultiTtCOC);
                            AddToolTipLine(lines, item.MultiTtCOCPrice);
                            AddCraftSeparator(lines);
                            foreach (var arg in COCLines)
                            {
                                FillToolTipLine(lines, item, arg, TtCraftTypes.MultiCraft);
                            }
                        }
                    }
                    goto endOfToolTip;
                }

                if (ttType == ToolTipTypes.Elem)
                {
                    if (!string.IsNullOrEmpty(item.CraftTtEDPS))
                    {
                        AddFrameSeparator(lines);
                        AddToolTipLine(lines, item.CraftTtEDPS);
                        AddToolTipLine(lines, item.CraftTtEDPSPrice);
                        AddCraftSeparator(lines);
                        foreach (var arg in EDPSLines)
                        {
                            FillToolTipLine(lines, item, arg, TtCraftTypes.Craft);
                        }

                        if (!string.IsNullOrEmpty(item.MultiTtEDPS) && (item.MultiTtEDPS != item.CraftTtEDPS))
                        {
                            AddFrameSeparator(lines);
                            AddToolTipLine(lines, item.MultiTtEDPS);
                            AddToolTipLine(lines, item.MultiTtEDPSPrice);
                            AddSeparator(lines);
                            foreach (var arg in EDPSLines)
                            {
                                FillToolTipLine(lines, item, arg, TtCraftTypes.MultiCraft);
                            }
                        }
                    }
                    goto endOfToolTip;
                }

                if (ttType == ToolTipTypes.Spell)
                {
                    /*AddSeparator(lines);
                    foreach (var arg in SPDLines)
                    {
                        FillRareItemToolTip(lines, item, arg, TtCraftTypes.NoCraft);
                    }*/

                    if (!string.IsNullOrEmpty(item.CraftTtSPD))
                    {
                        AddFrameSeparator(lines);
                        AddToolTipLine(lines, item.CraftTtSPD);
                        AddToolTipLine(lines, item.CraftTtSPDPrice);
                        AddCraftSeparator(lines);
                        foreach (var arg in SPDLines)
                        {
                            FillToolTipLine(lines, item, arg, TtCraftTypes.Craft);
                        }

                        if (!string.IsNullOrEmpty(item.CraftTtSPD) && (item.CraftTtSPD != item.MultiTtSPD))
                        {
                            AddFrameSeparator(lines);
                            AddToolTipLine(lines, item.MultiTtSPD);
                            AddToolTipLine(lines, item.MultiTtSPDPrice);
                            AddCraftSeparator(lines);
                            foreach (var arg in SPDLines)
                            {
                                FillToolTipLine(lines, item, arg, TtCraftTypes.MultiCraft);
                            }
                        }
                    }
                    goto endOfToolTip;
                }
            }


            if (ttType == ToolTipTypes.Common)
            {
                AddFrameSeparator(lines);
                if (item.IsArmour)
                {
                    if (!string.IsNullOrEmpty(item.CraftTtArmour))
                    {
                        AddToolTipLine(lines, item.CraftTtArmour, item.CraftTtArmourPrice);
                        //AddToolTipLine(lines, item.CraftTtArmourPrice);
                        AddCraftSeparator(lines);
                        foreach (var arg in armourLines)
                            FillToolTipLine(lines, item, arg, TtCraftTypes.Craft);
                    }
                    else
                        foreach (var arg in armourLines)
                            FillToolTipLine(lines, item, arg, TtCraftTypes.NoCraft);
                    foreach (var arg in commonLines)
                    {
                        FillToolTipLine(lines, item, arg, TtCraftTypes.NoCraft);
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(item.CraftTtTotalRes))
                    {
                        AddToolTipLine(lines, item.CraftTtTotalRes, item.CraftTtTotalResPrice);
                        AddCraftSeparator(lines);
                        foreach (var arg in resLines)
                            FillToolTipLine(lines, item, arg, TtCraftTypes.Craft);
                    }
                    else
                        foreach (var arg in resLines)
                            FillToolTipLine(lines, item, arg, TtCraftTypes.NoCraft);
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
                    FillToolTipLine(lines, item, arg, TtCraftTypes.NoCraft);
                }
            }


            endOfToolTip:
            AddAffixesSeparator(lines);
            AddToolTipLine(lines, $"FreePref: {MaxPrefixCount - item.Prefixes}", $"FreeSuff: {MaxSuffixCount - item.Suffixes}");
            tooltip.TtLines = lines.ToArray();
            return tooltip;
        }


        private void AddToolTipLine(ICollection<TtLine> lines, string firstValue, string secondValue)
        {
            if (string.IsNullOrEmpty(firstValue) && string.IsNullOrEmpty(secondValue)) return;
            lines.Add(new TtLine {Arg = firstValue, Value = secondValue});
        }


        private void AddToolTipLine(ICollection<TtLine> lines, string firstValue)
        {
            lines.Add(new TtLine {Arg = firstValue, Value = string.Empty});
        }

        private void AddSeparator(ICollection<TtLine> lines)
        {
            lines.Add(new TtLine {Arg = "------------", Value = "------------"});
        }

        private void AddEmptyLines(ICollection<TtLine> lines)
        {
            lines.Add(new TtLine
            {
                Arg = "\n\n\u2191\u2191\u2191\u2191\u2191\u2191\u2191\n\u2193\u2193\u2193\u2193\u2193\u2193\u2193\n",
                Value = "\n\n\u2191\u2191\u2191\u2191\n\u2193\u2193\u2193\u2193\n"
            });
        }

        private void AddFrameSeparator(ICollection<TtLine> lines)
        {
            lines.Add(new TtLine {Arg = "==========", Value = "=========="});
        }

        private void AddAffixesSeparator(ICollection<TtLine> lines)
        {
            lines.Add(new TtLine {Arg = "---", Value = "---"});
        }

        private void AddCraftSeparator(ICollection<TtLine> lines)
        {
            lines.Add(new TtLine {Arg = "---", Value = "---"});
        }

        private void FillToolTipLine(ICollection<TtLine> lines, Item item, string arg, TtCraftTypes craftType)
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
                    argFirst = string.Empty;
                    break;
                default:
                    argFirst = string.Empty;
                    break;
            }

            var valueSecond = item.Get<double>(argSecond);
            if (valueSecond > MaxFlatValue)
            {
                valueSecond = (int) valueSecond;
            }

            var line = new TtLine();
            if (!string.IsNullOrEmpty(argFirst))
            {
                var valueFirst = item.Get<double>(argFirst);
                var valueFirstLo = item.Get<double>(argFirst + "Lo");
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
                    line.Value = item.Get<double>(argSecond);
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
            public object Value;
        }

        private enum TtCraftTypes
        {
            NoCraft,
            Craft,
            MultiCraft
        }
    }
}