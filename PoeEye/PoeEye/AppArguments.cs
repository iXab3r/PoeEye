using CommandLine;

namespace PoeEye
{
    public class AppArguments
    {
        [Option('d', "debugMode", DefaultValue=false)]
        public bool IsDebugMode { get; set; }
    }
}