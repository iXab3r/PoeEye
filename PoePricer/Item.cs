using System;
using System.Configuration;
using System.Diagnostics.Contracts;
using System.Diagnostics.Eventing.Reader;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters;
using System.Security.AccessControl;
using System.Security.Cryptography.X509Certificates;
using System.Security.Policy;
using System.Threading.Tasks;
using PoePricer.Parser;

namespace PoePricer
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Extensions;


    public class Item
    {
        public string Name = "Buriza";
        public string BaseType ="Cool Bow";
        public string ClassType = "Bow";
        public string Rarity = "";


        public string GripType = "";
        public string Implicit = "";



        
        



        public int iLevel = 0;
        public int Links = 0;

        public int BaseAR = 0;
        public int BaseEV = 0;
        public int BaseES = 0;

        public int BaseDamageLo;
        public int BaseDamageHi;
        public float BaseAPS = 0;
        public float BaseCC = 0;

        //ItemDataText

        public string ItemAffixPart;

        //Flags

        public bool IsUnidentified = false;
        public bool IsCorrupted = false;
        public bool IsNote = false;
        public string TradeNoteMessage;



        public enum ParseResult
        {
            Success,
            Unidentified,
            NotRare,
            WrongDataText

        }




        public float GetPropValue(string fieldName)
        {
            try
            {
                //return this.GetType().GetField(fieldName).GetValue(this) as float;
                return 0;
            }
            catch
            {
                return 0;
            }
        }





        public ParseResult ParseItemDataText(string itemDataText, IDictionary<AffixBracketType, AffixBrackets> affixBrackets, IDictionary<BaseItemTypes, BaseItemsSource> baseItems)
        {
            //seperate DataText for blocks via splitting with char "`"
            var itemDataParts = itemDataText.Replace("\r\n--------\r\n", "`").Split('`');
            var parseRegEx = new Regex(@"Rarity: (?'rarity'[A-z ']+)",RegexOptions.Compiled);
            var match = parseRegEx.Match(itemDataParts[0]);
            var lastPartIndex = itemDataParts.Length - 1;
            var affixPartIndex = lastPartIndex;
            
            
            //check rarity
            if (match.Success)
            {
                if (match.Groups["rarity"].Value != "Rare")
                {
                    Console.WriteLine($"[Item.ParseItemData] Not rare item. {itemDataText}");
                    return ParseResult.NotRare ;
                }
            }
            else
            {
                Console.WriteLine($"[Item.ParseItemData] Wrong DataText. {itemDataText}");
                return ParseResult.WrongDataText;
            }
            //check Identify

            if (itemDataText.Contains("Unidentified"))
            {
                IsUnidentified = true;
                return ParseResult.Unidentified;
            }

            //parsing item level
            iLevel = new Regex(@"\r\nItem Level: (?'iLevel'\d+)\r\n", RegexOptions.Compiled).Match(itemDataText).Groups["iLevel"].Value.ToInt();
            
            //parse text for Links
            Links = ParseLinks(itemDataText);

            //check corruption
            if (itemDataText.Contains("`Corrupted"))
            {
                IsCorrupted = true;
                affixPartIndex--;
            }

            //check TradingNote

            if (itemDataParts[lastPartIndex].Contains("Note:"))
            {
                IsNote = true;
                var noteRegExp = new Regex(@"^Note: (?'noteMessage'.*)$", RegexOptions.Compiled);
                var matchNote = noteRegExp.Match(itemDataParts[lastPartIndex]);
                TradeNoteMessage = matchNote.Success ? matchNote.Groups["noteMessage"].Value : "";
                affixPartIndex--;
            }
            
            //get Name,BaseType from first DataTextBlock
            var nameDataPart = itemDataParts[0].Replace("\r\n", "`").Split('`');

            Name = nameDataPart[1];
            BaseType = nameDataPart[2];

            ItemAffixPart = itemDataParts[affixPartIndex];
            
            //parse ClassType
            var secondDataBlock = itemDataParts[2].Replace("\r\n", "`").Split('`');
            var firstDataStatLine = secondDataBlock[0];
            ParseItemClassType(firstDataStatLine);

            return ParseResult.Success;
        }



        private static int ParseLinks(string itemDataText)
        {
            if (!itemDataText.Contains("Sockets:"))
                return 0;
            if (new Regex(@"Sockets: .*.-.-.-.-.-.", RegexOptions.Compiled).Match(itemDataText).Success)
                return 6;
            if (new Regex(@"Sockets: .*.-.-.-.-.", RegexOptions.Compiled).Match(itemDataText).Success)
                return 5;
            if (new Regex(@"Sockets: .*.-.-.-.", RegexOptions.Compiled).Match(itemDataText).Success)
                return 4;
            if (new Regex(@"Sockets: .*.-.-.", RegexOptions.Compiled).Match(itemDataText).Success)
                return 3;
            if (new Regex(@"Sockets: .*.-.", RegexOptions.Compiled).Match(itemDataText).Success)
                return 2;
            return 0;
        }

        private bool ParseItemClassType(string firstDataStatLine)
        {
            
            /*if (!firstDataStatLine.Contains(":"))
            {
                var weaponparseRegEx = new Regex(@"(?'weaponClass'One Handed Axe|One Handed Mace|One Handed Sword|Wand|Dagger|Claw)$", RegexOptions.Compiled);
                var weaponmatch = weaponparseRegEx.Match(firstDataStatLine);


                
                if (weaponmatch.Success)
                {
                    baseItems.SetWeaponsBaseProperties(BaseType, out BaseDamageLo, out BaseDamageHi, out BaseCC, out BaseAPS);
                    baseItems.DumpToConsole();
                    GripType = "1h";
                    ClassType = weaponmatch.Groups["weaponClass"].Value;
                    return true;
                }

                weaponparseRegEx = new Regex(@"(?'weaponClass'Two Handed Axe|Two Handed Mace|Bow|Two Handed Sword|Staff)$", RegexOptions.Compiled);
                weaponmatch = weaponparseRegEx.Match(firstDataStatLine);
                
                if (weaponmatch.Success)
                {
                    baseItems.SetWeaponsBaseProperties(BaseType, out BaseDamageLo, out BaseDamageHi, out BaseCC, out BaseAPS);
                    GripType = "2h";
                    ClassType = weaponmatch.Groups["weaponClass"].Value;
                    return true;
                }
                Console.WriteLine($"[Item.ParseItemClassType] Unknown WeaponType - BaseTypeNmae : {BaseType}  FirstStatLine: {firstDataStatLine}");
                return false;
            }


            if (firstDataStatLine.Contains("Map"))
            {
                ClassType = "Map";
                return true;
            }
            if (BaseType.Contains("Quiver"))
            {
                ClassType = "Quiver";
                return true;
            }
            if (BaseType.Contains("Jewel"))
            {
                ClassType = "Jewel";
                return true;
            }
            if (BaseType.Contains("Amulet"))
            {
                ClassType = "Amulet";
                return true;
            }
            if (BaseType.Contains("Talisman"))
            {
                ClassType = "Talisman";
                return true;
            }

            var parseRegEx = new Regex(@"Ring$", RegexOptions.Compiled);

            if (parseRegEx.Match(BaseType).Success)
            {
                ClassType = "Ring";
                return true;
            }

            parseRegEx = new Regex(@"(Belt|Sash)$", RegexOptions.Compiled);

            if (parseRegEx.Match(BaseType).Success)
            {
                ClassType = "Belt";
                return true;
            }

            parseRegEx = new Regex(@"(Shield|Bundle|Buckler)$", RegexOptions.Compiled);

            if (parseRegEx.Match(BaseType).Success)
            {
                var match = parseRegEx.Match(BaseType);
                ClassType = "Shield";
                baseItems.SetArmourBaseProperties(BaseType, out BaseAR, out BaseEV, out BaseES);
                return true;
            }

            parseRegEx = new Regex(@"(Boots|Greaves|Shoes|Slippers)$", RegexOptions.Compiled);

            if (parseRegEx.Match(BaseType).Success)
            {
                var match = parseRegEx.Match(BaseType);
                ClassType = "Boots";
                baseItems.SetArmourBaseProperties(BaseType, out BaseAR, out BaseEV, out BaseES);
                return true;
            }

            parseRegEx = new Regex(@" (Hat|Helm|Bascinet|Burgonet|Cap|Tricorne|Hood|Pelt|Circlet|Cage|Sallet|Coif|Crown|Mask)$", RegexOptions.Compiled);

            if (parseRegEx.Match(BaseType).Success)
            {
                var match = parseRegEx.Match(BaseType);
                baseItems.SetArmourBaseProperties(BaseType, out BaseAR, out BaseEV, out BaseES);
                ClassType = "Helm";
                return true;
            }

            parseRegEx = new Regex(@"(Gauntlets|Gloves|Mitts)$", RegexOptions.Compiled);

            if (parseRegEx.Match(BaseType).Success)
            {
                var match = parseRegEx.Match(BaseType);
                ClassType = "Gloves";
                //baseItems.SetArmourBaseProperties(BaseType, out BaseAR, out BaseEV, out BaseES);
                return true;
            }

            parseRegEx = new Regex(@"(Gauntlets|Gloves|Mitts)$", RegexOptions.Compiled);

            if (parseRegEx.Match(BaseType).Success)
            {
                var match = parseRegEx.Match(BaseType);
                ClassType = "Gloves";
                //baseItems.SetArmourBaseProperties(BaseType, out BaseAR, out BaseEV, out BaseES);
                return true;
            }

            if (baseItems.SetArmourBaseProperties(BaseType, out BaseAR, out BaseEV, out BaseES))
            {
                ClassType = "BodyArmour";
                return true;
            }*/

            //Console.WriteLine($"[Item.ParseItemClassType] Unknown ItemType  BaseTypeName : {BaseType}  DataBlock-FirstLine: {firstDataStatLine}");
            return false;
        }
    }
}