namespace PoeEyeUi.Config
{
    using System.IO;
    using System.Linq;
    using System.Reflection;

    using Newtonsoft.Json;

    using PoeShared;

    internal sealed class PoeEyeConfig : IPoeEyeConfig
    {
        private PoeEyeTabConfig[] tabConfigs = new PoeEyeTabConfig[0];

        public PoeEyeTabConfig[] TabConfigs
        {
            get { return tabConfigs; }
            set { tabConfigs = value ?? new PoeEyeTabConfig[0]; }
        }
    }
}