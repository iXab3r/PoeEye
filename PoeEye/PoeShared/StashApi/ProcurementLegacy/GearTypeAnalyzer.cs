using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using Common.Logging;
using Guards;
using PoeShared.Scaffolding;
using PoeShared.StashApi.DataTypes;

namespace PoeShared.StashApi.ProcurementLegacy
{
    internal class GearTypeAnalyzer : IGearTypeAnalyzer, IItemTypeAnalyzer
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(GearTypeAnalyzer));

        private static readonly IDictionary<GearType, IEnumerable<string>> GearBaseTypes = new Dictionary<GearType, IEnumerable<string>>();

        private static readonly ConcurrentDictionary<string, Regex> cachedRegexes = new ConcurrentDictionary<string, Regex>();

        private readonly IEnumerable<GearTypeRunner> runners;

        public GearTypeAnalyzer()
        {
            GearBaseTypes.Clear();
            var resources = new HashSet<string>
            {
                "Data.xml"
            };

            var assembly = Assembly.GetExecutingAssembly();
            foreach (var resource in resources)
            {
                var resourcePath = $"{nameof(StashApi)}.{nameof(ProcurementLegacy)}.{resource}";
                var resourceData = assembly.ReadResourceAsString(resourcePath);

                if (string.IsNullOrWhiteSpace(resourceData))
                {
                    continue;
                }

                var doc = new XmlDocument();
                doc.LoadXml(resourceData);

                // Get entity nodes
                var entityNodes = doc.SelectNodes("Data/GearBaseTypes/GearBaseType");
                if (entityNodes == null)
                {
                    continue;
                }

                foreach (XmlNode gearBaseTypesNodes in entityNodes)
                {
                    if (gearBaseTypesNodes.Attributes == null)
                    {
                        continue;
                    }

                    GearType gearType;
                    var gearTypeString = gearBaseTypesNodes.Attributes["name"]?.InnerText;
                    if (!Enum.TryParse(gearTypeString, out gearType))
                    {
                        continue;
                    }

                    GearBaseTypes[gearType] = gearBaseTypesNodes
                                              .SelectNodes("Item")
                                              ?.OfType<XmlNode>()
                                              .Where(x => x.Attributes != null)
                                              .Select(x => x.Attributes["name"]?.InnerText)
                                              .Where(itemName => !string.IsNullOrWhiteSpace(itemName))
                                              .ToArray();
                }

                Log.Debug($"[GearTypeAnalyzer] Loaded the following ItemTypes:\n\t{GearBaseTypes.DumpToTable()}");
            }

            runners = new GearTypeRunner[]
            {
                new HelmetRunner(),
                new RingRunner(),
                new AmuletRunner(),
                new BeltRunner(),
                new GloveRunner(),
                new BootRunner(),
                new AxeRunner(),
                new ClawRunner(),
                new BowRunner(),
                new DaggerRunner(),
                new ShieldRunner(),
                new MaceRunner(),
                new QuiverRunner(),
                new SceptreRunner(),
                new StaffRunner(),
                new SwordRunner(),
                new WandRunner(),
                new JewelRunner(),
                new ChestRunner(),
                new FlaskRunner(),
                new MapRunner(),
                new DivinationCardRunner()
            };
        }

        public GearType Resolve(string itemType)
        {
            Guard.ArgumentNotNull(itemType, nameof(itemType));

            var query = from runner in runners
                        let compatibleType = new {TypeName = runner.FindCompatibleType(itemType), GearType = runner.Type}
                        where !string.IsNullOrWhiteSpace(compatibleType.TypeName)
                        orderby compatibleType.TypeName.Length
                        select runner.Type;

            return query.LastOrDefault();
        }

        public ItemTypeInfo ResolveTypeInfo(string itemNameText)
        {
            var query = from runner in runners
                        let compatibleType = new {TypeName = runner.FindCompatibleType(itemNameText)}
                        where !string.IsNullOrWhiteSpace(compatibleType.TypeName)
                        select new {GearType = runner.Type, ItemType = compatibleType.TypeName};
            var options = query.ToArray();
            Log.Debug(options.DumpToTable());

            var itemInfo = options.FirstOrDefault();

            var resultingName = itemNameText;
            var itemType = itemInfo?.ItemType;

            resultingName = resultingName.Replace("Superior", "");
            if (itemType != null && resultingName.EndsWith(itemType))
            {
                resultingName = resultingName.Replace(itemType, string.Empty);
            }

            resultingName = resultingName.Trim();

            return new ItemTypeInfo
            {
                ItemName = resultingName,
                ItemType = itemType,
                GearType = itemInfo?.GearType ?? GearType.Unknown
            };
        }

        private static bool ContainsWord(string source, string word)
        {
            var regex = cachedRegexes.GetOrAdd(word, x => new Regex($@"(?<=^|\s){word}(?=$|\s)", RegexOptions.Compiled | RegexOptions.IgnoreCase));

            return regex.IsMatch(source);
        }

        internal abstract class GearTypeRunner
        {
            protected GearTypeRunner(GearType gearType)
            {
                Type = gearType;
            }

            public GearType Type { get; }

            public abstract string FindCompatibleType(string item);
        }

        internal class GearTypeRunnerBase : GearTypeRunner
        {
            protected List<string> compatibleTypes;
            protected List<string> generalTypes;

            public GearTypeRunnerBase(GearType gearType)
                : base(gearType)
            {
                generalTypes = new List<string>();
                compatibleTypes = GearBaseTypes.ContainsKey(gearType)
                    ? GearBaseTypes[gearType].ToList()
                    : new List<string>();
            }

            public override string FindCompatibleType(string itemName)
            {
                // check all known types.
                foreach (var typeName in compatibleTypes)
                {
                    if (ContainsWord(itemName, typeName))
                    {
                        return typeName;
                    }
                }

                // check the general types, to see if there is an easy match.
                foreach (var typeName in generalTypes)
                {
                    if (ContainsWord(itemName, typeName))
                    {
                        return typeName;
                    }
                }

                return null;
            }
        }

        internal class RingRunner : GearTypeRunnerBase
        {
            public RingRunner()
                : base(GearType.Ring)
            {
            }
        }

        internal class AmuletRunner : GearTypeRunnerBase
        {
            public AmuletRunner()
                : base(GearType.Amulet)
            {
                generalTypes.Add("Amulet");
            }
        }

        internal class HelmetRunner : GearTypeRunnerBase
        {
            public HelmetRunner()
                : base(GearType.Helmet)
            {
                generalTypes.AddRange(
                    new List<string>
                    {
                        "Helmet",
                        "Circlet",
                        "Cap",
                        "Mask",
                        "Chain Coif",
                        "Casque",
                        "Hood",
                        "Ringmail Coif",
                        "Chainmail Coif",
                        "Ring Coif",
                        "Crown",
                        "Burgonet",
                        "Bascinet",
                        "Pelt"
                    });
            }
        }

        internal class ChestRunner : GearTypeRunnerBase
        {
            public ChestRunner()
                : base(GearType.Chest)
            {
            }
        }

        internal class BeltRunner : GearTypeRunnerBase
        {
            public BeltRunner()
                : base(GearType.Belt)
            {
                generalTypes.Add("Belt");
                generalTypes.Add("Sash");
                generalTypes.Add("Stygian Vise");
            }
        }

        internal class FlaskRunner : GearTypeRunnerBase
        {
            public FlaskRunner()
                : base(GearType.Flask)
            {
                generalTypes.Add("Flask");
            }
        }

        internal class MapRunner : GearTypeRunnerBase
        {
            public MapRunner()
                : base(GearType.Map)
            {
                generalTypes.Add("Map");
            }
        }

        internal class DivinationCardRunner : GearTypeRunnerBase
        {
            public DivinationCardRunner()
                : base(GearType.DivinationCard)
            {
            }
        }

        internal class JewelRunner : GearTypeRunnerBase
        {
            public JewelRunner()
                : base(GearType.Jewel)
            {
                generalTypes.Add("Jewel");
            }
        }

        internal class GloveRunner : GearTypeRunnerBase
        {
            public GloveRunner()
                : base(GearType.Gloves)
            {
                generalTypes.Add("Glove");
                generalTypes.Add("Mitts");
                generalTypes.Add("Gauntlets");
            }
        }

        internal class BootRunner : GearTypeRunnerBase
        {
            public BootRunner()
                : base(GearType.Boots)
            {
                generalTypes.Add("Greaves");
                generalTypes.Add("Slippers");
                generalTypes.Add("Boots");
                generalTypes.Add("Shoes");
            }
        }

        internal class AxeRunner : GearTypeRunnerBase
        {
            public AxeRunner()
                : base(GearType.Axe)
            {
                generalTypes.AddRange(new List<string>
                {
                    "Axe",
                    "Chopper",
                    "Splitter",
                    "Hatchet"
                });
            }
        }

        internal class ClawRunner : GearTypeRunnerBase
        {
            public ClawRunner()
                : base(GearType.Claw)
            {
                generalTypes.AddRange(new List<string>
                {
                    "Fist",
                    "Paw",
                    "Ripper",
                    "Stabber",
                    "Claw",
                    "Gouger"
                });
            }
        }

        internal class BowRunner : GearTypeRunnerBase
        {
            public BowRunner()
                : base(GearType.Bow)
            {
                generalTypes.Add("Bow");
            }
        }

        internal class DaggerRunner : GearTypeRunnerBase
        {
            public DaggerRunner()
                : base(GearType.Dagger)
            {
                generalTypes.AddRange(new List<string>
                {
                    "Dagger",
                    "Shank",
                    "Knife",
                    "Skean",
                    "Kris"
                });
            }
        }

        internal class MaceRunner : GearTypeRunnerBase
        {
            public MaceRunner()
                : base(GearType.Mace)
            {
                generalTypes.AddRange(
                    new List<string>
                    {
                        "Club",
                        "Mace",
                        "Hammer",
                        "Maul",
                        "Mallet"
                    });
            }
        }

        internal class QuiverRunner : GearTypeRunnerBase
        {
            public QuiverRunner()
                : base(GearType.Quiver)
            {
                generalTypes.Add("Quiver");
            }
        }

        internal class SceptreRunner : GearTypeRunnerBase
        {
            public SceptreRunner()
                : base(GearType.Sceptre)
            {
                generalTypes.Add("Sceptre");
                generalTypes.Add("Fetish");
                generalTypes.Add("Sekhem");
            }
        }

        internal class StaffRunner : GearTypeRunnerBase
        {
            public StaffRunner()
                : base(GearType.Staff)
            {
                generalTypes.Add("Staff");
            }
        }

        internal class SwordRunner : GearTypeRunnerBase
        {
            public SwordRunner()
                : base(GearType.Sword)
            {
                generalTypes.AddRange(
                    new List<string>
                    {
                        "Sword",
                        "Rapier",
                        "Foil"
                    });
            }
        }

        internal class ShieldRunner : GearTypeRunnerBase
        {
            public ShieldRunner()
                : base(GearType.Shield)
            {
                generalTypes.Add("Shield");
                generalTypes.Add("Buckler");
            }
        }

        internal class WandRunner : GearTypeRunnerBase
        {
            public WandRunner()
                : base(GearType.Wand)
            {
                generalTypes.Add("Wand");
                generalTypes.Add("Horn");
            }
        }
    }
}