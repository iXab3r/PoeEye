using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using PoePricer.Extensions;
using PoePricer.Parser;

namespace PoePricer
{
    public class Item
    {
        public enum AffixSolution
        {
            Missing,
            Positive,
            Negative,
            Uncertain
        }

        public enum AffixValueType
        {
            Int,
            Double,
            String
        }

        public enum ItemRarityType
        {
            Unknown,
            Normal,
            Magic,
            Rare,
            Unique,
            WrongItem
        }

        public enum ParseResult
        {
            Success,
            Unidentified,
            NotRareOrUnique,
            WrongDataText
        }

        public int AccuracyRating = 0;
        public int AffixCrit = 0;
        public int AffixCritDamage = 0;
        public byte Affixes;
        public int AffixSpellCrit = 0;
        public int AllRes = 0;
        public int AllStat = 0;

        //>>>>>>>>>>>>>>>>>>>>>>>>>>WEAPON STATS
        public double APS;
        //>>>>>>>>>>Armour stats
        public double AR;
        public double BaseAPS;

        //base
        public int BaseAR;
        public double BaseCC;
        public int BaseDamageHi;

        public int BaseDamageLo;
        public int BaseES;
        public int BaseEV;
        public string BaseType;

        //suffixes

        public int Block = 0;
        public int BowGem = 0;
        public int CastSpeed = 0;
        public int ChaosGem = 0;
        public int ChaosRes = 0;
        public ItemClassType ClassType;
        public int ColdGem = 0;
        public int ColdRes = 0;
        public double CraftAR;
        public double CraftARLo;
        public double CraftCastSpeed;
        public double CraftCastSpeedLo;
        public double CraftCOCAPS;
        public double CraftCOCAPSLo;
        public double CraftCOCCrit;
        public double CraftCOCCritLo;
        public double CraftCOCFlatSPD;
        public double CraftCOCFlatSPDLo;
        public double CraftCOCLocalElemDamage;
        public double CraftCOCLocalElemDamageLo;
        public double CraftCOCSPD;

        //COC
        public double CraftCOCSPDLo;
        public double CraftCOCSpellCrit;
        public double CraftCOCSpellCritDamage;
        public double CraftCOCSpellCritDamageLo;
        public double CraftCOCSpellCritLo;
        public double CraftCOCTotalSPD;
        public double CraftCOCTotalSPDLo;
        public double CraftEAPS;
        public double CraftEAPSLo;
        public double CraftECrit;
        public double CraftECritDamage;
        public double CraftECritDamageLo;
        public double CraftECritLo;
        public double CraftEDPS;

        public double CraftEDPSLo;
        public double CraftES;
        public double CraftESLo;
        public double CraftEV;
        public double CraftEVLo;
        public double CraftFlatSPD;
        public double CraftFlatSPDLo;
        public double CraftLocalElemDamage;
        public double CraftLocalElemDamageLo;
        public double CraftPAPS;
        public double CraftPAPSLo;
        public double CraftPCrit;
        public double CraftPCritDamage;
        public double CraftPCritDamageLo;
        public double CraftPCritLo;
        public double CraftPDPS;


        public double CraftPDPSLo;
        public double CraftSPD;
        public double CraftSPDLo;
        public double CraftSpellCrit;
        public double CraftSpellCritDamage;
        public double CraftSpellCritDamageLo;
        public double CraftSpellCritLo;
        public double CraftTotalRes;
        public double CraftTotalResLo;
        public double CraftTotalSPD;
        public double CraftTotalSPDLo;
        public int CraftTotalSpellCrit;

        public string CraftTtArmour;
        public string CraftTtArmourPrice;
        public string CraftTtCOC;
        public string CraftTtCOCPrice;
        public string CraftTtEDPS;
        public string CraftTtEDPSPrice;
        public string CraftTtPDPS;
        public string CraftTtPDPSPrice;
        public string CraftTtSPD;
        public string CraftTtSPDPrice;
        public string CraftTtTotalRes;
        public string CraftTtTotalResPrice;

        //crit
        public double Crit;
        public double CritDamage;
        public int Dex = 0;
        public int DOT = 0;
        public double DPS;
        public double EAPS;
        public double ECrit;
        public double ECritDamage;

        //elem
        public double EDPS;
        public double ES;
        public double EV;

        public bool FilterSuccess = false;
        public int FireGem = 0;
        public int FireRes = 0;
        public int FlatAR = 0;
        public int FlatChaos;
        public int FlatCold;
        public int FlatColdSPD = 0;
        //<<<<<<<<<<<<<<<<<<<<<<<<<<WEAPON STATS

        public double FlatElem;
        public int FlatES = 0;
        public int FlatEV = 0;
        public int FlatFire;
        public int FlatFireSPD = 0;
        public int FlatLightning;
        public int FlatLightningSPD = 0;
        public int FlatPhys;
        public double FlatSPD;
        public int GlobalCrit = 0;

        public string GripType;
        public int IAS = 0;

        public int iLevel;
        public int ImplicitCastSpeed;
        //Implicit
        public int ImplicitCrit;
        public int ImplicitCritDamage = 0;
        public int ImplicitGlobalCrit = 0;
        public int ImplicitLocalElemDamage = 0;
        public int ImplicitSPD;
        public int Int = 0;
        public bool IsAccuracyRating;
        public AffixSolution IsAccuracyRatingAff;
        public bool IsAffixCrit;
        public bool IsAffixCritDamage;
        public bool IsAffixSpellCrit;
        public bool IsAllRes;
        public bool IsAllStat;
        public bool IsArmour;
        public bool IsBlock;
        public bool IsBowGem;
        public bool IsCastSpeed;
        public bool IsChaosGem;
        public bool IsChaosRes;
        public bool IsColdGem;
        public bool IsColdRes;
        public AffixSolution IsComboLocalArmourAff;
        public AffixSolution IsComboLocalPhysAff;

        //ComboSpMana
        public AffixSolution IsComboSpellDamageAff;
        public bool IsCorrupted;
        public bool IsDex;
        public bool IsDOT;
        public bool IsFireGem;
        public bool IsFireRes;
        public bool IsFlatAR;
        public bool IsFlatChaos;
        public bool IsFlatCold;
        public bool IsFlatColdSPD;
        public bool IsFlatES;
        public bool IsFlatEV;
        public bool IsFlatFire;
        public bool IsFlatFireSPD;
        public bool IsFlatLightning;
        public bool IsFlatLightningSPD;
        public bool IsFlatPhys;
        public bool IsGlobalCrit;
        public bool IsHasMTXEffect;
        public bool IsIAS;
        public int IsImplicitCastSpeed;
        public bool IsImplicitCritDamage;
        public bool IsImplicitLocalElemDamage;
        public bool IsImplicitSPD;
        public bool IsImpplicitGlobalCrit;
        public bool IsInt;
        public bool IsLevelGem;
        public bool IsLifeLeech;
        public AffixSolution IsLightAccuracyRatingAff;
        public bool IsLightningGem;
        public bool IsLightningRes;
        public bool IsLightRadius;
        public bool IsLocalAccuracyRating;
        public bool IsLocalArmour;
        public AffixSolution IsLocalArmourAff;
        public bool IsLocalColdDamage;
        public bool IsLocalElemDamage;
        public bool IsLocalFireDamage;
        public bool IsLocalLightDamage;
        public bool IsLocalPhys;

        //PhysAcc
        public AffixSolution IsLocalPhysAff;
        public bool IsManaLeech;
        public bool IsMaxLife;
        public bool IsMaxMana;
        public AffixSolution IsMaxManaAff;
        public bool IsMeleeGem;
        public bool IsMinionGem;
        public bool IsMirrored;
        public bool IsMoveSpeed;
        public bool IsNote;
        public AffixSolution IsPrefixRarity;
        public bool IsProjSpeed;
        public bool IsRarity;
        public bool IsSceptre;
        public bool IsSPD;
        public AffixSolution IsSpellDamageAff;
        public bool IsSpiritShield;
        public bool IsStr;
        public bool IsStunRecovery;
        //<<<<<<<<<<<<Armour stats

        //comboflags 


        //ComboLocalArmourStunRecovery

        public AffixSolution IsStunRecoveryAff;
        public bool IsStunTheshold;

        //Rarity
        public AffixSolution IsSuffixRarity;

        //Flags

        public bool IsUnidentified;

        //ClassFlags
        public bool IsWeapon;
        public bool IsWED;

        //ItemDataText
        public string[] ItemAffixData;
        public string[] ItemImplicitData;

        public ItemRarityType ItemRarity;
        public int LevelGem = 0;
        public double LifeLeech = 0;
        public int LightningGem = 0;
        public int LightningRes = 0;
        public int LightRadius = 0;
        public int Links;
        public int LocalAccuracyRating = 0;
        public int LocalArmour = 0;
        public int LocalColdDamage = 0;
        public int LocalElemDamage;
        public int LocalFireDamage = 0;
        public int LocalLightDamage = 0;
        public int LocalPhys = 0;
        public double ManaLeech = 0;
        public int MaxLife = 0;
        public int MaxMana = 0;
        public int MeleeGem = 0;
        public int MinionGem = 0;
        public int MoveSpeed = 0;
        public double MultiCastSpeed;
        public double MultiCastSpeedLo;
        public double MultiCOCAPS;
        public double MultiCOCAPSLo;
        public double MultiCOCCrit;
        public double MultiCOCCritLo;
        public double MultiCOCFlatSPD;
        public double MultiCOCFlatSPDLo;
        public double MultiCOCLocalElemDamage;
        public double MultiCOCLocalElemDamageLo;
        public double MultiCOCSPD;

        public double MultiCOCSPDLo;
        public double MultiCOCSpellCrit;
        public double MultiCOCSpellCritDamage;
        public double MultiCOCSpellCritDamageLo;
        public double MultiCOCSpellCritLo;
        public double MultiCOCTotalSPD;
        public double MultiCOCTotalSPDLo;
        public double MultiEAPS;
        public double MultiEAPSLo;
        public double MultiECrit;
        public double MultiECritDamage;
        public double MultiECritDamageLo;
        public double MultiECritLo;
        public double MultiEDPS;

        public double MultiEDPSLo;
        public double MultiFlatSPD;
        public double MultiFlatSPDLo;
        public double MultiLocalElemDamage;
        public double MultiLocalElemDamageLo;
        public double MultiPAPS;
        public double MultiPAPSLo;
        public double MultiPCrit;
        public double MultiPCritDamage;
        public double MultiPCritDamageLo;
        public double MultiPCritLo;
        public double MultiPDPS;

        public double MultiPDPSLo;
        public double MultiSPD;
        public double MultiSPDLo;
        public double MultiSpellCrit;
        public double MultiSpellCritDamage;
        public double MultiSpellCritDamageLo;
        public double MultiSpellCritLo;
        public double MultiTotalSPD;
        public double MultiTotalSPDLo;
        public string MultiTtCOC;
        public string MultiTtCOCPrice;
        public string MultiTtEDPS;
        public string MultiTtEDPSPrice;
        public string MultiTtPDPS;
        public string MultiTtPDPSPrice;
        public string MultiTtSPD;
        public string MultiTtSPDPrice;
        public string Name;
        public double PAPS;
        public double PCrit;
        public double PCritDamage;
        //phys
        public double PDPS;


        //
        public byte Prefixes;
        public int ProjSpeed = 0;

        //affixes
        public int Rarity = 0;
        public int SPD;
        public double SpellCrit;
        public double SpellCritDamage;
        public int Str = 0;
        public int StunRecovery = 0;
        public int StunTreshold = 0;
        public bool Success;
        public byte Suffixes;


        //spell
        public double TotalCastSpeed;
        public double TotalCrit;
        public int TotalMaxLife;
        public double TotalRes;
        public double TotalSPD;
        public int TotalSpellCrit;


        public string TradeNoteMessage;
        //tooltips
        public string Tt;
        public List<ToolTipTypes> TtTypes = new List<ToolTipTypes> {ToolTipTypes.Unknown};
        public string UniqueDescription = "";
        public string UniqueText = "";

        //unique affix text
        public string UniqueTextLeft = "";
        public string UniqueTextRight = "";
        public byte UnsolvedAffixes = 0;


        //prefixes
        public int WED = 0;


        public bool IsArgExist(string name)
        {
            return GetType().GetField(name) != null;
        }

        public dynamic Get(string fieldName)
        {
            try
            {
                return GetType().GetField(fieldName).GetValue(this);
            }
            catch (Exception)
            {
                Console.WriteLine($"[Item.Get]Wrong arg:{fieldName}");
                return -1;
            }
        }

        public void SetNumericalValue(string fieldName, string fieldValue)
        {
            try
            {
                GetType().GetField(fieldName);
            }
            catch (Exception)
            {
                Console.WriteLine($"[Item.SetNumericValue]Wrong fieldName: {fieldName}");
                throw;
            }

            int intvalue;
            double doublevalue;

            if (int.TryParse(fieldValue, out intvalue))
            {
                try
                {
                    GetType().GetField(fieldName).SetValue(this, Get(fieldName) + intvalue);
                    return;
                }
                catch (Exception)
                {
                    Console.WriteLine($"[Item.SetNumericValue]Wrong fieldValue: {fieldValue} for fieldName: {fieldName}");
                    throw;
                }
            }
            if (!double.TryParse(fieldValue, out doublevalue)) return;
            try
            {
                GetType().GetField(fieldName).SetValue(this, Get(fieldName) + doublevalue);
            }
            catch (Exception)
            {
                Console.WriteLine($"[Item.SetNumericValue]Wrong fieldValue: {fieldValue} for fieldName: {fieldName}");
                throw;
            }
        }

        public bool SetFlagValue(string fieldName, bool flagValue)
        {
            try
            {
                GetType().GetField(fieldName).SetValue(this, flagValue);
                return true;
            }
            catch (Exception)
            {
                Console.WriteLine($"[Item.Set]Wrong fieldName: {fieldName}");
                return false;
            }
        }

        private bool ParseAffixLine(string line, AffixesSource affixes)
        {
            foreach (var affixLineSource in affixes.AffixesLines)
            {
                var match = affixLineSource.AffixRegExp.Match(line);
                if (match.Success)
                {
                    for (var i = 0; i < affixLineSource.AffixLineArgs.Length; i++)
                    {
                        if (affixLineSource.AffixLineArgs[i] == "") continue;

                        if (affixLineSource.AffixLineArgs[i].StartsWith("Is"))
                        {
                            SetFlagValue(affixLineSource.AffixLineArgs[i], true);
                        }
                        else
                        {
                            if (match.Groups["valueFirst"].Success)
                                SetNumericalValue(affixLineSource.AffixLineArgs[i], match.Groups["valueFirst"].Value);
                            else
                            {
                                Console.WriteLine(
                                    $"[{Name}.ParseAffixData.Affixes] Wrong argMod {affixLineSource.ArgMods[i]} for arg: {affixLineSource.AffixLineArgs[i]} \nLine :\n {line} \nRegex:\n {affixLineSource.AffixRegExp}");
                                return false;
                            }

                            if (affixLineSource.ArgMods.Contains("2"))
                                if (match.Groups["valueSecond"].Success)
                                    SetNumericalValue(affixLineSource.AffixLineArgs[i],
                                        match.Groups["valueSecond"].Value);
                                else
                                {
                                    Console.WriteLine(
                                        $"[{Name}.ParseAffixData.Affixes] Wrong argMod {affixLineSource.ArgMods[i]} for arg: {affixLineSource.AffixLineArgs[i]} \nLine :\n {line} \nRegex:\n {affixLineSource.AffixRegExp}");
                                    return false;
                                }
                            if (affixLineSource.ArgMods.Contains("3"))
                                if (match.Groups["valueThird"].Success)
                                    SetNumericalValue(affixLineSource.AffixLineArgs[i], match.Groups["valueThird"].Value);
                                else
                                {
                                    Console.WriteLine(
                                        $"[{Name}.ParseAffixData.Affixes] Wrong argMod {affixLineSource.ArgMods[i]} for arg: {affixLineSource.AffixLineArgs[i]} \nLine :\n {line} \nRegex:\n {affixLineSource.AffixRegExp}");
                                    return false;
                                }
                        }
                    }

                    return true;
                }
            }
            return false;
        }

        public void ParseAffixData(IDictionary<AffixTypes, AffixesSource> knownAffixes)
        {
            foreach (var line in ItemAffixData)
            {
                if (line == "")
                    return;
                if (ParseAffixLine(line, knownAffixes[AffixTypes.affixes]))
                {
                    Affixes++;
                    continue;
                }
                if (Prefixes < 3)
                {
                    if (ParseAffixLine(line, knownAffixes[AffixTypes.prefixes]))
                    {
                        Prefixes++;
                        continue;
                    }
                }
                if (Suffixes < 3)
                {
                    if (ParseAffixLine(line, knownAffixes[AffixTypes.suffixes]))
                    {
                        Suffixes++;
                        continue;
                    }
                }
                Console.WriteLine($"[Item.ParseAffixData] Unknown AffixLnie : {line}");
                MessageBox.Show($"UnknownAffixLine: {line}");
            }
        }

        private void ParseImplicitData(IDictionary<AffixTypes, AffixesSource> knownAffixes)
        {
            if (ItemImplicitData != null)
                foreach (var line in ItemImplicitData)
                {
                    if (ParseAffixLine(line, knownAffixes[AffixTypes.implicitaff])) continue;
                    Console.WriteLine($"[Item.ParseAffixData] Unknown ImplicitAffixLine: {line}");
                }
        }


        public ParseResult ParseItemDataText(string itemDataText,
            IDictionary<AffixBracketType, AffixBracketsSource> affixBrackets,
            IDictionary<BaseItemTypes, BaseItemsSource> baseItems, IDictionary<AffixTypes, AffixesSource> knownAffixes,
            IDictionary<ParseRegEx, Regex> knownRegexes, UniqueAffixSource knownUniques)
        {
            //seperate DataText for blocks via splitting with char "`"


            var itemDataParts = itemDataText.Replace("\r\n--------\r\n", "`").Split('`');

            var lastPartIndex = itemDataParts.Length - 1;


            //check rarity
            ParseItemRarity(itemDataParts[0], knownRegexes);

            if (ItemRarity == ItemRarityType.WrongItem)
                return ParseResult.WrongDataText;

            if ((ItemRarity != ItemRarityType.Rare) && (ItemRarity != ItemRarityType.Unique))
                return ParseResult.NotRareOrUnique;


            //check Identify
            if (itemDataText.Contains("Unidentified"))
            {
                IsUnidentified = true;
                return ParseResult.Unidentified;
            }

            //parsing item level
            ParseItemLevel(itemDataParts);

            //check TradingNote,Effects, MTX etc.
            ParseEffects(itemDataParts, ref lastPartIndex, knownRegexes);

            //get Name,BaseType from first DataTextBlock
            itemDataParts[0] = itemDataParts[0].Replace("<<set:MS>><<set:M>><<set:S>>", "");
            var nameDataPart = itemDataParts[0].Replace("\r\n", "`").Split('`');


            Name = nameDataPart[1];
            BaseType = nameDataPart[2];
            var firstDataStatLine = itemDataParts[1].Replace("\r\n", "`").Split('`')[0];

            //parse ClassType
            ParseItemClassType(firstDataStatLine, baseItems, knownRegexes);

            //parse text for Links
            Links = ParseLinks(itemDataParts, knownRegexes);


            //assign affix parts
            if (ClassType == ItemClassType.Flask)
            {
                lastPartIndex--;
            }
            ItemAffixData = itemDataParts[lastPartIndex].Replace("\r\n", "`").Split('`');

            if (ClassType == ItemClassType.Belt || ClassType == ItemClassType.Amulet || ClassType == ItemClassType.Ring ||
                ClassType == ItemClassType.Quiver)
            {
                ItemImplicitData = itemDataParts[lastPartIndex - 1].Replace("\r\n", "`").Split('`');
            }
            else if ((ClassType == ItemClassType.BodyArmour || ClassType == ItemClassType.Helm ||
                      ClassType == ItemClassType.Shield || ClassType == ItemClassType.Gloves ||
                      ClassType == ItemClassType.Boots || IsWeapon) && (lastPartIndex == 6))
            {
                ItemImplicitData = itemDataParts[lastPartIndex - 1].Replace("\r\n", "`").Split('`');
            }


            if (ItemRarity == ItemRarityType.Unique)
            {
                ParseUniqueAffixData(knownUniques, knownRegexes);
                goto endofparse;
            }
            ParseAffixData(knownAffixes);


            //обязательно до парсинга имплисит мода
            SolveSpellDamageManaAffixes(affixBrackets);
            SolveItemRarityAffixes(affixBrackets);
            SolveComboPhysAccAffixes(affixBrackets);
            SolveArmourStunRecoveryAffixes(affixBrackets);
            //parse implicit mode
            ParseImplicitData(knownAffixes);

            //calculations
            DuCalcsMich();


            //Console.WriteLine($"FreePrefixes: {3-Prefixes} FreeSuffixes: {3-Suffixes} UnsolvedAffixes: {Affixes}");
            endofparse:
            return ParseResult.Success;
        }

        public void ParseUniqueAffixData(UniqueAffixSource knownUniques, IDictionary<ParseRegEx, Regex> knownRegexes)
        {
            var uniqueItem = knownUniques.GetUnique(Name, ClassType);

            UniqueDescription = uniqueItem.Desc;
            if (ItemImplicitData != null)
                foreach (var line in ItemImplicitData)
                {
                    if (line == "")
                        continue;
                    ParseUniqueAffix(line, uniqueItem.Affixes, knownRegexes, true);
                }
            if (ItemAffixData != null)
                foreach (var line in ItemAffixData)
                {
                    if (line == "")
                        continue;
                    ParseUniqueAffix(line, uniqueItem.Affixes, knownRegexes, false);
                }
        }


        public bool ParseUniqueAffix(string line, List<UniqueAffix> affixes, IDictionary<ParseRegEx, Regex> knownRegexes,
            bool isImplicit)
        {
            if (affixes == null)
                return false;
            foreach (var affix in affixes)
            {
                if (!isImplicit.Equals(affix.IsImplicit))
                    continue;
                foreach (var word in affix.WordsInLine)
                    if (!line.ToLower().Contains(word.ToLower()))
                        goto skipknownaffixline;

                Regex parseRegex;
                //    ParseRegEx.RegexUniqueValue, new Regex(@"(\+|^| )(?'value'[0-9\.,]+)(%| )", RegexOptions.Compiled)
                //    ParseRegEx.RegexUniqueValueDouble, new Regex(@"(\+|^| )(?'valueLo'[0-9\.,]+)-(?'valueHi'[0-9\.,]+)(%| )", RegexOptions.Compiled)
                string left, right;
                if (affix.IsDoubleAffix)
                {
                    knownRegexes.TryGetValue(ParseRegEx.RegexUniqueValueDouble, out parseRegex);
                    var match = parseRegex.Match(line);

                    //calc how good is roll
                    var loMath = affix.HighValue - affix.LoValue == 0
                        ? 1
                        : (match.Groups["valueLo"].Value.ToInt() - affix.LoValue)/(affix.HighValue - affix.LoValue);

                    var hiMath = (match.Groups["valueHi"].Value.ToInt() - affix.LowValueSecond)/
                                 (affix.HighValueSecond - affix.LowValueSecond);

                    var percent = Math.Round((loMath + hiMath)/2, 2)*100 + "%";

                    left = affix.RawLine;
                    right = match.Groups["valueLo"].Value + "-" + match.Groups["valueHi"].Value + " (" + affix.LoValue +
                            "-" + affix.HighValue + ")" + "(" +
                            affix.LowValueSecond + "-" + affix.HighValueSecond + ")  " + percent;
                }
                else
                {
                    knownRegexes.TryGetValue(ParseRegEx.RegexUniqueValue, out parseRegex);
                    var match = parseRegex.Match(line);

                    //calc how good is roll
                    var value = 0d;
                    double.TryParse(match.Groups["value"].Value, out value);

                    var percent =
                        Math.Round((value - affix.LoValue)/(affix.HighValue - affix.LoValue), 2)*100 + "%";
                    left = affix.RawLine;
                    right = match.Groups["value"].Value + "(" + affix.LoValue + "-" + affix.HighValue + ")  " + percent;
                }

                UniqueTextLeft = UniqueTextLeft + (UniqueTextLeft != "" ? "\n" : "") + left;
                UniqueTextRight = UniqueTextRight + (UniqueTextRight != "" ? "\n" : "") + right;
                UniqueText = UniqueText + "\n" + left.PadRight(30) + right.PadLeft(30);
                return true;

                skipknownaffixline:
                ;
            }
            return false;
        }

        public void ParseEffects(string[] itemDataParts, ref int lastPartIndex,
            IDictionary<ParseRegEx, Regex> knownRegexes)
        {
            if (itemDataParts[lastPartIndex].Contains("Note:"))
            {
                IsNote = true;
                Regex parseRegex;
                knownRegexes.TryGetValue(ParseRegEx.RegexNoteMessageLine, out parseRegex);
                var matchNote = parseRegex.Match(itemDataParts[lastPartIndex]);
                TradeNoteMessage = matchNote.Success ? matchNote.Groups["noteMessage"].Value : "";
                lastPartIndex--;
            }

            //check MTX effect

            if (itemDataParts[lastPartIndex].Contains("Has"))
            {
                IsHasMTXEffect = true;
                lastPartIndex--;
            }


            //check mirror effect
            if (itemDataParts[lastPartIndex].Contains("Mirrored"))
            {
                IsMirrored = true;
                lastPartIndex--;
            }

            //check corruption
            if (itemDataParts[lastPartIndex].Contains("Corrupted"))
            {
                IsCorrupted = true;
                lastPartIndex--;
            }

            //check unique description
            if (ItemRarity == ItemRarityType.Unique)
                lastPartIndex--;
        }

        public ItemRarityType ParseItemRarity(string itemDataPart, IDictionary<ParseRegEx, Regex> knownRegexes)
        {
            Regex parseRegex;
            knownRegexes.TryGetValue(ParseRegEx.RegExItemRarityLine, out parseRegex);
            var match = parseRegex.Match(itemDataPart);
            if (match.Success)
            {
                switch (match.Groups["rarity"].Value)
                {
                    case "Normal":
                        ItemRarity = ItemRarityType.Normal;
                        return ItemRarityType.Normal;
                    case "Magic":
                        ItemRarity = ItemRarityType.Magic;
                        return ItemRarityType.Magic;
                    case "Rare":
                        ItemRarity = ItemRarityType.Rare;
                        return ItemRarityType.Rare;
                    case "Unique":
                        ItemRarity = ItemRarityType.Unique;
                        return ItemRarityType.Unique;
                    default:
                        ItemRarity = ItemRarityType.Unknown;
                        return ItemRarityType.Unknown;
                }
            }
            Console.WriteLine($"[Item.ParseItemData] Wrong DataText. {itemDataPart}");
            return ItemRarityType.WrongItem;
        }

        private void ParseItemLevel(string[] itemDataText)
        {
            iLevel =
                (from line in itemDataText
                    where line.StartsWith("Item Level:")
                    select line.Replace("Item Level: ", "").ToInt()).FirstOrDefault();
        }

        private int ParseLinks(string[] itemDataText, IDictionary<ParseRegEx, Regex> knownRegexes)
        {
            if (!IsWeapon && !IsArmour) return 0;
            foreach (var line in itemDataText)
            {
                if (!line.StartsWith("Sockets:")) continue;
                Regex parseRegex;
                knownRegexes.TryGetValue(ParseRegEx.RegexSocket6, out parseRegex);
                if (parseRegex.Match(line).Success)
                    return 6;
                knownRegexes.TryGetValue(ParseRegEx.RegexSocket5, out parseRegex);
                if (parseRegex.Match(line).Success)
                    return 5;
                knownRegexes.TryGetValue(ParseRegEx.RegexSocket4, out parseRegex);
                if (parseRegex.Match(line).Success)
                    return 4;
                knownRegexes.TryGetValue(ParseRegEx.RegexSocket3, out parseRegex);
                if (parseRegex.Match(line).Success)
                    return 3;
                knownRegexes.TryGetValue(ParseRegEx.RegexSocket2, out parseRegex);
                if (parseRegex.Match(line).Success)
                    return 2;
            }
            return 0;
        }

        private bool ParseItemClassType(string firstDataStatLine, IDictionary<BaseItemTypes, BaseItemsSource> baseItems,
            IDictionary<ParseRegEx, Regex> knownRegexes)
        {
            Regex parseRegex;
            if (BaseType.Contains("Flask"))
            {
                ClassType = ItemClassType.Flask;
                return true;
            }
            if (!firstDataStatLine.Contains(":"))
            {
                knownRegexes.TryGetValue(ParseRegEx.Regex1HWeaponClassLine, out parseRegex);
                var match = parseRegex.Match(firstDataStatLine);

                if (match.Success)
                {
                    Enum.TryParse(match.Groups["weaponClass"].Value.Replace(" ", ""), out ClassType);
                    baseItems[BaseItemTypes.Weapon].SetWeaponsBaseProperties(BaseType, out BaseDamageLo,
                        out BaseDamageHi, out BaseCC, out BaseAPS);
                    GripType = "1h";
                    IsWeapon = true;
                    if ((match.Groups["weaponClass"].Value == "Sekhem") ||
                        (match.Groups["weaponClass"].Value == "Fetish") ||
                        (match.Groups["weaponClass"].Value == "Sceptre"))
                    {
                        ClassType = ItemClassType.Sceptre;
                        IsSceptre = true;
                    }
                    return true;
                }


                knownRegexes.TryGetValue(ParseRegEx.Regex2HWeaponClassLine, out parseRegex);
                match = parseRegex.Match(firstDataStatLine);

                if (match.Success)
                {
                    Enum.TryParse(match.Groups["weaponClass"].Value.Replace(" ", ""), out ClassType);
                    baseItems[BaseItemTypes.Weapon].SetWeaponsBaseProperties(BaseType, out BaseDamageLo,
                        out BaseDamageHi, out BaseCC, out BaseAPS);
                    GripType = "2h";
                    IsWeapon = true;
                    return true;
                }
                Console.WriteLine(
                    $"[Item.ParseItemClassType] Unknown WeaponType - BaseTypeName : {BaseType}  FirstStatLine: {firstDataStatLine}");
                return false;
            }

            if (firstDataStatLine.Contains("Map"))
            {
                ClassType = ItemClassType.Map;
                return true;
            }
            if (BaseType.Contains("Quiver"))
            {
                ClassType = ItemClassType.Quiver;
                return true;
            }
            if (BaseType.Contains("Jewel"))
            {
                ClassType = ItemClassType.Jewel;
                return true;
            }
            if (BaseType.Contains("Amulet"))
            {
                ClassType = ItemClassType.Amulet;
                return true;
            }
            if (BaseType.Contains("Talisman"))
            {
                ClassType = ItemClassType.Talisman;
                return true;
            }

            knownRegexes.TryGetValue(ParseRegEx.RegexRingClassLine, out parseRegex);

            if (parseRegex.Match(BaseType).Success)
            {
                ClassType = ItemClassType.Ring;
                return true;
            }

            knownRegexes.TryGetValue(ParseRegEx.RegexBeltClassLine, out parseRegex);

            if (parseRegex.Match(BaseType).Success)
            {
                ClassType = ItemClassType.Belt;
                return true;
            }

            knownRegexes.TryGetValue(ParseRegEx.RegexShieldClassLine, out parseRegex);


            if (parseRegex.Match(BaseType).Success)
            {
                ClassType = ItemClassType.Shield;
                IsArmour = true;
                IsSpiritShield = true;
                baseItems[BaseItemTypes.Shields].SetArmourBaseProperties(BaseType, out BaseAR, out BaseEV, out BaseES);
                return true;
            }

            knownRegexes.TryGetValue(ParseRegEx.RegexBootsClassLine, out parseRegex);

            if (parseRegex.Match(BaseType).Success)
            {
                ClassType = ItemClassType.Boots;
                IsArmour = true;
                baseItems[BaseItemTypes.Boots].SetArmourBaseProperties(BaseType, out BaseAR, out BaseEV, out BaseES);
                return true;
            }

            knownRegexes.TryGetValue(ParseRegEx.RegexHelmClassLine, out parseRegex);

            if (parseRegex.Match(BaseType).Success)
            {
                baseItems[BaseItemTypes.Helmets].SetArmourBaseProperties(BaseType, out BaseAR, out BaseEV, out BaseES);
                ClassType = ItemClassType.Helm;
                IsArmour = true;
                return true;
            }
            knownRegexes.TryGetValue(ParseRegEx.RegexGlovesClassLine, out parseRegex);

            if (parseRegex.Match(BaseType).Success)
            {
                ClassType = ItemClassType.Gloves;
                IsArmour = true;
                baseItems[BaseItemTypes.Gloves].SetArmourBaseProperties(BaseType, out BaseAR, out BaseEV, out BaseES);
                return true;
            }

            if (baseItems[BaseItemTypes.BodyArmour].SetArmourBaseProperties(BaseType, out BaseAR, out BaseEV, out BaseES))
            {
                ClassType = ItemClassType.BodyArmour;
                IsArmour = true;
                return true;
            }

            Console.WriteLine(
                $"[Item.ParseItemClassType] Unknown ItemType  BaseTypeName : {BaseType}  DataBlock-FirstLine: {firstDataStatLine}");
            return false;
        }


        private bool CalcCraftPhysDPS()
        {
            if (!IsWeapon) return false;

            CraftPDPSLo = 0;
            CraftPDPS = PDPS;
            CraftPAPSLo = 0;
            CraftPAPS = APS;
            CraftPCritLo = 0;
            CraftPCrit = Crit;
            CraftPCritDamageLo = 0;
            CraftPCritDamage = CritDamage;
            CraftTtPDPS = "";
            CraftTtPDPSPrice = "";

            if ((Prefixes > 2) && (Suffixes > 2)) return false;


            var tCraftPhysDamageLo = 0;
            var tCraftPhysDamageHi = 0;
            var tCraftFlatPhysDamageLo = 0;
            var tCraftFlatPhysDamageHi = 0;
            var tCraftIasLo = 0;
            var tCraftIasHi = 0;
            var tCraftCritLo = 0;
            var tCraftCritHi = 0;
            var tCraftCritDamageLo = 0;
            var tCraftCritDamageHi = 0;
            var tSuffixes = Suffixes;
            var tTTcraft = "";
            var tTTcraftPrice = "";

            if (!IsFlatPhys && (Prefixes < 3))
            {
                switch (GripType)
                {
                    case "1h":
                        tCraftFlatPhysDamageLo = 27/2;
                        tCraftFlatPhysDamageHi = 33/2;
                        break;
                    default:
                        tCraftFlatPhysDamageLo = 40/2;
                        tCraftFlatPhysDamageHi = 49/2;
                        break;
                }
                tTTcraft = "[FlatPhys]";
                tTTcraftPrice = "[2chaos]";
                goto skipcrafts;
            }

            if (((IsLocalPhysAff == AffixSolution.Negative) || (IsLocalPhysAff == AffixSolution.Uncertain)) &&
                (Prefixes < 3))
            {
                tCraftPhysDamageLo = 60;
                tCraftPhysDamageHi = 79;
                tTTcraft = "[Phys]";
                tTTcraftPrice = "[2chaos]";
            }


            if (!IsIAS && (Suffixes < 3))
            {
                switch (ClassType)
                {
                    case ItemClassType.Bow:
                        tCraftIasLo = 7;
                        tCraftIasHi = 12;
                        break;
                    default:
                        tCraftIasLo = 12;
                        tCraftIasHi = 15;
                        break;
                }
                tTTcraft = "[IAS]";
                tTTcraftPrice = "[4chaos]";
                goto skipcrafts;
            }

            if (!IsAffixCrit && (Suffixes < 3))
            {
                tCraftCritLo = 22;
                tCraftCritHi = 27;
                tTTcraft = "[CritChance]";
                tTTcraftPrice = "[4alch]";
                goto skipcrafts;
            }

            if (!IsAffixCritDamage && (Suffixes < 3))
            {
                tCraftCritDamageLo = 22;
                tCraftCritDamageHi = 27;
                tTTcraft = "[AffixCritDamage]";
                tTTcraftPrice = "[4alch]";
            }

            skipcrafts:

            CraftPAPSLo = Math.Round(BaseAPS*(100 + tCraftIasLo)/100, 2);
            CraftPAPS = Math.Round(BaseAPS*(100 + tCraftIasHi)/100, 2);
            CraftPDPSLo =
                Math.Round((double) (BaseDamageLo/2 + BaseDamageHi/2 + FlatPhys + tCraftFlatPhysDamageLo)*
                           (120 + LocalPhys + tCraftPhysDamageLo)/100*CraftPAPSLo, 1);
            CraftPDPS =
                Math.Round((double) (BaseDamageLo/2 + BaseDamageHi/2 + FlatPhys + tCraftFlatPhysDamageHi)*
                           (120 + LocalPhys + tCraftPhysDamageHi)/100*CraftPAPS, 1);

            CraftPCritLo = Math.Round(BaseCC*(100 + ImplicitCrit + tCraftCritLo)/100, 1);
            CraftPCrit = Math.Round(BaseCC*(100 + ImplicitCrit + tCraftCritHi)/100, 1);
            CraftPCritDamageLo = AffixCritDamage + ImplicitCritDamage + tCraftCritDamageLo;
            CraftPCritDamage = AffixCritDamage + ImplicitCritDamage + tCraftCritDamageHi;
            CraftTtPDPS = tTTcraft;
            CraftTtPDPSPrice = tTTcraftPrice;

            return true;
        }


        private bool CalcMultiPhysDPS()
        {
            if (!IsWeapon) return false;

            MultiPAPSLo = CraftPAPSLo;
            MultiPAPS = CraftPAPS;
            MultiPDPSLo = CraftPDPSLo;
            MultiPDPS = CraftPDPS;
            MultiPCritLo = CraftPCritLo;
            MultiPCrit = CraftPCrit;
            MultiPCritDamageLo = CraftPCritDamageLo;
            MultiPCritDamage = CraftPCritDamage;
            MultiTtPDPS = CraftTtPDPS;
            MultiTtPDPSPrice = CraftTtPDPSPrice;

            if (Suffixes > 2) return false;


            var tMultiPhysDamageLo = 0;
            var tMultiPhysDamageHi = 0;
            var tMultiFlatPhysDamageLo = 0;
            var tMultiFlatPhysDamageHi = 0;
            var tMultiIasLo = 0;
            var tMultiIasHi = 0;
            var tMultiCritLo = 0;
            var tMultiCritHi = 0;
            var tMultiCritDamageHi = 0;
            var tMultiCritDamageLo = 0;
            var tPrefixes = Prefixes;
            var tSuffixes = Suffixes;
            var tTTmulti = "";
            var tTTmultiPrice = "";


            if ((IAS < 10) && (AffixCritDamage < 20) && (AffixCrit < 20))
            {
                tTTmulti += "[ClearSuffixes]";
                tTTmultiPrice += "[2exa][Scouring]";
                tSuffixes = 0;
            }

            tTTmulti += "[MultiMod]";
            tTTmultiPrice += "[2exa]";
            tSuffixes++;
            if (!IsFlatPhys && (tPrefixes < 3))
            {
                switch (GripType)
                {
                    case "1h":
                        tMultiFlatPhysDamageLo = 27/2;
                        tMultiFlatPhysDamageHi = 33/2;
                        break;
                    default:
                        tMultiFlatPhysDamageLo = 40/2;
                        tMultiFlatPhysDamageHi = 49/2;
                        break;
                }
                tTTmultiPrice += "[2chaos]";
                tTTmulti += "[FlatPhys]";
                tPrefixes++;
            }

            if ((IsLocalPhysAff != AffixSolution.Positive) && (tPrefixes < 3))
            {
                tMultiPhysDamageLo = 60;
                tMultiPhysDamageHi = 79;
                tTTmulti += "[Phys]";
                tTTmultiPrice += "[2chaos]";
            }

            if (!IsIAS && (tSuffixes < 3))
            {
                switch (ClassType)
                {
                    case ItemClassType.Bow:
                        tMultiIasLo = 7;
                        tMultiIasHi = 12;
                        break;
                    default:
                        tMultiIasLo = 12;
                        tMultiIasHi = 15;
                        break;
                }
                tSuffixes++;
                tTTmulti += "[IAS]";
                tTTmultiPrice += "[4chaos]";
            }

            if (!IsAffixCrit && (tSuffixes < 3))
            {
                tMultiCritLo = 22;
                tMultiCritHi = 27;
                tTTmulti += "[CritChance]";
                tTTmultiPrice += "[4alch]";
                tSuffixes++;
            }

            if (!IsAffixCritDamage && (tSuffixes < 3))
            {
                tMultiCritDamageLo = 22;
                tMultiCritDamageHi = 27;
                tTTmulti += "[CritMultiplier]";
                tTTmultiPrice += "[4alch]";
            }

            if (tTTmulti == "[MultiMod]")
                return true;
            MultiPAPSLo = Math.Round(BaseAPS*(100 + tMultiIasLo)/100, 2);
            MultiPAPS = Math.Round(BaseAPS*(100 + tMultiIasHi)/100, 2);
            MultiPDPSLo =
                Math.Round((double) (BaseDamageLo/2 + BaseDamageHi/2 + FlatPhys + tMultiFlatPhysDamageLo)*
                           (120 + LocalPhys + tMultiPhysDamageLo)/100*MultiPAPSLo, 1);
            MultiPDPS =
                Math.Round((double) (BaseDamageLo/2 + BaseDamageHi/2 + FlatPhys + tMultiFlatPhysDamageHi)*
                           (120 + LocalPhys + tMultiPhysDamageHi)/100*MultiPAPS, 1);


            MultiPCritLo = Math.Round(BaseCC*(100 + ImplicitCrit + tMultiCritLo)/100, 1);
            MultiPCrit = Math.Round(BaseCC*(100 + ImplicitCrit + tMultiCritHi)/100, 1);
            MultiPCritDamageLo = AffixCritDamage + ImplicitCritDamage + tMultiCritDamageLo;
            MultiPCritDamage = AffixCritDamage + ImplicitCritDamage + tMultiCritDamageHi;
            MultiTtPDPS = tTTmulti;
            MultiTtPDPSPrice = tTTmultiPrice;
            return true;
        }


        private bool CalcCraftElemDPS()
        {
            if (!IsWeapon) return false;

            CraftEDPSLo = 0;
            CraftEDPS = EDPS;
            CraftEAPSLo = 0;
            CraftEAPS = APS;
            CraftECritLo = 0;
            CraftECrit = Crit;
            CraftECritDamageLo = 0;
            CraftECritDamage = CritDamage;
            CraftTtEDPS = "";
            CraftTtEDPSPrice = "";


            if ((Prefixes > 2) && (Suffixes > 2)) return false;

            var tCraftFlatElemDamageLo = 0d;
            var tCraftFlatElemDamageHi = 0d;
            var tCraftIasLo = 0;
            var tCraftIasHi = 0;
            var tCraftCritLo = 0;
            var tCraftCritHi = 0;
            var tCraftCritDamageLo = 0;
            var tCraftCritDamageHi = 0;
            var tSuffixes = Suffixes;
            var tTTcraft = "";
            var tTTcraftPrice = "";

            if (!IsFlatLightning && (Prefixes < 3))
            {
                switch (GripType)
                {
                    case "1h":
                        tCraftFlatElemDamageLo += 25.5d;
                        tCraftFlatElemDamageHi += 28d;
                        break;
                    default:
                        switch (ClassType)
                        {
                            case ItemClassType.Bow:
                                tCraftFlatElemDamageLo += 25.5d;
                                tCraftFlatElemDamageHi += 28d;
                                break;
                            default:
                                tCraftFlatElemDamageLo += 36.5d;
                                tCraftFlatElemDamageHi += 42.5d;
                                break;
                        }
                        break;
                }

                tTTcraft = "[FlatLightning]";
                tTTcraftPrice = "[2chaos]";
                goto skipelemcrafts;
            }

            if (!IsFlatFire && (Prefixes < 3))
            {
                switch (GripType)
                {
                    case "1h":
                        tCraftFlatElemDamageLo += 22d;
                        tCraftFlatElemDamageHi += 26.5d;
                        break;
                    default:
                        switch (ClassType)
                        {
                            case ItemClassType.Bow:
                                tCraftFlatElemDamageLo += 22d;
                                tCraftFlatElemDamageHi += 26.5d;
                                break;
                            default:
                                tCraftFlatElemDamageLo += 32.5d;
                                tCraftFlatElemDamageHi += 40d;
                                break;
                        }
                        break;
                }

                tTTcraft = "[FlatFire]";
                tTTcraftPrice = "[2chaos]";
                goto skipelemcrafts;
            }

            if (!IsFlatCold && (Prefixes < 3))
            {
                switch (GripType)
                {
                    case "1h":
                        tCraftFlatElemDamageLo += 17.5d;
                        tCraftFlatElemDamageHi += 22d;
                        break;
                    default:
                        switch (ClassType)
                        {
                            case ItemClassType.Bow:
                                tCraftFlatElemDamageLo += 17.5d;
                                tCraftFlatElemDamageHi += 22d;
                                break;
                            default:
                                tCraftFlatElemDamageLo += 26.5d;
                                tCraftFlatElemDamageHi += 32.5d;
                                break;
                        }
                        break;
                }

                tTTcraft = "[FlatCold]";
                tTTcraftPrice = "[2chaos]";
                goto skipelemcrafts;
            }


            if (!IsIAS && (Suffixes < 3))
            {
                switch (ClassType)
                {
                    case ItemClassType.Bow:
                        tCraftIasLo = 7;
                        tCraftIasHi = 12;
                        break;
                    default:
                        tCraftIasLo = 12;
                        tCraftIasHi = 15;
                        break;
                }
                tTTcraft = "[IAS]";
                tTTcraftPrice = "[4chaos]";
                goto skipelemcrafts;
            }

            if (!IsAffixCrit && (Suffixes < 3))
            {
                tCraftCritLo = 22;
                tCraftCritHi = 27;
                tTTcraft = "[CritChance]";
                tTTcraftPrice = "[4alch]";
                goto skipelemcrafts;
            }

            if (!IsAffixCritDamage && (Suffixes < 3))
            {
                tCraftCritDamageLo = 22;
                tCraftCritDamageHi = 27;
                tTTcraft = "[AffixCritDamage]";
                tTTcraftPrice = "[4alch]";
            }

            skipelemcrafts:

            CraftEAPSLo = Math.Round(BaseAPS*(100 + IAS + tCraftIasLo)/100, 2);
            CraftEAPS = Math.Round(BaseAPS*(100 + IAS + tCraftIasHi)/100, 2);
            CraftEDPSLo = Math.Round((FlatElem + tCraftFlatElemDamageLo)*CraftEAPSLo, 1);
            CraftEDPS = Math.Round((FlatElem + tCraftFlatElemDamageHi)*CraftEAPS, 1);
            CraftECritLo = Math.Round(BaseCC*(100 + ImplicitCrit + tCraftCritLo)/100, 1);
            CraftECrit = Math.Round(BaseCC*(100 + ImplicitCrit + tCraftCritHi)/100, 1);
            CraftECritDamageLo = AffixCritDamage + tCraftCritDamageLo;
            CraftECritDamage = AffixCritDamage + tCraftCritDamageHi;

            CraftTtEDPS = tTTcraft;
            CraftTtEDPSPrice = tTTcraftPrice;

            return true;
        }

        private bool CalcMultiElemDPS()
        {
            if (!IsWeapon) return false;

            MultiEDPSLo = CraftEDPSLo;
            MultiEDPS = CraftEDPS;
            MultiEAPSLo = CraftEAPSLo;
            MultiEAPS = CraftEAPS;
            MultiECritLo = CraftECritLo;
            MultiECrit = CraftECrit;
            MultiECritDamageLo = CraftECritDamageLo;
            MultiECritDamage = CraftECritDamage;
            MultiTtEDPS = CraftTtEDPS;
            MultiTtEDPSPrice = CraftTtEDPSPrice;

            if (Suffixes > 2) return false;


            var tMultiFlatElemDamageLo = 0d;
            var tMultiFlatElemDamageHi = 0d;
            var tMultiIasLo = 0;
            var tMultiIasHi = 0;
            var tMultiCritLo = 0;
            var tMultiCritHi = 0;
            var tMultiCritDamageLo = 0;
            var tMultiCritDamageHi = 0;
            var tPrefixes = Prefixes;
            var tSuffixes = Suffixes;
            var tTTMulti = "";
            var tTTMultiPrice = "";


            if ((IAS < 10) && (AffixCritDamage < 20) && (AffixCrit < 20))
            {
                tTTMulti += "[ClearSuffixes]";
                tTTMultiPrice += "[2exa][Scouring]";
                tSuffixes = 0;
            }

            tTTMultiPrice += "[2exa]";
            tTTMulti += "[MultiMod]";
            tSuffixes++;

            if (!IsFlatLightning && (tPrefixes < 3))
            {
                switch (GripType)
                {
                    case "1h":
                        tMultiFlatElemDamageLo += 25.5d;
                        tMultiFlatElemDamageHi += 28d;
                        break;
                    default:
                        switch (ClassType)
                        {
                            case ItemClassType.Bow:
                                tMultiFlatElemDamageLo += 25.5d;
                                tMultiFlatElemDamageHi += 28d;
                                break;
                            default:
                                tMultiFlatElemDamageLo += 36.5d;
                                tMultiFlatElemDamageHi += 42.5d;
                                break;
                        }
                        break;
                }

                tTTMulti += "[FlatLightning]";
                tTTMultiPrice += "[2chaos]";
                tPrefixes++;
            }

            if (!IsFlatFire && (tPrefixes < 3))
            {
                switch (GripType)
                {
                    case "1h":
                        tMultiFlatElemDamageLo += 22d;
                        tMultiFlatElemDamageHi += 26.5d;
                        break;
                    default:
                        switch (ClassType)
                        {
                            case ItemClassType.Bow:
                                tMultiFlatElemDamageLo += 22d;
                                tMultiFlatElemDamageHi += 26.5d;
                                break;
                            default:
                                tMultiFlatElemDamageLo += 32.5d;
                                tMultiFlatElemDamageHi += 40d;
                                break;
                        }
                        break;
                }

                tTTMulti += "[FlatFire]";
                tTTMultiPrice += "[2chaos]";
                tPrefixes++;
            }

            if (!IsFlatCold && (tPrefixes < 3))
            {
                switch (GripType)
                {
                    case "1h":
                        tMultiFlatElemDamageLo += 17.5d;
                        tMultiFlatElemDamageHi += 22d;
                        break;
                    default:
                        switch (ClassType)
                        {
                            case ItemClassType.Bow:
                                tMultiFlatElemDamageLo += 17.5d;
                                tMultiFlatElemDamageHi += 22d;
                                break;
                            default:
                                tMultiFlatElemDamageLo += 26.5d;
                                tMultiFlatElemDamageHi += 32.5d;
                                break;
                        }
                        break;
                }

                tTTMulti += "[FlatCold]";
                tTTMultiPrice += "[2chaos]";
            }


            if (!IsIAS && (tSuffixes < 3))
            {
                switch (ClassType)
                {
                    case ItemClassType.Bow:
                        tMultiIasLo = 7;
                        tMultiIasHi = 12;
                        break;
                    default:
                        tMultiIasLo = 12;
                        tMultiIasHi = 15;
                        break;
                }
                tSuffixes++;
                tTTMulti += "[IAS]";
                tTTMultiPrice += "[4chaos]";
                tSuffixes++;
            }

            if (!IsAffixCrit && (tSuffixes < 3))
            {
                tMultiCritLo = 22;
                tMultiCritHi = 27;
                tSuffixes++;
                tTTMulti += "[CritChance]";
                tTTMultiPrice += "[4alch]";
                tSuffixes++;
            }

            if (!IsAffixCritDamage && (tSuffixes < 3))
            {
                tMultiCritDamageLo = 22;
                tMultiCritDamageHi = 27;
                tTTMulti += "[AffixCritDamage]";
                tTTMultiPrice += "[4alch]";
            }

            if (tTTMulti == "[MultiMod]")
                return true;
            MultiEAPSLo = Math.Round(BaseAPS*(100 + IAS + tMultiIasLo)/100, 2);
            MultiEAPS = Math.Round(BaseAPS*(100 + IAS + tMultiIasHi)/100, 2);
            MultiEDPSLo = Math.Round((FlatElem + tMultiFlatElemDamageLo)*MultiEAPSLo, 1);
            MultiEDPS = Math.Round((FlatElem + tMultiFlatElemDamageHi)*MultiEAPS, 1);
            MultiECritLo = Math.Round(BaseCC*(100 + ImplicitCrit + tMultiCritLo)/100, 1);
            MultiECrit = Math.Round(BaseCC*(100 + ImplicitCrit + tMultiCritHi)/100, 1);
            MultiECritDamageLo = AffixCritDamage + tMultiCritDamageLo;
            MultiECritDamage = AffixCritDamage + tMultiCritDamageHi;

            MultiTtEDPS = tTTMulti;
            MultiTtEDPSPrice = tTTMultiPrice;

            return true;
        }

        private bool CalcCraftSpellDamage()
        {
            if ((ClassType != ItemClassType.Wand) && (ClassType != ItemClassType.Staff) && !IsSceptre &&
                (ClassType != ItemClassType.Dagger) && !IsSpiritShield) return false;

            CraftSPDLo = 0;
            CraftSPD = SPD;
            CraftLocalElemDamageLo = 0;
            CraftLocalElemDamage = LocalElemDamage;
            CraftTotalSPDLo = 0;
            CraftTotalSPD = TotalSPD;
            CraftSpellCritDamageLo = 0;
            CraftSpellCritDamage = SpellCritDamage;
            CraftSpellCritLo = 0;
            CraftSpellCrit = AffixSpellCrit;
            CraftFlatSPDLo = 0;
            CraftFlatSPD = FlatSPD;
            CraftTtSPD = "";
            CraftTtSPDPrice = "";
            CraftCastSpeed = CastSpeed;
            CraftCastSpeedLo = 0;

            if ((Prefixes > 2) && (Suffixes > 2)) return false;

            var tLocalElemDamage = LocalFireDamage >= LocalColdDamage
                ? (LocalFireDamage >= LocalLightDamage ? LocalFireDamage : LocalLightDamage)
                : (LocalColdDamage >= LocalLightDamage ? LocalColdDamage : LocalLightDamage);

            var tCraftSpellDamageLo = 0;
            var tCraftSpellDamage = 0;
            var tCraftLocalElemDamageLo = 0;
            var tCraftLocalElemDamage = 0;
            var tCraftFlatSPDLo = 0;
            var tCraftFlatSPD = 0;
            var tCraftSpellCritLo = 0;
            var tCraftSpellCrit = 0;
            var tCraftCritDamageLo = 0;
            var tCraftCritDamage = 0;
            var tTTcraft = "";
            var tTTcraftPrice = "";
            var tCraftCastSpeedLo = 0;
            var tCraftCastSpeed = 0;


            if ((IsSpellDamageAff != AffixSolution.Positive) && (Prefixes < 3))
            {
                switch (GripType)
                {
                    case "1h":
                        tCraftSpellDamageLo = 35;
                        tCraftSpellDamage = 44;
                        break;
                    default:
                        tCraftSpellDamageLo = 53;
                        tCraftSpellDamage = 68;
                        break;
                }
                tTTcraft = "[Spell]";
                tTTcraftPrice = "[4Chaos]";
                goto skipspellcraft;
            }

            if (!IsFlatColdSPD && !IsFlatLightningSPD && !IsFlatFireSPD && (Prefixes < 3))
            {
                switch (GripType)
                {
                    case "1h":
                        tCraftFlatSPDLo = 20;
                        tCraftFlatSPD = 23;
                        break;
                    default:
                        tCraftFlatSPDLo = 30;
                        tCraftFlatSPD = 34;
                        break;
                }
                tTTcraft = "[FlatSPD]";
                tTTcraftPrice = "[3chaos]";
                goto skipspellcraft;
            }

            if (!IsCastSpeed && (Suffixes < 3) && (ClassType != ItemClassType.Dagger))
            {
                tCraftCastSpeedLo = 9;
                tCraftCastSpeed = 11;
                tTTcraft = "[CastSpeed]";
                tTTcraftPrice = "[Regal]";
                goto skipspellcraft;
            }

            if (!IsAffixSpellCrit && (Suffixes < 3))
            {
                tCraftSpellCritLo = 50;
                tCraftSpellCrit = 69;
                tTTcraft = "[AffixSpellCrit]";
                tTTcraftPrice = "[2chaos]";
                goto skipspellcraft;
            }

            if ((tLocalElemDamage < 19) && (Suffixes < 3))
            {
                tCraftLocalElemDamageLo = 15;
                tCraftLocalElemDamage = 19;
                tTTcraft = "[LocalElem]";
                tTTcraftPrice = "[10augmentation]";
                goto skipspellcraft;
            }

            if (!IsAffixCritDamage && (Suffixes < 3))
            {
                tCraftCritDamageLo = 21;
                tCraftCritDamage = 27;
                tTTcraft = "[AffixCritDamage]";
                tTTcraftPrice = "[4alchemy]";
            }

            skipspellcraft:

            CraftSPDLo = SPD + tCraftSpellDamageLo;
            CraftSPD = SPD + tCraftSpellDamage;
            CraftLocalElemDamageLo = tCraftLocalElemDamageLo + ImplicitLocalElemDamage;
            CraftLocalElemDamage = tCraftLocalElemDamage + ImplicitLocalElemDamage;
            CraftTotalSPDLo = SPD + LocalElemDamage + tCraftSpellDamageLo + tCraftLocalElemDamageLo;
            CraftTotalSPD = SPD + LocalElemDamage + tCraftSpellDamage + tCraftLocalElemDamage;
            CraftSpellCritDamageLo = CritDamage + tCraftCritDamageLo;
            CraftSpellCritDamage = CritDamage + tCraftCritDamage;
            CraftSpellCritLo = AffixSpellCrit + tCraftSpellCritLo;
            CraftSpellCrit = AffixSpellCrit + tCraftSpellCrit;
            CraftFlatSPDLo = tCraftFlatSPDLo;
            CraftFlatSPD = tCraftFlatSPD;
            CraftTtSPD = tTTcraft;
            CraftTtSPDPrice = tTTcraftPrice;
            CraftCastSpeedLo = tCraftCastSpeedLo + ImplicitCastSpeed;
            CraftCastSpeed = tCraftCastSpeed + ImplicitCastSpeed;

            return true;
        }

        private bool CalcMultiSpellDamage()
        {
            if ((ClassType != ItemClassType.Wand) && (ClassType != ItemClassType.Staff) && !IsSceptre &&
                (ClassType != ItemClassType.Dagger) && !IsSpiritShield) return false;

            MultiSPDLo = CraftSPDLo;
            MultiSPD = CraftSPD;
            MultiLocalElemDamageLo = CraftLocalElemDamageLo;
            MultiLocalElemDamage = CraftLocalElemDamage;
            MultiTotalSPDLo = CraftTotalSPDLo;
            MultiTotalSPD = CraftTotalSPD;
            MultiSpellCritDamageLo = CraftSpellCritDamageLo;
            MultiSpellCritDamage = CraftSpellCritDamage;
            MultiSpellCritLo = CraftSpellCritLo;
            MultiSpellCrit = CraftSpellCrit;
            MultiFlatSPDLo = CraftFlatSPDLo;
            MultiFlatSPD = CraftFlatSPD;
            MultiTtSPD = CraftTtSPD;
            MultiTtSPDPrice = CraftTtSPDPrice;

            if (Suffixes > 2) return false;


            var tLocalElemDamage = LocalFireDamage >= LocalColdDamage
                ? (LocalFireDamage >= LocalLightDamage ? LocalFireDamage : LocalLightDamage)
                : (LocalColdDamage >= LocalLightDamage ? LocalColdDamage : LocalLightDamage);

            var tMultiSpellDamageLo = 0;
            var tMultiSpellDamage = 0;
            var tMultiLocalElemDamageLo = 0;
            var tMultiLocalElemDamage = 0;
            var tMultiFlatSPDLo = 0;
            var tMultiFlatSPD = 0;
            var tMultiSpellCritLo = 0;
            var tMultiSpellCrit = 0;
            var tMultiCritDamageLo = 0;
            var tMultiCritDamage = 0;
            var tTTMulti = "";
            var tTTMultiPrice = "";
            var tSuffixes = Suffixes;
            var tPrefixes = Prefixes;


            if ((!IsAffixSpellCrit || (IsAffixSpellCrit && AffixSpellCrit < 50)) && (tLocalElemDamage < 19) &&
                !IsCastSpeed && (!IsAffixCritDamage || (AffixCritDamage < 20)))
            {
                tTTMulti += "[ClearSuffixes]";
                tTTMultiPrice += "[2exa][Scouring]";
                tSuffixes = 0;
            }

            tTTMultiPrice += "[2exa]";
            tTTMulti += "[MultiMod]";
            tSuffixes++;

            if ((IsSpellDamageAff != AffixSolution.Positive) && (tPrefixes < 3))
            {
                switch (GripType)
                {
                    case "1h":
                        tMultiSpellDamageLo = 35;
                        tMultiSpellDamage = 44;
                        break;
                    default:
                        tMultiSpellDamageLo = 53;
                        tMultiSpellDamage = 68;
                        break;
                }
                tTTMulti += "[Spell]";
                tTTMultiPrice += "[4Chaos]";
                tPrefixes++;
            }

            if (!IsFlatColdSPD && !IsFlatLightningSPD && !IsFlatFireSPD && (tPrefixes < 3))
            {
                switch (GripType)
                {
                    case "1h":
                        tMultiFlatSPDLo = 20;
                        tMultiFlatSPD = 23;
                        break;
                    default:
                        tMultiFlatSPDLo = 30;
                        tMultiFlatSPD = 34;
                        break;
                }
                tTTMulti += "[FlatSPD]";
                tTTMultiPrice += "[3chaos]";
            }

            if (!IsCastSpeed && (tSuffixes < 3) && (ClassType != ItemClassType.Dagger))
            {
                MultiCastSpeedLo = 9;
                MultiCastSpeed = 11;
                MultiTtSPD += "[CastSpeed]";
                MultiTtSPDPrice += "[Regal]";
                tSuffixes++;
            }

            if (!IsAffixSpellCrit && (tSuffixes < 3))
            {
                tMultiSpellCritLo = 50;
                tMultiSpellCrit = 69;
                MultiTtSPD += "[AffixSpellCrit]";
                MultiTtSPDPrice += "[2chaos]";
                tSuffixes++;
            }

            if ((tLocalElemDamage < 19) && (tSuffixes < 3))
            {
                tMultiLocalElemDamageLo = 15;
                tMultiLocalElemDamage = 19;
                MultiTtSPD += "[LocalElem]";
                MultiTtSPDPrice += "[10augmentation]";
                tSuffixes++;
            }

            if (!IsAffixCritDamage && (tSuffixes < 3))
            {
                tMultiCritDamageLo = 21;
                tMultiCritDamage = 27;
                MultiTtSPD += "[AffixCritDamage]";
                MultiTtSPDPrice += "[4alchemy]";
            }
            if (tTTMulti == "[MultiMod]")
                return true;

            MultiSPDLo = SPD + tMultiSpellDamageLo;
            MultiSPD = SPD + tMultiSpellDamage;
            MultiLocalElemDamageLo = tMultiLocalElemDamageLo + ImplicitLocalElemDamage;
            MultiLocalElemDamage = tMultiLocalElemDamage + ImplicitLocalElemDamage;
            MultiTotalSPDLo = SPD + ImplicitSPD + LocalElemDamage + tMultiSpellDamageLo + tMultiLocalElemDamageLo;
            MultiTotalSPD = SPD + ImplicitSPD + LocalElemDamage + tMultiSpellDamage + tMultiLocalElemDamage;
            MultiSpellCritDamageLo = ImplicitCritDamage + tMultiCritDamageLo;
            MultiSpellCritDamage = ImplicitCritDamage + tMultiCritDamage;
            MultiSpellCritLo = AffixSpellCrit + ImplicitCrit + tMultiSpellCritLo;
            MultiSpellCrit = AffixSpellCrit + ImplicitCrit + tMultiSpellCrit;
            MultiFlatSPDLo = tMultiFlatSPDLo;
            MultiFlatSPD = tMultiFlatSPD;
            MultiTtSPD = tTTMulti;
            MultiTtSPDPrice = tTTMultiPrice;

            return true;
        }

        private bool CalcCraftCOC()
        {
            if (!IsSceptre && (ClassType != ItemClassType.Dagger)) return false;

            CraftCOCSPDLo = 0;
            CraftCOCSPD = SPD;
            CraftCOCLocalElemDamageLo = 0;
            CraftCOCLocalElemDamage = LocalElemDamage;
            CraftCOCTotalSPDLo = 0;
            CraftCOCTotalSPD = TotalSPD;
            CraftCOCSpellCritDamageLo = 0;
            CraftCOCSpellCritDamage = CritDamage;
            CraftCOCSpellCritLo = 0;
            CraftCOCSpellCrit = SpellCrit;
            CraftCOCFlatSPDLo = 0;
            CraftCOCFlatSPD = FlatSPD;
            CraftCOCCritLo = 0;
            CraftCOCCrit = Crit;
            CraftCOCAPSLo = 0;
            CraftCOCAPS = APS;
            CraftTtCOC = "";
            CraftTtCOCPrice = "";

            if ((Prefixes > 2) && (Suffixes > 2)) return false;

            var tLocalElemDamage = LocalFireDamage >= LocalColdDamage
                ? (LocalFireDamage >= LocalLightDamage ? LocalFireDamage : LocalLightDamage)
                : (LocalColdDamage >= LocalLightDamage ? LocalColdDamage : LocalLightDamage);

            var tCraftSpellDamageLo = 0;
            var tCraftSpellDamage = 0;
            var tCraftLocalElemDamageLo = 0;
            var tCraftLocalElemDamage = 0;
            var tCraftFlatSPDLo = 0;
            var tCraftFlatSPD = 0;
            var tCraftSpellCritLo = 0;
            var tCraftSpellCrit = 0;
            var tCraftCritDamageLo = 0;
            var tCraftCritDamage = 0;
            var tCraftCritLo = 0;
            var tCraftCrit = 0;
            var tCraftIasLo = 0;
            var tCraftIas = 0;
            var tTTcraft = "";
            var tTTcraftPrice = "";


            if (!IsAffixCrit && (Suffixes < 3))
            {
                tCraftCritLo = 22;
                tCraftCrit = 27;
                tTTcraft = "[CritChance]";
                tTTcraftPrice = "[4alch]";
                goto skipspellcraft;
            }

            if ((IsSpellDamageAff != AffixSolution.Positive) && (Prefixes < 3))
            {
                tCraftSpellDamageLo = 35;
                tCraftSpellDamage = 44;
                tTTcraft = "[Spell]";
                tTTcraftPrice = "[4Chaos]";
                goto skipspellcraft;
            }

            if (!IsIAS && (Suffixes < 3))
            {
                tCraftIasLo = 12;
                tCraftIas = 15;
                tTTcraft = "[IAS]";
                tTTcraftPrice = "[4chaos]";
                goto skipspellcraft;
            }

            if (!IsAffixSpellCrit && (Suffixes < 3))
            {
                tCraftSpellCritLo = 50;
                tCraftSpellCrit = 69;
                CraftTtSPD = "[AffixSpellCrit]";
                CraftTtSPDPrice = "[2chaos]";
                goto skipspellcraft;
            }

            if (!IsAffixCritDamage && (Suffixes < 3))
            {
                tCraftCritDamageLo = 21;
                tCraftCritDamage = 27;
                tTTcraft = "[AffixCritDamage]";
                tTTcraftPrice = "[4alchemy]";
                goto skipspellcraft;
            }


            if (!IsFlatColdSPD && !IsFlatLightningSPD && !IsFlatFireSPD && (Prefixes < 3))
            {
                tCraftFlatSPDLo = 20;
                tCraftFlatSPD = 23;
                tTTcraft = "[FlatSPD]";
                tTTcraftPrice = "[3chaos]";
                goto skipspellcraft;
            }


            if ((tLocalElemDamage < 19) && (Suffixes < 3))
            {
                tCraftLocalElemDamageLo = 15;
                tCraftLocalElemDamage = 19;
                tTTcraft = "[LocalElem]";
                tTTcraftPrice = "[10augmentation]";
            }

            skipspellcraft:
            CraftCOCSPDLo = SPD + tCraftSpellDamageLo;
            CraftCOCSPD = SPD + tCraftSpellDamage;
            CraftCOCLocalElemDamageLo = tCraftLocalElemDamageLo + ImplicitLocalElemDamage;
            CraftCOCLocalElemDamage = tCraftLocalElemDamage + ImplicitLocalElemDamage;
            CraftCOCTotalSPDLo = SPD + LocalElemDamage + tCraftSpellDamageLo + tCraftLocalElemDamageLo;
            CraftCOCTotalSPD = SPD + LocalElemDamage + tCraftSpellDamage + tCraftLocalElemDamage;
            CraftCOCSpellCritDamageLo = CritDamage + tCraftCritDamageLo;
            CraftCOCSpellCritDamage = CritDamage + tCraftCritDamage;
            CraftCOCSpellCritLo = AffixSpellCrit + tCraftSpellCritLo + ImplicitGlobalCrit;
            CraftCOCSpellCrit = AffixSpellCrit + tCraftSpellCrit + ImplicitGlobalCrit;
            CraftCOCFlatSPDLo = tCraftFlatSPDLo + FlatSPD;
            CraftCOCFlatSPD = tCraftFlatSPD + FlatSPD;
            CraftCOCCritLo = Crit + tCraftCritLo;
            CraftCOCCrit = Crit + tCraftCrit;
            CraftCOCAPSLo = Math.Round(BaseAPS*(100 + tCraftIasLo)/100, 2);
            CraftCOCAPS = Math.Round(BaseAPS*(100 + tCraftIas)/100, 2);
            CraftTtCOC = tTTcraft;
            CraftTtCOCPrice = tTTcraftPrice;

            return true;
        }

        private bool CalcMultiCOC()
        {
            if (!IsSceptre && (ClassType != ItemClassType.Dagger)) return false;

            MultiCOCSPDLo = CraftCOCSPDLo;
            MultiCOCSPD = CraftCOCSPD;
            MultiCOCLocalElemDamageLo = CraftCOCLocalElemDamageLo;
            MultiCOCLocalElemDamage = CraftCOCLocalElemDamage;
            MultiCOCTotalSPDLo = CraftCOCTotalSPDLo;
            MultiCOCTotalSPD = CraftCOCTotalSPD;
            MultiCOCSpellCritDamageLo = CraftCOCSpellCritDamageLo;
            MultiCOCSpellCritDamage = CraftCOCSpellCritDamage;
            MultiCOCSpellCritLo = CraftCOCSpellCritLo;
            MultiCOCSpellCrit = CraftCOCSpellCrit;
            MultiCOCFlatSPDLo = CraftCOCFlatSPDLo;
            MultiCOCFlatSPD = CraftCOCFlatSPD;
            MultiCOCCritLo = CraftCOCCritLo;
            MultiCOCCrit = CraftCOCCrit;
            MultiCOCAPSLo = CraftCOCAPSLo;
            MultiCOCAPS = CraftCOCAPS;
            MultiTtCOC = CraftTtCOC;
            MultiTtCOCPrice = CraftTtCOCPrice;

            if ((Prefixes > 2) && (Suffixes > 2)) return false;

            var tLocalElemDamage = LocalFireDamage >= LocalColdDamage
                ? (LocalFireDamage >= LocalLightDamage ? LocalFireDamage : LocalLightDamage)
                : (LocalColdDamage >= LocalLightDamage ? LocalColdDamage : LocalLightDamage);

            var tMultiSpellDamageLo = 0;
            var tMultiSpellDamage = 0;
            var tMultiLocalElemDamageLo = 0;
            var tMultiLocalElemDamage = 0;
            var tMultiFlatSPDLo = 0;
            var tMultiFlatSPD = 0;
            var tMultiSpellCritLo = 0;
            var tMultiSpellCrit = 0;
            var tMultiCritDamageLo = 0;
            var tMultiCritDamage = 0;
            var tMultiCritLo = 0;
            var tMultiCrit = 0;
            var tMultiIasLo = 0;
            var tMultiIas = 0;
            var tPrefixes = Prefixes;
            var tSuffixes = Suffixes;
            var tTTMulti = "";
            var tTTMultiPrice = "";


            if ((!IsAffixCrit || (IsAffixCrit && AffixCrit < 50)) && (!IsAffixCritDamage || (AffixCritDamage < 20)))
            {
                tTTMulti += "[ClearSuffixes]";
                tTTMultiPrice += "[2exa][Scouring]";
                tSuffixes = 0;
            }


            tTTMultiPrice += "[2exa]";
            tTTMulti += "[MultiMod]";
            tSuffixes++;


            if ((IsSpellDamageAff != AffixSolution.Positive) && (tPrefixes < 3))
            {
                tMultiSpellDamageLo = 35;
                tMultiSpellDamage = 44;
                tTTMulti += "[Spell]";
                tTTMultiPrice += "[4Chaos]";
                tPrefixes++;
            }

            if (!IsAffixCrit && (tSuffixes < 3))
            {
                tMultiCritLo = 22;
                tMultiCrit = 27;
                tTTMulti += "[CritChance]";
                tTTMultiPrice += "[4alch]";
                tSuffixes++;
            }


            if (!IsIAS && (tSuffixes < 3))
            {
                tMultiIasLo = 12;
                tMultiIas = 15;
                tTTMulti += "[IAS]";
                tTTMultiPrice += "[4chaos]";
                tSuffixes++;
            }


            if (!IsAffixSpellCrit && (tSuffixes < 3))
            {
                tMultiSpellCritLo = 50;
                tMultiSpellCrit = 69;
                tTTMulti += "[AffixSpellCrit]";
                tTTMultiPrice += "[2chaos]";
                tSuffixes++;
            }

            if (!IsAffixCritDamage && (tSuffixes < 3))
            {
                tMultiCritDamageLo = 21;
                tMultiCritDamage = 27;
                tTTMulti += "[AffixCritDamage]";
                tTTMultiPrice += "[4alchemy]";
                tSuffixes++;
            }


            if (!IsFlatColdSPD && !IsFlatLightningSPD && !IsFlatFireSPD && (tPrefixes < 3))
            {
                tMultiFlatSPDLo = 20;
                tMultiFlatSPD = 23;
                tTTMulti += "[FlatSPD]";
                tTTMultiPrice += "[3chaos]";
                tPrefixes++;
            }


            if ((tLocalElemDamage < 19) && (tSuffixes < 3))
            {
                tMultiLocalElemDamageLo = 15;
                tMultiLocalElemDamage = 19;
                tTTMulti += "[LocalElem]";
                tTTMultiPrice += "[10augmentation]";
            }
            if (tTTMulti == "[MultiMod]")
                return true;

            MultiCOCSPDLo = SPD + tMultiSpellDamageLo;
            MultiCOCSPD = SPD + tMultiSpellDamage;
            MultiCOCLocalElemDamageLo = tMultiLocalElemDamageLo + ImplicitLocalElemDamage;
            MultiCOCLocalElemDamage = tMultiLocalElemDamage + ImplicitLocalElemDamage;
            MultiCOCTotalSPDLo = SPD + LocalElemDamage + tMultiSpellDamageLo + tMultiLocalElemDamageLo;
            MultiCOCTotalSPD = SPD + LocalElemDamage + tMultiSpellDamage + tMultiLocalElemDamage;
            MultiCOCSpellCritDamageLo = CritDamage + tMultiCritDamageLo;
            MultiCOCSpellCritDamage = CritDamage + tMultiCritDamage;
            MultiCOCSpellCritLo = AffixSpellCrit + tMultiSpellCritLo + ImplicitGlobalCrit;
            MultiCOCSpellCrit = AffixSpellCrit + tMultiSpellCrit + ImplicitGlobalCrit;
            MultiCOCFlatSPDLo = tMultiFlatSPDLo + FlatSPD;
            MultiCOCFlatSPD = tMultiFlatSPD + FlatSPD;
            MultiCOCCritLo = Crit + tMultiCritLo;
            MultiCOCCrit = Crit + tMultiCrit;
            MultiCOCAPSLo = Math.Round(BaseAPS*(100 + tMultiIasLo)/100, 2);
            MultiCOCAPS = Math.Round(BaseAPS*(100 + tMultiIas)/100, 2);
            MultiTtCOC = tTTMulti;
            MultiTtCOCPrice = tTTMultiPrice;

            return true;
        }


        private void CalcLife()
        {
            TotalMaxLife = MaxLife + Str/5;
        }


        private void DuCalcsMich()
        {
            //APS section
            PAPS = EAPS = APS = Math.Round(BaseAPS*(100 + IAS)/100, 2);


            //crit section
            PCrit = ECrit = Crit = Math.Round(BaseCC*(100 + AffixCrit + ImplicitCrit)/100, 2);
            TotalCrit = GlobalCrit + ImplicitGlobalCrit;
            PCritDamage = ECritDamage = CritDamage = AffixCritDamage + ImplicitCritDamage;

            //phys
            FlatPhys /= 2;
            PDPS = Math.Round(
                (double) (BaseDamageLo/2 + BaseDamageHi/2 + FlatPhys)*(120 + LocalPhys)/100*BaseAPS*(100 + IAS)/
                100, 1);


            //elem
            FlatLightning /= 2;
            FlatCold /= 2;
            FlatChaos /= 2;
            FlatFire /= 2;

            FlatElem = Math.Round((double) (FlatLightning + FlatFire + FlatCold), 1);
            EDPS = Math.Round(FlatElem*BaseAPS*(100 + IAS)/100, 1);

            //spell
            var tLocalElemDamage = LocalFireDamage >= LocalColdDamage
                ? (LocalFireDamage >= LocalLightDamage ? LocalFireDamage : LocalLightDamage)
                : (LocalColdDamage >= LocalLightDamage ? LocalColdDamage : LocalLightDamage);
            SpellCritDamage = AffixCritDamage + ImplicitCritDamage;
            SPD = SPD + ImplicitSPD;
            LocalElemDamage = tLocalElemDamage + ImplicitLocalElemDamage;
            TotalSPD = SPD + LocalElemDamage;
            SpellCrit = AffixSpellCrit + ImplicitGlobalCrit;
            FlatSPD = Math.Round((double) (FlatLightningSPD + FlatFireSPD + FlatColdSPD)/2);
            TotalCastSpeed = CastSpeed + ImplicitCastSpeed;
            //total
            DPS = EDPS + PDPS;

            //crafts
            CalcCraftPhysDPS();
            CalcMultiPhysDPS();
            CalcCraftElemDPS();
            CalcMultiElemDPS();
            CalcCraftSpellDamage();
            CalcMultiSpellDamage();
            CalcCraftCOC();
            CalcMultiCOC();

            //other calcs
            CalcLife();
            CalcTotalRes();
            CalcArmour();
        }


        private void CalcArmour()
        {
            if (!IsArmour && (ClassType != ItemClassType.Shield))
                return;

            AR = Math.Round((double) (BaseAR + FlatAR)*(LocalArmour + 120)/100);
            EV = Math.Round((double) (BaseEV + FlatEV)*(LocalArmour + 120)/100);
            ES = Math.Round((double) (BaseES + FlatES)*(LocalArmour + 120)/100);

            CraftAR = AR;
            CraftEV = EV;
            CraftES = ES;
            CraftTotalRes = TotalRes;
            if ((Prefixes > 2) && (Suffixes > 2)) return;

            var tLocalArmour = 0;
            var tFlatAR = 0d;
            var tFlatEV = 0d;
            var tFlatES = 0d;
            var tLocalArmourLo = 0d;
            var tFlatARLo = 0d;
            var tFlatESLo = 0d;
            var tFlatEVLo = 0d;
            var tTTcraft = "";
            var tTTcraftPrice = "";

            if ((IsLocalArmourAff != AffixSolution.Positive) && (Prefixes < 3))
            {
                tLocalArmourLo = 55;
                tLocalArmour = 68;
                tTTcraft = "[+% Armour]";
                tTTcraftPrice = "[10 trans]";
                goto armourCalcEnd;
            }

            if (!IsFlatES && (BaseES > 0) && (BaseAR == 0) && (BaseEV == 0) && (Prefixes < 3))
            {
                tFlatESLo = 18;
                tFlatES = 22;
                tTTcraft = "[FlatES]";
                tTTcraftPrice = "[2 chaos]";
                goto armourCalcEnd;
            }

            if (Suffixes < 3)
            {
                CraftTotalResLo = TotalRes + 21;
                CraftTotalRes = TotalRes + 30;
                tTTcraft = "[Res]";
                tTTcraftPrice = "[2 alchemy]";
                goto armourCalcEnd;
            }

            if (!IsFlatES && (BaseES > 0) && (Prefixes < 3))
            {
                tFlatESLo = 18;
                tFlatES = 22;
                tTTcraft = "[FlatES]";
                tTTcraftPrice = "[2 chaos]";
                goto armourCalcEnd;
            }

            if (!IsFlatEV && (BaseEV > 0) && (Prefixes < 3))
            {
                tFlatEVLo = 51;
                tFlatEV = 80;
                tTTcraft = "[FlatEV]";
                tTTcraftPrice = "[2 chaos]";
            }
            if (!IsFlatAR && (BaseAR > 0) && (Prefixes < 3))
            {
                tFlatARLo = 51;
                tFlatAR = 80;
                tTTcraft = "[FlatAR]";
                tTTcraftPrice = "[2 chaos]";
            }


            armourCalcEnd:
            CraftARLo = Math.Round((BaseAR + FlatAR + tFlatARLo)*(LocalArmour + tLocalArmourLo + 120)/100);
            CraftEVLo = Math.Round((BaseEV + FlatEV + tFlatEVLo)*(LocalArmour + tLocalArmourLo + 120)/100);
            CraftESLo = Math.Round((BaseES + FlatES + tFlatESLo)*(LocalArmour + tLocalArmourLo + 120)/100);
            CraftAR = Math.Round((BaseAR + FlatAR + tFlatAR)*(LocalArmour + tLocalArmour + 120)/100);
            CraftEV = Math.Round((BaseEV + FlatEV + tFlatEV)*(LocalArmour + tLocalArmour + 120)/100);
            CraftES = Math.Round((BaseES + FlatES + tFlatES)*(LocalArmour + tLocalArmour + 120)/100);
            CraftTtArmour = tTTcraft;
            CraftTtArmourPrice = tTTcraftPrice;
        }

        private void CalcTotalRes()
        {
            TotalRes = LightningRes + ColdRes + FireRes + ChaosRes + AllRes*3;
            if ((ClassType != ItemClassType.Ring) && (ClassType != ItemClassType.Amulet) &&
                (ClassType != ItemClassType.Quiver) && (ClassType != ItemClassType.Belt))
                return;
            if (Suffixes < 3)
            {
                CraftTotalRes = TotalRes + 25;
                CraftTotalResLo = TotalRes + 21;
                CraftTtTotalRes = "[Res]";
                CraftTtTotalResPrice = "[2 alchemy]";
            }
            else
                CraftTotalRes = TotalRes;
        }


        private void SolveItemRarityAffixes(IDictionary<AffixBracketType, AffixBracketsSource> affixBrackets)
        {
            if (!IsRarity) return;
            IsSuffixRarity = AffixSolution.Uncertain;
            IsPrefixRarity = AffixSolution.Uncertain;

            if (Prefixes > 2)
            {
                Suffixes++;
                IsPrefixRarity = AffixSolution.Negative;
                IsSuffixRarity = AffixSolution.Positive;
                Affixes--;
                return;
            }
            if (Suffixes > 2)
            {
                Prefixes++;
                IsPrefixRarity = AffixSolution.Positive;
                IsSuffixRarity = AffixSolution.Negative;
                Affixes--;
                return;
            }
            if (iLevel < 20)
            {
                Suffixes++;
                IsPrefixRarity = AffixSolution.Negative;
                IsSuffixRarity = AffixSolution.Positive;
                Affixes--;
                return;
            }
            if (Rarity < 8)
            {
                Suffixes++;
                IsPrefixRarity = AffixSolution.Negative;
                IsSuffixRarity = AffixSolution.Positive;
                Affixes--;
                return;
            }
            if ((iLevel < 30) && (Rarity < 13))
            {
                Prefixes++;
                IsPrefixRarity = AffixSolution.Positive;
                IsSuffixRarity = AffixSolution.Negative;
                Affixes--;
                return;
            }

            var maxRarityPrefix = affixBrackets[AffixBracketType.ItemRarityPrefix].GetAffixMinMaxFromiLevel(iLevel,
                "Rarity", AffixBracketsSource.MinOrMax.Max);
            var maxRaritySuffix = affixBrackets[AffixBracketType.ItemRaritySuffix].GetAffixMinMaxFromiLevel(iLevel,
                "Rarity", AffixBracketsSource.MinOrMax.Max);

            if (maxRarityPrefix > maxRaritySuffix)
            {
                if (Rarity > maxRarityPrefix)
                {
                    IsPrefixRarity = AffixSolution.Positive;
                    IsSuffixRarity = AffixSolution.Positive;
                    Prefixes++;
                    Suffixes++;
                    Affixes--;
                    return;
                }
                if (Rarity > maxRaritySuffix)
                {
                    Prefixes++;
                    IsPrefixRarity = AffixSolution.Positive;
                    IsSuffixRarity = AffixSolution.Uncertain;
                    return;
                }
                IsPrefixRarity = AffixSolution.Uncertain;
                IsSuffixRarity = AffixSolution.Uncertain;
            }
            else
            {
                if (Rarity > maxRaritySuffix)
                {
                    IsPrefixRarity = AffixSolution.Positive;
                    IsSuffixRarity = AffixSolution.Positive;
                    Prefixes++;
                    Suffixes++;
                    Affixes--;
                    return;
                }
                if (Rarity > maxRarityPrefix)
                {
                    Suffixes++;
                    IsPrefixRarity = AffixSolution.Uncertain;
                    IsSuffixRarity = AffixSolution.Positive;
                    return;
                }
                IsPrefixRarity = AffixSolution.Uncertain;
                IsSuffixRarity = AffixSolution.Uncertain;
            }
        }

        private void SolveArmourStunRecoveryAffixes(IDictionary<AffixBracketType, AffixBracketsSource> affixBrackets)
        {
            if (!IsArmour) return;

            if (IsLocalArmour)
            {
                if (!IsStunRecovery)
                {
                    IsLocalArmourAff = AffixSolution.Positive;
                    IsComboLocalArmourAff = AffixSolution.Negative;
                    IsStunRecoveryAff = AffixSolution.Negative;
                    return;
                }
                if (Affixes < 1)
                {
                    Console.WriteLine($"[Item.SolveArmourStunRecoveryAffixes] Not enough unsolved affixes: {Affixes}");
                    return;
                }
            }
            else
            {
                if (IsStunRecovery)
                {
                    IsLocalArmourAff = AffixSolution.Negative;
                    IsComboLocalArmourAff = AffixSolution.Negative;
                    IsStunRecoveryAff = AffixSolution.Positive;
                    Affixes--;
                    Suffixes++;
                    return;
                }
                return;
            }

            IsLocalArmourAff = AffixSolution.Uncertain;
            IsComboLocalArmourAff = AffixSolution.Uncertain;
            IsStunRecoveryAff = AffixSolution.Uncertain;

            var comboArmourMax = affixBrackets[AffixBracketType.ComboArmourStun].GetAffixMinMaxFromiLevel(iLevel,
                "Armour", AffixBracketsSource.MinOrMax.Max);
            var comboStunMax = affixBrackets[AffixBracketType.ComboArmourStun].GetAffixMinMaxFromiLevel(iLevel,
                "StunRecovery", AffixBracketsSource.MinOrMax.Max);

            var armourMax = affixBrackets[AffixBracketType.Armour].GetAffixMinMaxFromiLevel(iLevel, "Armour",
                AffixBracketsSource.MinOrMax.Max);
            var stunMax = affixBrackets[AffixBracketType.StunRecovery].GetAffixMinMaxFromiLevel(iLevel, "StunRecovery",
                AffixBracketsSource.MinOrMax.Max);

            if (LocalArmour > armourMax)
            {
                IsLocalArmourAff = AffixSolution.Positive;
                IsComboLocalArmourAff = AffixSolution.Positive;
                Prefixes++;
                Affixes--;
                if (Suffixes > 2)
                {
                    IsStunRecoveryAff = AffixSolution.Negative;
                    return;
                }
                if (stunMax > comboStunMax)
                {
                    IsStunRecoveryAff = AffixSolution.Positive;
                    Suffixes++;
                    return;
                }
                if (StunRecovery < 17)
                {
                    IsStunRecoveryAff = AffixSolution.Negative;
                    return;
                }
                IsStunRecoveryAff = AffixSolution.Uncertain;
                Affixes--;
                return;
            }

            if (LocalArmour > comboArmourMax)
            {
                IsLocalArmourAff = AffixSolution.Positive;
                if (Prefixes > 2)
                {
                    IsComboLocalArmourAff = AffixSolution.Negative;
                    IsStunRecoveryAff = AffixSolution.Positive;
                    Affixes--;
                    return;
                }
                if (Suffixes > 2)
                {
                    IsComboLocalArmourAff = AffixSolution.Positive;
                    IsStunRecoveryAff = AffixSolution.Negative;
                    Affixes--;
                    return;
                }

                if (StunRecovery < 11)
                {
                    IsStunRecoveryAff = AffixSolution.Negative;
                    IsComboLocalArmourAff = AffixSolution.Positive;
                    Prefixes++;
                    Affixes--;
                    return;
                }

                if (StunRecovery > comboStunMax)
                {
                    IsStunRecoveryAff = AffixSolution.Positive;
                    Suffixes++;
                    Affixes--;
                    if (StunRecovery > stunMax)
                    {
                        Prefixes++;
                        IsComboLocalArmourAff = AffixSolution.Positive;
                        return;
                    }
                    IsComboLocalArmourAff = AffixSolution.Uncertain;
                    return;
                }
                Affixes--;
                IsComboLocalArmourAff = AffixSolution.Uncertain;
                IsStunRecoveryAff = AffixSolution.Uncertain;
                return;
            }


            if (LocalArmour <= comboArmourMax)
            {
                int stunFromArmourMin, stunFromArmourMax;
                affixBrackets[AffixBracketType.ComboArmourStun].GetAffixValueRangeFromAffixValue("Armour", LocalArmour,
                    "StunRecovery", out stunFromArmourMin, out stunFromArmourMax);
                Affixes--;

                //100% not ComboArmour and LocalArmour at the same time
                if (Prefixes > 2)
                {
                    if (Suffixes > 2)
                    {
                        IsComboLocalArmourAff = AffixSolution.Positive;
                        IsLocalArmourAff = AffixSolution.Negative;
                        IsStunRecoveryAff = AffixSolution.Negative;
                        return;
                    }
                    if (StunRecovery > stunFromArmourMax)
                    {
                        IsComboLocalArmourAff = AffixSolution.Positive;
                        IsLocalArmourAff = AffixSolution.Negative;
                        IsStunRecoveryAff = AffixSolution.Positive;
                        Suffixes++;
                        Affixes--;
                        return;
                    }

                    if (StunRecovery < stunFromArmourMin)
                    {
                        IsComboLocalArmourAff = AffixSolution.Negative;
                        IsLocalArmourAff = AffixSolution.Positive;
                        IsStunRecoveryAff = AffixSolution.Positive;
                        Suffixes++;
                        Affixes--;
                        return;
                    }
                    if (StunRecovery < 11)
                    {
                        IsComboLocalArmourAff = AffixSolution.Positive;
                        IsLocalArmourAff = AffixSolution.Negative;
                        IsStunRecoveryAff = AffixSolution.Negative;
                        Affixes--;
                        return;
                    }

                    if (LocalArmour < 11)
                    {
                        IsComboLocalArmourAff = AffixSolution.Positive;
                        IsLocalArmourAff = AffixSolution.Negative;
                        if (StunRecovery > 7)
                        {
                            IsStunRecoveryAff = AffixSolution.Negative;
                            return;
                        }
                        IsStunRecoveryAff = AffixSolution.Uncertain;
                        return;
                    }
                    IsComboLocalArmourAff = AffixSolution.Uncertain;
                    IsLocalArmourAff = AffixSolution.Uncertain;
                    IsStunRecoveryAff = AffixSolution.Uncertain;
                    return;
                }
                //100% not StunRecoverySuffix
                if (Suffixes > 2)
                {
                    IsStunRecoveryAff = AffixSolution.Negative;
                    IsComboLocalArmourAff = AffixSolution.Positive;
                    if (StunRecovery >= stunFromArmourMin)
                    {
                        IsLocalArmourAff = AffixSolution.Negative;
                        return;
                    }
                    if (StunRecovery < stunFromArmourMin)
                    {
                        IsLocalArmourAff = AffixSolution.Positive;
                        Prefixes++;
                        return;
                    }
                    if (LocalArmour < 17)
                    {
                        IsLocalArmourAff = AffixSolution.Negative;
                        return;
                    }

                    IsLocalArmourAff = AffixSolution.Uncertain;
                    return;
                }

                //100% StunRecoverySuffix and ComboArmour
                if (StunRecovery > stunMax)
                {
                    IsStunRecoveryAff = AffixSolution.Positive;
                    IsComboLocalArmourAff = AffixSolution.Positive;
                    Suffixes++;
                    Affixes--;

                    //попробовать рассчитать наличие LocalArmourAffix
                    int armourFromStunMin, armourFromStunMax;
                    affixBrackets[AffixBracketType.ComboArmourStun].GetAffixValueRangeFromAffixValue("StunRecovery",
                        StunRecovery - 11, "Armour", out armourFromStunMin, out armourFromStunMax);
                    if (LocalArmour > armourFromStunMax)
                    {
                        IsLocalArmourAff = AffixSolution.Positive;
                        return;
                    }

                    IsLocalArmourAff = AffixSolution.Uncertain;
                    return;
                }

                // 100% StunRecoveryAffix
                if (StunRecovery > stunFromArmourMax)
                {
                    IsStunRecoveryAff = AffixSolution.Positive;
                    Suffixes++;
                    Affixes--;
                    int armourFromStunMin, armourFromStunMax;
                    affixBrackets[AffixBracketType.ComboArmourStun].GetAffixValueRangeFromAffixValue("StunRecovery",
                        StunRecovery - 11, "Armour", out armourFromStunMin, out armourFromStunMax);
                    if (LocalArmour > armourFromStunMax)
                    {
                        IsLocalArmourAff = AffixSolution.Positive;
                        IsComboLocalArmourAff = AffixSolution.Uncertain;
                        return;
                    }
                    IsLocalArmourAff = AffixSolution.Uncertain;
                    IsComboLocalArmourAff = AffixSolution.Uncertain;
                }

                IsComboLocalArmourAff = AffixSolution.Uncertain;
                IsLocalArmourAff = AffixSolution.Uncertain;
                IsStunRecoveryAff = AffixSolution.Uncertain;
            }
        } //solve comboarmour


        private void SolveSpellDamageManaAffixes(IDictionary<AffixBracketType, AffixBracketsSource> affixBrackets)
        {
            if (!IsMaxMana)
                if (!IsSPD)
                    return;
                else
                {
                    IsSpellDamageAff = AffixSolution.Positive;
                    return;
                }

            if (!IsSPD)
            {
                Affixes--;
                IsMaxManaAff = AffixSolution.Positive;
                Prefixes++;
                return;
            }

            if (!IsWeapon)
            {
                Affixes--;
                Prefixes++;
                IsMaxManaAff = AffixSolution.Positive;
                IsSpellDamageAff = AffixSolution.Positive;
                return;
            }

            IsSpellDamageAff = AffixSolution.Uncertain;
            IsComboSpellDamageAff = AffixSolution.Uncertain;
            IsMaxManaAff = AffixSolution.Uncertain;
            Affixes--;

            int comboManaMax, spMax, spMin, manaFromSpMin, manaFromSpMax, comboSpMax;
            int spFromManaMinusMinMana_Min,
                spFromManaMinusMinMana_Max,
                manaFromSpMinusMinSp_Min,
                manaFromSpMinusMinSp_Max;

            switch (GripType)
            {
                case "1h":
                    comboManaMax = affixBrackets[AffixBracketType.ComboSpellMana].GetAffixMinMaxFromiLevel(iLevel,
                        "MaxMana", AffixBracketsSource.MinOrMax.Max);
                    spMax = affixBrackets[AffixBracketType.SpellDamage].GetAffixMinMaxFromiLevel(iLevel, "SPD",
                        AffixBracketsSource.MinOrMax.Max);
                    spMin = 10;
                    affixBrackets[AffixBracketType.ComboSpellMana].GetAffixValueRangeFromAffixValue("SPD", SPD,
                        "MaxMana", out manaFromSpMin, out manaFromSpMax);
                    comboSpMax = affixBrackets[AffixBracketType.ComboSpellMana].GetAffixMinMaxFromiLevel(iLevel,
                        "SPD", AffixBracketsSource.MinOrMax.Max);
                    affixBrackets[AffixBracketType.ComboSpellMana].GetAffixValueRangeFromAffixValue("MaxMana",
                        MaxMana - 15, "SPD", out spFromManaMinusMinMana_Min, out spFromManaMinusMinMana_Max);
                    affixBrackets[AffixBracketType.ComboSpellMana].GetAffixValueRangeFromAffixValue("SPD", SPD - 10,
                        "MaxMana", out manaFromSpMinusMinSp_Min, out manaFromSpMinusMinSp_Max);
                    break;
                default:
                    comboManaMax = affixBrackets[AffixBracketType.ComboStaffSpellMana].GetAffixMinMaxFromiLevel(iLevel,
                        "MaxMana", AffixBracketsSource.MinOrMax.Max);
                    spMax = affixBrackets[AffixBracketType.StaffSpellDamage].GetAffixMinMaxFromiLevel(iLevel, "SPD",
                        AffixBracketsSource.MinOrMax.Max);
                    spMin = 15;
                    affixBrackets[AffixBracketType.ComboStaffSpellMana].GetAffixValueRangeFromAffixValue("SPD", SPD,
                        "MaxMana", out manaFromSpMin, out manaFromSpMax);
                    comboSpMax = affixBrackets[AffixBracketType.ComboStaffSpellMana].GetAffixMinMaxFromiLevel(iLevel,
                        "SPD", AffixBracketsSource.MinOrMax.Max);
                    affixBrackets[AffixBracketType.ComboStaffSpellMana].GetAffixValueRangeFromAffixValue("MaxMana",
                        MaxMana - 15, "SPD", out spFromManaMinusMinMana_Min, out spFromManaMinusMinMana_Max);
                    affixBrackets[AffixBracketType.ComboSpellMana].GetAffixValueRangeFromAffixValue("SPD", SPD - 15,
                        "MaxMana", out manaFromSpMinusMinSp_Min, out manaFromSpMinusMinSp_Max);
                    break;
            }

            var manaMax = affixBrackets[AffixBracketType.MaxMana].GetAffixMinMaxFromiLevel(iLevel, "MaxMana",
                AffixBracketsSource.MinOrMax.Max);


            if (SPD > spMax)
            {
                IsSpellDamageAff = AffixSolution.Positive;
                IsComboSpellDamageAff = AffixSolution.Positive;
                Prefixes++;
                if (Prefixes > 2)
                {
                    IsMaxManaAff = AffixSolution.Negative;
                    return;
                }
                if (MaxMana > comboManaMax)
                {
                    IsMaxManaAff = AffixSolution.Positive;
                    Prefixes++;
                    return;
                }
                // можно добавить проверку на сп+минкомбосп
                if (MaxMana < 15)
                {
                    IsMaxManaAff = AffixSolution.Negative;
                    return;
                }
                return;
            }

            if (SPD > comboSpMax)
            {
                IsSpellDamageAff = AffixSolution.Positive;
                if (MaxMana > manaMax)
                {
                    IsMaxManaAff = AffixSolution.Positive;
                    IsComboSpellDamageAff = AffixSolution.Positive;
                    Prefixes++;
                    return;
                }
                if (MaxMana < 15)
                {
                    IsMaxManaAff = AffixSolution.Negative;
                    IsComboSpellDamageAff = AffixSolution.Positive;
                    Prefixes++;
                    return;
                }
                if (Prefixes == 2)
                {
                    if (MaxMana > comboManaMax)
                    {
                        IsComboSpellDamageAff = AffixSolution.Negative;
                        IsMaxManaAff = AffixSolution.Positive;
                        Prefixes++;
                        return;
                    }
                    Prefixes++;
                    //мана или комбосп (гарантированный аффикс из двух)
                    return;
                }
                if (MaxMana > comboManaMax)
                {
                    IsMaxManaAff = AffixSolution.Positive;
                    Prefixes++;
                    //попробовать рассчитать наличие комбоспеллдемеджа
                    if (Prefixes == 3)
                    {
                        IsComboLocalArmourAff = AffixSolution.Negative;
                        return;
                    }

                    if (MaxMana > manaFromSpMinusMinSp_Max)
                    {
                        IsComboSpellDamageAff = AffixSolution.Negative;
                        return;
                    }
                    IsComboSpellDamageAff = AffixSolution.Uncertain;
                    return;
                }
                IsMaxManaAff = AffixSolution.Uncertain;
                IsComboSpellDamageAff = AffixSolution.Uncertain;
                return;
            }

            if (SPD <= comboSpMax)
            {
                if (Prefixes == 3)
                {
                    IsComboSpellDamageAff = AffixSolution.Positive;
                    IsSpellDamageAff = AffixSolution.Negative;
                    IsMaxManaAff = AffixSolution.Negative;
                    return;
                }

                if (SPD < spMin)
                {
                    IsSpellDamageAff = AffixSolution.Negative;
                    IsComboSpellDamageAff = AffixSolution.Positive;
                    if (MaxMana > 10)
                    {
                        IsMaxManaAff = AffixSolution.Positive;
                        Prefixes++;
                        return;
                    }
                    IsMaxManaAff = AffixSolution.Negative;
                    return;
                }


                if (Prefixes == 2)
                {
                    if (MaxMana > manaMax)
                    {
                        IsMaxManaAff = AffixSolution.Positive;
                        IsComboSpellDamageAff = AffixSolution.Positive;
                        IsSpellDamageAff = AffixSolution.Negative;
                        Prefixes++;
                        return;
                    }


                    if (MaxMana > comboManaMax)
                    {
                        IsMaxManaAff = AffixSolution.Positive;
                        Prefixes++;
                        return;
                    }

                    if (MaxMana < 15)
                    {
                        IsMaxManaAff = AffixSolution.Negative;
                        IsComboSpellDamageAff = AffixSolution.Positive;
                        if (SPD > spFromManaMinusMinMana_Max)
                        {
                            IsSpellDamageAff = AffixSolution.Positive;
                            Prefixes++;
                            return;
                        }
                        IsSpellDamageAff = AffixSolution.Negative;
                        return;
                    }

                    if (SPD > spFromManaMinusMinMana_Max)
                    {
                        IsSpellDamageAff = AffixSolution.Positive;
                        Prefixes++;
                        return;
                    }
                    if (SPD < spFromManaMinusMinMana_Min)
                    {
                        IsMaxManaAff = AffixSolution.Positive;
                        Prefixes++;
                        return;
                    }
                }

                ////


                if (MaxMana > manaMax)
                {
                    IsComboSpellDamageAff = AffixSolution.Positive;
                    IsMaxManaAff = AffixSolution.Positive;
                    Prefixes++;
                    if (SPD > manaFromSpMinusMinSp_Max)
                    {
                        IsSpellDamageAff = AffixSolution.Positive;
                        Prefixes++;
                        return;
                    }
                    //возможно можно еще что-то проанализировать
                    return;
                }

                if (MaxMana > comboManaMax)
                {
                    IsMaxManaAff = AffixSolution.Positive;
                    Prefixes++;
                    if (SPD > manaFromSpMinusMinSp_Max)
                    {
                        IsSpellDamageAff = AffixSolution.Positive;
                        Prefixes++;
                        return;
                    }
                    return;
                }
                if (MaxMana < 15)
                {
                    IsMaxManaAff = AffixSolution.Negative;
                    IsComboSpellDamageAff = AffixSolution.Positive;
                    if (SPD > spFromManaMinusMinMana_Max)
                    {
                        IsSpellDamageAff = AffixSolution.Positive;
                        Prefixes++;
                        return;
                    }
                    if (SPD < spFromManaMinusMinMana_Min)
                    {
                        IsSpellDamageAff = AffixSolution.Positive;
                        Prefixes++;
                    }
                }
            }
        } //solvespelldamagemanaaffixes


        private void SolveComboPhysAccAffixes(IDictionary<AffixBracketType, AffixBracketsSource> affixBrackets)
        {
            if (!IsAccuracyRating && !IsLocalPhys) return;
            if ((ClassType == ItemClassType.Ring) || (ClassType == ItemClassType.Amulet) ||
                (ClassType == ItemClassType.Quiver) || (ClassType == ItemClassType.Gloves))
            {
                if (IsAccuracyRating)
                {
                    IsAccuracyRatingAff = AffixSolution.Positive;
                    Affixes--;
                    Suffixes++;
                }
                return;
            }

            int minLightAcc = 0, maxLightAcc = 0;
            const int minAcc = 5;
            if (IsLightRadius)
            {
                switch (LightRadius)
                {
                    case 5:
                        IsLightAccuracyRatingAff = AffixSolution.Positive;
                        minLightAcc = 10;
                        maxLightAcc = 20;
                        break;
                    case 10:
                        IsLightAccuracyRatingAff = AffixSolution.Positive;
                        minLightAcc = 21;
                        maxLightAcc = 40;
                        break;
                }
            }
            else
            {
                IsLightAccuracyRatingAff = AffixSolution.Negative;
            }

            if (ClassType == ItemClassType.Helm)
            {
                if (IsAccuracyRating)
                {
                    if ((AccuracyRating < minAcc + minLightAcc) || (AccuracyRating > maxLightAcc))
                    {
                        IsAccuracyRatingAff = AffixSolution.Positive;
                        Suffixes++;
                        Affixes--;
                        return;
                    }
                    IsAccuracyRatingAff = AffixSolution.Uncertain;
                    Affixes--;
                }
                return;
            }

            if (!IsWeapon) return;

            if (!IsAccuracyRating)
            {
                if (IsLocalPhys)
                {
                    IsLocalPhysAff = AffixSolution.Positive;
                    return;
                }
                IsLocalPhysAff = AffixSolution.Negative;
                return;
            }
            if (!IsLocalPhys)
            {
                Affixes--;
                if (Suffixes > 2)
                {
                    IsAccuracyRatingAff = AffixSolution.Negative;
                    return;
                }

                if ((AccuracyRating < minAcc + minLightAcc) || (AccuracyRating > maxLightAcc))
                {
                    IsAccuracyRatingAff = AffixSolution.Positive;
                    Suffixes++;
                    return;
                }
                IsAccuracyRatingAff = AffixSolution.Uncertain;
                return;
            }

            var maxPhys = affixBrackets[AffixBracketType.LocalPhys].GetAffixMinMaxFromiLevel(iLevel, "LocalPhys",
                AffixBracketsSource.MinOrMax.Max);
            var maxComboPhys = affixBrackets[AffixBracketType.ComboLocalPhysAcc].GetAffixMinMaxFromiLevel(iLevel,
                "LocalPhys",
                AffixBracketsSource.MinOrMax.Max);
            var maxAcc = affixBrackets[AffixBracketType.AccuracyRating].GetAffixMinMaxFromiLevel(iLevel,
                "AccuracyRating",
                AffixBracketsSource.MinOrMax.Max);
            var maxComboAcc = affixBrackets[AffixBracketType.ComboLocalPhysAcc].GetAffixMinMaxFromiLevel(iLevel,
                "AccuracyRating",
                AffixBracketsSource.MinOrMax.Max);
            var minComboAcc = affixBrackets[AffixBracketType.ComboLocalPhysAcc].GetAffixMinMaxFromiLevel(iLevel,
                "AccuracyRating", AffixBracketsSource.MinOrMax.Min);


            Affixes--;
            IsLocalPhysAff = AffixSolution.Uncertain;
            IsComboLocalPhysAff = AffixSolution.Uncertain;
            IsAccuracyRatingAff = AffixSolution.Uncertain;

            if (Prefixes == 3)
            {
                if (Suffixes == 3)
                {
                    IsComboLocalPhysAff = AffixSolution.Positive;
                    IsLocalPhysAff = AffixSolution.Negative;
                    IsAccuracyRatingAff = AffixSolution.Negative;
                    return;
                }

                if (AccuracyRating > maxComboAcc + maxLightAcc)
                {
                    IsComboLocalPhysAff = AffixSolution.Negative;
                    IsLocalPhysAff = AffixSolution.Positive;
                    IsAccuracyRatingAff = AffixSolution.Positive;
                    Suffixes++;
                    return;
                }
                if (AccuracyRating < minComboAcc + minLightAcc)
                {
                    IsComboLocalPhysAff = AffixSolution.Negative;
                    IsLocalPhysAff = AffixSolution.Positive;
                    IsAccuracyRatingAff = AffixSolution.Positive;
                    Suffixes++;
                    return;
                }
                return;
            }

            if (LocalPhys > maxComboPhys)
            {
                IsLocalPhysAff = AffixSolution.Positive;
                if (LocalPhys > maxPhys)
                {
                    IsComboLocalPhysAff = AffixSolution.Positive;
                    Prefixes++;
                }
                if (AccuracyRating > maxComboAcc + maxLightAcc)
                {
                    IsAccuracyRatingAff = AffixSolution.Positive;
                    Suffixes++;
                    return;
                }
                return;
            }

            int minAccFromComboPhys, maxAccFromComboPhys;

            affixBrackets[AffixBracketType.ComboLocalPhysAcc].GetAffixValueRangeFromAffixValue("LocalPhys", LocalPhys,
                "AccuracyRating", out minAccFromComboPhys, out maxAccFromComboPhys);

            if (LocalPhys <= maxComboPhys)
            {
                if (IsLightAccuracyRatingAff == AffixSolution.Positive)
                {
                    if (AccuracyRating > maxAccFromComboPhys + maxLightAcc)
                    {
                        Suffixes++;
                        IsAccuracyRatingAff = AffixSolution.Positive;
                        return;
                    }
                    if (AccuracyRating < minAccFromComboPhys + minLightAcc)
                    {
                        if (LocalPhys < 40)
                        {
                            IsLocalPhysAff = AffixSolution.Negative;
                            IsComboLocalPhysAff = AffixSolution.Positive;
                            return;
                        }
                        IsLocalPhysAff = AffixSolution.Positive;
                        Prefixes++;
                        return;
                    }
                    return;
                }

                if (AccuracyRating > maxAccFromComboPhys)
                {
                    IsAccuracyRatingAff = AffixSolution.Positive;
                    Suffixes++;
                    return;
                }

                if (AccuracyRating < minAcc)
                {
                    IsAccuracyRatingAff = AffixSolution.Negative;
                    IsComboLocalPhysAff = AffixSolution.Positive;
                    if (LocalPhys < 40)
                    {
                        IsLocalPhysAff = AffixSolution.Negative;
                        return;
                    }
                    IsLocalPhysAff = AffixSolution.Positive;
                }
            }
        } //solvecombophysacc
    } //class
} //namespace