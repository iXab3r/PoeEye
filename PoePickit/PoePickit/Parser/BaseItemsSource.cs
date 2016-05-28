using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using PoePricer.Extensions;

namespace PoePricer.Parser
{
    internal class BaseItemsSource : PricerDataReader
    {
        public BaseItemsSource(string fileName) : base(Path.Combine("Bases", fileName))
        {
            IsWeaponBase = FileName == "Weapon" ? true : false;
            Items = Read(FileName);
        }

        public bool IsWeaponBase { get; set; }

        public BaseItemProperties[] Items { get; set; }

        private BaseItemProperties[] Read(string fileName)
        {
            var result = new List<BaseItemProperties>();

            var lines = RawLines;


            var parseRegexBaseItems = IsWeaponBase
                ? new Regex(
                    @"^(?'baseName'[A-Za-z ']+)\t+(?'damageLo'\d+)\t+(?'damageHi'\d+)\t+(?'baseCC'[0-9,]+)%\t+(?'baseAPS'[0-9,]+)[ ]*$",
                    RegexOptions.Compiled)
                : new Regex(
                    @"^(?'baseName'[A-Za-z ']+)\t(?'baseAR'\d*)\t{0,1}(?'baseEV'\d*)\t{0,1}(?'baseES'\d*)[ ]*$",
                    RegexOptions.Compiled);


            foreach (var match in lines.Select(line => parseRegexBaseItems.Match(line)).Where(match => match.Success))
            {
                var item = new BaseItemProperties
                {
                    BaseName = match.Groups["baseName"].Value,
                    BaseDamageLo = match.Groups["damageLo"].Success ? match.Groups["damageLo"].Value.ToInt() : 0,
                    BaseDamageHi = match.Groups["damageHi"].Success ? match.Groups["damageHi"].Value.ToInt() : 0,
                    BaseCC = match.Groups["baseCC"].Success ? match.Groups["baseCC"].Value.ToDouble() : 0,
                    BaseAPS = match.Groups["baseAPS"].Success ? match.Groups["baseAPS"].Value.ToDouble() : 0,
                    BaseAR =
                        match.Groups["baseAR"].Success && match.Groups["baseAR"].Value != ""
                            ? match.Groups["baseAR"].Value.ToInt()
                            : 0,
                    BaseEV =
                        match.Groups["baseEV"].Success && match.Groups["baseEV"].Value != ""
                            ? match.Groups["baseEV"].Value.ToInt()
                            : 0,
                    BaseES =
                        match.Groups["baseES"].Success && match.Groups["baseES"].Value != ""
                            ? match.Groups["baseES"].Value.ToInt()
                            : 0
                };

                result.Add(item);
            }

            return result.ToArray();
        }

        public void SetWeaponsBaseProperties(string baseTypeName, out int baseDamageLo, out int baseDamageHi,
            out double baseCC, out double baseAPS)
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