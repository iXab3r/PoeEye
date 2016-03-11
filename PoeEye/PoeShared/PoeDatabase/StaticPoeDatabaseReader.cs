namespace PoeShared.PoeDatabase
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using Scaffolding;

    internal sealed class StaticPoeDatabaseReader : IPoeDatabaseReader
    {
        private readonly ISet<string> knownEntities = new HashSet<string>();

        public StaticPoeDatabaseReader()
        {
            Initialize();
        }

        public string[] KnownEntitiesNames => knownEntities.ToArray();

        private void Initialize()
        {
            Log.Instance.Debug($"[StaticPoeDatabaseReader] Loading database...");

            knownEntities.Clear();
            var resources = new HashSet<string>
            {
                "PoeTradeUniques.lst",
            };

            foreach (var resource in resources)
            {
                var resourcePath = $"{nameof(PoeDatabase)}.{resource}";
                var resourceData = ResourceReader.ReadResourceAsString(Assembly.GetExecutingAssembly(), resourcePath);

                if (string.IsNullOrEmpty(resourceData))
                {
                    continue;
                }

                var items = resourceData.SplitTrim("\n");

                foreach (var item in items)
                {
                    knownEntities.Add(item);
                }
            }

            Log.Instance.Debug($"[StaticPoeDatabaseReader] Loaded {knownEntities.Count} entries");
        }
    }
}