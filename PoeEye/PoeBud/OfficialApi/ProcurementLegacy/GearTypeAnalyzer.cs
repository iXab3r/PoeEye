using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Windows;
using System.Xml;
using Guards;
using PoeBud.OfficialApi.DataTypes;
using PoeShared.Scaffolding;

namespace PoeBud.OfficialApi.ProcurementLegacy
{
    internal class GearTypeAnalyzer : IGearTypeAnalyzer
    {
        private static readonly IDictionary<GearType, IEnumerable<string>> GearBaseTypes = new Dictionary<GearType, IEnumerable<string>>();

        private readonly IEnumerable<GearTypeRunner> runners;

        public GearTypeAnalyzer()
        {
            GearBaseTypes.Clear();
            var resources = new HashSet<string>
            {
                "Data.xml",
            };

            var assembly = Assembly.GetExecutingAssembly();
            foreach (string resource in resources)
            {
                var resourcePath = $"{nameof(OfficialApi)}.{nameof(ProcurementLegacy)}.{resource}";
                var resourceData = assembly.ReadResourceAsString(resourcePath);

                if (string.IsNullOrWhiteSpace(resourceData))
                {
                    continue;
                }

                var doc = new XmlDocument();
                doc.LoadXml(resourceData);

                // Get entity nodes
                var entityNodes = doc.SelectNodes("Data/GearBaseTypes/GearBaseType");
                if (entityNodes == null) continue;

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
            }

            runners = new GearTypeRunner[]
            {
                new HelmetRunner(),
                new RingRunner(),
                new AmuletRunner(),
                new BeltRunner(),
                new FlaskRunner(),
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
                new MapRunner(),
                new DivinationCardRunner(),
                new JewelRunner(),
                new ChestRunner() //Must always be last!
            };
        }

        public GearType Resolve(string itemType)
        {
            Guard.ArgumentNotNull(() => itemType);

            var query = from runner in runners
                        let compatibleType = new { TypeName = runner.FindCompatibleType(itemType), GearType = runner.Type }
                        where !string.IsNullOrWhiteSpace(compatibleType.TypeName)
                        orderby compatibleType.TypeName.Length
                        select runner.Type;

            return query.LastOrDefault();
        }

        internal abstract class GearTypeRunner
        {
            protected GearTypeRunner(GearType gearType)
            {
                Type = gearType;
            }

            public GearType Type { get; set; }

            public abstract string FindCompatibleType(string item);

            public abstract string GetBaseType(string item);
        }

        internal class GearTypeRunnerBase : GearTypeRunner
        {
            protected List<string> compatibleTypes;
            protected List<string> generalTypes;
            protected List<string> incompatibleTypes;

            public GearTypeRunnerBase(GearType gearType)
                : base(gearType)
            {
                generalTypes = new List<string>();
                compatibleTypes = GearBaseTypes.ContainsKey(gearType)
                    ? GearBaseTypes[gearType].ToList()
                    : new List<string>();
                incompatibleTypes = new List<string>();
            }

            public override string FindCompatibleType(string itemName)
            {
                // First, check the general types, to see if there is an easy match.
                foreach (var typeName in generalTypes)
                {
                    if (itemName.Contains(typeName))
                    {
                        return typeName;
                    }
                }

                // Second, check all known types.
                foreach (var typeName in compatibleTypes)
                {
                    if (itemName.Contains(typeName))
                    {
                        return typeName;
                    }
                }
                return null;
            }

            public override string GetBaseType(string itemType)
            {
                if (incompatibleTypes != null && incompatibleTypes.Any(itemType.Contains))
                {
                    return null;
                }

                return compatibleTypes.FirstOrDefault(itemType.Contains);
            }
        }

        internal class RingRunner : GearTypeRunnerBase
        {
            public RingRunner()
                : base(GearType.Ring)
            {
                incompatibleTypes = new List<string> { "Ringmail" };
            }

            public override string FindCompatibleType(string itemName)
            {
                if (itemName.Contains("Ring") && !incompatibleTypes.Any(itemName.Contains))
                {
                    return itemName;
                }

                return null;
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
                generalTypes.AddRange(new List<string> { "Axe", "Chopper", "Splitter", "Labrys", "Tomahawk", "Hatchet", "Poleaxe", "Woodsplitter", "Cleaver" });
            }
        }

        internal class ClawRunner : GearTypeRunnerBase
        {
            public ClawRunner()
                : base(GearType.Claw)
            {
                generalTypes.AddRange(new List<string> { "Fist", "Awl", "Paw", "Blinder", "Ripper", "Stabber", "Claw", "Gouger" });
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
                generalTypes.AddRange(new List<string> { "Dagger", "Shank", "Knife", "Stiletto", "Skean", "Poignard", "Ambusher", "Boot Blade", "Kris" });
            }
        }

        internal class MaceRunner : GearTypeRunnerBase
        {
            public MaceRunner()
                : base(GearType.Mace)
            {
                generalTypes.AddRange(
                    new List<string> { "Club", "Tenderizer", "Mace", "Hammer", "Maul", "Mallet", "Breaker", "Gavel", "Pernarch", "Steelhead", "Piledriver", "Bladed Mace" });
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
                generalTypes.Add("Gnarled Branch");
                generalTypes.Add("Quarterstaff");
                generalTypes.Add("Lathi");
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
                        "sword",
                        "Sabre",
                        "Dusk Blade",
                        "Cutlass",
                        "Baselard",
                        "Gladius",
                        "Variscite Blade",
                        "Vaal Blade",
                        "Midnight Blade",
                        "Corroded Blade",
                        "Highland Blade",
                        "Ezomyte Blade",
                        "Rusted Spike",
                        "Rapier",
                        "Foil",
                        "Pecoraro",
                        "Estoc",
                        "Twilight Blade"
                    });
            }
        }

        internal class ShieldRunner : GearTypeRunnerBase
        {
            public ShieldRunner()
                : base(GearType.Shield)
            {
                generalTypes.Add("Shield");
                generalTypes.Add("Spiked Bundle");
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