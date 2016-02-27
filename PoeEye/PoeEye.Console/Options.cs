namespace PoeEye.Console
{
    using CommandLine;

    internal sealed class Options
    {
        public enum ProgramMode
        {
            Live,
            Mock
        }

        [Option("mode", HelpText = "Program execution mode")]
        public ProgramMode Mode { get; set; }
    }
}