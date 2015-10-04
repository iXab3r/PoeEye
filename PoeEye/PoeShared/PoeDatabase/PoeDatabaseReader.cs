namespace PoeShared.PoeDatabase
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Xml;

    using Resourcer;

    public sealed class PoeDatabaseReader : IPoeDatabaseReader
    {
        private readonly HashSet<PoeDatabaseEntity> knownEntities = new HashSet<PoeDatabaseEntity>();

        public PoeDatabaseReader()
        {
            Initialize();

            KnownEntitiesNames = knownEntities.Select(x => x.Name).ToArray();
        }

        private void Initialize()
        {
            Log.Instance.Debug($"[PoeDatabaseReader] Loading database...");

            knownEntities.Clear();
            var resources = new HashSet<string>
            {
                "currency.xml",
                "gems.xml",
                "divcards.xml",
                "unique_maps.xml",
                "unique_jewels.xml",
                "unique_flasks.xml",
                "unique_body_armours.xml",
                "unique_helmets.xml",
                "unique_gloves.xml",
                "unique_boots.xml",
                "unique_amulets.xml",
                "unique_belts.xml",
                "unique_rings.xml",
                "unique_quivers.xml",
                "unique_shields.xml",
                "unique_axes.xml",
                "unique_bows.xml",
                "unique_claws.xml",
                "unique_daggers.xml",
                "unique_rods.xml",
                "unique_maces.xml",
                "unique_swords.xml",
                "unique_staves.xml",
                "unique_wands.xml",
            };

            foreach (string resource in resources)
            {
                var resourcePath = $"{nameof(PoeDatabase)}.{resource}";
                var resourceData = ReadResourceAsString(resourcePath);

                if (string.IsNullOrWhiteSpace(resourceData))
                {
                    continue;
                }

                var doc = new XmlDocument();
                doc.LoadXml(resourceData);

                // Get entity nodes
                var entityNodes = doc.SelectNodes("Root/Entity");
                if (entityNodes == null) continue;

                // Loop through nodes
                foreach (XmlNode node in entityNodes)
                {
                    if (node.Attributes == null) continue;

                    // Get entity type
                    var entity = new PoeDatabaseEntity();
                    entity.Deserialize(node);

                    if (string.IsNullOrWhiteSpace(entity.Name))
                    {
                        continue;
                    }

                    // Enlist entity
                    knownEntities.Add(entity);
                }
            }

            Log.Instance.Debug($"[PoeDatabaseReader] Loaded {knownEntities.Count} entries");
        }

        public string[] KnownEntitiesNames { get; }

        private string ReadResourceAsString(string path)
        {
            var assembly = Assembly.GetExecutingAssembly();

            var assemblyName = assembly.GetName().Name;
            var resourcePath = $"{assemblyName}.{path}";
            using (var stream = assembly.GetManifestResourceStream(resourcePath))
            using (var streamReader = new StreamReader(stream))
            {
                return streamReader.ReadToEnd();
            }
        }
    }
}