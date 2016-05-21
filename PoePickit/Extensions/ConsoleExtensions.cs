namespace PoePickit.Extensions
{
    using System;

    internal static class ConsoleExtensions
    {
        public static void WriteLine(string message, ConsoleColor textColor)
        {
            Console.ForegroundColor = textColor;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
}