﻿namespace PoeEye
{
    using CommandLine;

    internal sealed class Options
    {
        [Option("mode", HelpText = "Program execution mode")]
        public ProgramMode Mode { get; set; }

        public enum ProgramMode
        {
            Mock,
            Live
        }
    }
}