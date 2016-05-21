using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Channels;
using System.Windows.Forms;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.JScript;
using PoePickit.Extensions;
using System.Threading;
using PoePickit.Parser;

namespace PoePickit
{
    public class ToolTip
    {
        public List<ToolTipFrame> TtFrame = new List<ToolTipFrame>();

        private string _itemClassType;

        public string ArgText;
        public string ValueText;
        
        /*  public ToolTip(Item item)
        {
            
            if (!item.TtTypes.Contains(ToolTipTypes.Unknown))
            {
                foreach (var type in item.TtTypes)
                {
                    TtFrame.Add(FillToolTip(item, type));
                }
                
                ArgText = _itemClassType + "\n";
                ValueText = "\n";
                foreach (var frame in TtFrame)
                {
                    foreach (var line in frame.TtLines)
                    {
                        ArgText = ArgText + line.Arg + "\n";
                        ValueText = ValueText + line.Value + "\n";
                    }
                }
                _form.SetLeftText(ArgText);
                _form.SetRightText(ValueText);
               
            }
            if (item.FilterSuccess)
            {
                _form.BackColor = System.Drawing.Color.Khaki;
            }
            else
            {
                _form.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            }
        }*/

        public void Create(Item item)
        {
            if (!item.TtTypes.Contains(ToolTipTypes.Unknown))
            {
                foreach (var type in item.TtTypes)
                {
                    TtFrame.Add(FillToolTip(item, type));
                }

                ArgText = _itemClassType + "\n";
                ValueText = "\n";
                foreach (var frame in TtFrame)
                {
                    foreach (var line in frame.TtLines)
                    {
                        ArgText = ArgText + line.Arg + "\n";
                        ValueText = ValueText + line.Value + "\n";
                    }
                }
            }

            if (item.FilterSuccess)
            {
                //Forma.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(213)))), ((int)(((byte)(52)))));
            }
            else
            {
                //Forma.BackColor = System.Drawing.SystemColors.ScrollBar;
            }
        }

        public void Clear()
        {
            if (_itemClassType == null)
                return;
            TtFrame.Clear();
            _itemClassType = null;
            ArgText = null;
            ValueText = null;
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

        public void SetTtUnidentified()
        {
            ArgText = "Unidentified";
            ValueText = "";
        }

        private ToolTipFrame FillToolTip(Item item, ToolTipTypes ttType)
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
            string[] armourLines = {"ES", "AR", "EV"};
            string[] commonLines = {"MaxLife", "TotalRes", "Int", "Dex", "Str", "Rarity"};
            string[] bootsLines = {"MoveSpeed"};
            string[] glovesLines = {"IAS", "FlatPhys", "FlatLightning", "FlatFire", "FlatCold"};
            string[] quiverLines = {"TotalCrit", "CritDamage", "IAS", "WED"};
            string[] ringLines =
            {
                "TotalMaxLife", "TotalRes", "Str", "Int", "Dex", "Rarity", "WED", "FlatPhys",
                "FlatLightning", "FlatCold", "FlatFire", "CastSpeed", "CritDamage"
            };
            string[] amuletLines =
            {
                "TotalMaxLife", "TotalRes", "Str", "Int", "Dex", "Rarity", "WED", "FlatPhys",
                "FlatLightning", "FlatCold", "FlatFire", "CastSpeed", "CritDamage", "TotalCrit", "SpellDamage"
            };
            string[] beltLines = {"WED", "LocalPhys", "TotalMaxLife"};
            string[] spiritShields = {"SPD", "SpellCrit"};

            var lines = new List<TtLine>();
            var tooltip = new ToolTipFrame();


            //AddSeparator(ref lines);
            _itemClassType = item.ClassType;

            if (item.IsWeapon)
            {
                AddFrameSeparator(ref lines);
                foreach (var arg in DPSLines)
                {
                    FillToolTipLine(ref lines, item, arg, TtCraftTypes.NoCraft);
                }

                if (ttType == ToolTipTypes.PDPS)
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

                if (ttType == ToolTipTypes.EDPS)
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

                if (ttType == ToolTipTypes.SPD)
                {
                    /*AddSeparator(ref lines);
                    foreach (var arg in SPDLines)
                    {
                        FillToolTip(ref lines, item, arg, TtCraftTypes.NoCraft);
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
                        AddToolTipLine(ref lines, item.CraftTtArmour , item.CraftTtArmourPrice);
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
                    if (item.IsBoots)
                    {
                        foreach (var arg in bootsLines)
                        {
                            FillToolTipLine(ref lines, item, arg, TtCraftTypes.NoCraft);
                        }
                    }
                    else if (item.IsGloves)
                    {
                        foreach (var arg in glovesLines)
                        {
                            FillToolTipLine(ref lines, item, arg, TtCraftTypes.NoCraft);
                        }
                    }
                    else if (item.IsSpiritShield)
                    {
                        foreach (var arg in spiritShields)
                        {
                            FillToolTipLine(ref lines, item, arg, TtCraftTypes.NoCraft);
                        }
                    }
                }

                if (item.IsBelt)
                {
                    foreach (var arg in beltLines)
                    {
                        FillToolTipLine(ref lines, item, arg, TtCraftTypes.NoCraft);
                    }
                }
                else if (item.IsQuiver)
                {
                    foreach (var arg in quiverLines)
                    {
                        FillToolTipLine(ref lines, item, arg, TtCraftTypes.NoCraft);
                    }
                }
                else if (item.IsRing)
                {
                    foreach (var arg in ringLines)
                    {
                        FillToolTipLine(ref lines, item, arg, TtCraftTypes.NoCraft);
                    }
                }
                else if (item.IsAmulet)
                {
                    foreach (var arg in amuletLines)
                    {
                        FillToolTipLine(ref lines, item, arg, TtCraftTypes.NoCraft);
                    }
                }

                goto endOfToolTip;
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
            lines.Add(new TtLine { Arg = firstValue, Value = secondValue });
        }


        private void AddSeparator(ref List<TtLine> lines)
        {
            lines.Add(new TtLine {Arg = "------------------------------", Value = "--------------"});
        }

        private void AddFrameSeparator(ref List<TtLine> lines)
        {
            lines.Add(new TtLine {Arg = "=======================", Value = "==========" });
        }

        private void AddAffixesSeparator(ref List<TtLine> lines)
        {
            lines.Add(new TtLine { Arg = "---", Value = "---" });
        }

        private void AddCraftSeparator(ref List<TtLine> lines)
        {
            lines.Add(new TtLine { Arg = "---", Value = "---" });
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
    }
}
