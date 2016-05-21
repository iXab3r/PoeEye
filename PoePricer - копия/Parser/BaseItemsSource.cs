using PoePricer.Parser;
using System;
using System.Configuration;
using System.Diagnostics.Eventing.Reader;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters;
using System.Security.Cryptography.X509Certificates;
using System.Security.Policy;
using System.Threading.Tasks;


namespace PoePricer.Parser
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Extensions;


    public enum BaseItemTypes
    {
        BodyArmour,
        Boots,
        Gloves,
        Helmets,
        Shields,
        Weapon
    }


    public struct BaseItemProperties
    {
        public string BaseName { get; set; }

        public int BaseDamageLo { get; set; }

        public int BaseDamageHi { get; set; }

        public double BaseCC { get; set; }

        public double BaseAPS { get; set; }

        public int BaseAR { get; set; }

        public int BaseEV { get; set; }

        public int BaseES { get; set; }

    }


    public class BaseItemsSource : PricerDataReader
    {
        public bool IsWeaponBase { get; set; }

        public BaseItemProperties[] Items { get; set; }

        public BaseItemsSource(string fileName) : base(Path.Combine("Bases", fileName))
        {
            IsWeaponBase = (FileName == "Weapon") ? true : false;
            Items = Read(FileName);
        }

        private BaseItemProperties[] Read(string fileName)
        {

            var result = new List<BaseItemProperties>();

            var lines = this.RawLines;


            var parseRegexBaseItems = IsWeaponBase
                ? new Regex(
                    @"^(?'baseName'[A-Za-z ']+)\t+(?'damageLo'\d+)\t+(?'damageHi'\d+)\t+(?'baseCC'[0-9,]+)%\t+(?'baseAPS'[0-9,]+)[ ]*$", RegexOptions.Compiled)
                : new Regex(
                    @"^(?'baseName'[A-Za-z ']+)\t+(?'baseAR'\d*)\t*(?'baseEV'\d*)\t*(?'baseES'\d*)[ ]*$",RegexOptions.Compiled);



            foreach (var match in lines.Select(line => parseRegexBaseItems.Match(line)).Where(match => match.Success))
            {
                var item = new BaseItemProperties
                {
                    BaseName = match.Groups["baseName"].Value,
                    BaseDamageLo = match.Groups["damageLo"].Success ? match.Groups["damageLo"].Value.ToInt() : 0,
                    BaseDamageHi = match.Groups["damageHi"].Success ? match.Groups["damageHi"].Value.ToInt() : 0,
                    BaseCC = match.Groups["baseCC"].Success ? Convert.ToDouble(match.Groups["baseCC"].Value) : 0,
                    BaseAPS = match.Groups["baseAPS"].Success ? Convert.ToDouble(match.Groups["baseAPS"].Value) : 0,
                    BaseAR = (match.Groups["baseAR"].Success && match.Groups["baseAR"].Value != "") ? match.Groups["baseAR"].Value.ToInt() : 0,
                    BaseEV = (match.Groups["baseEV"].Success && match.Groups["baseEV"].Value != "") ? match.Groups["baseEV"].Value.ToInt() : 0,
                    BaseES = (match.Groups["baseES"].Success && match.Groups["baseES"].Value != "") ? match.Groups["baseES"].Value.ToInt() : 0,
                };

                result.Add(item);
            }
            
            return result.ToArray();
        }

        public void SetWeaponsBaseProperties(string baseTypeName, out int baseDamageLo, out int baseDamageHi, out double baseCC, out double baseAPS)
        {
            baseDamageLo = 0;
            baseDamageHi = 0;
            baseCC = 0;
            baseAPS = 0;
            foreach (var match in Items.Where(match => match.BaseName == baseTypeName))
            {
                baseDamageLo = match.BaseDamageLo;
                baseDamageHi = match.BaseDamageHi;
                baseCC = match.BaseCC;
                baseAPS = match.BaseAPS;
                return;
            }
            Console.WriteLine($"[BaseItems.SetWeaponBaseProperties] Wrong baseType : [{baseTypeName}]");
        }

        public bool SetArmourBaseProperties(string baseTypeName, out int baseAR, out int baseEV, out int baseES)
        {
            baseAR = 0;
            baseEV = 0;
            baseES = 0;
            foreach (var match in Items.Where(match => match.BaseName == baseTypeName))
            {
                baseAR = match.BaseAR;
                baseEV = match.BaseEV;
                baseES = match.BaseES;
                return true;
            }
            Console.WriteLine($"[BaseItems.SetArmourBaseProperties] Wrong baseType : [{baseTypeName}]");
            return false;
        }
    }
}