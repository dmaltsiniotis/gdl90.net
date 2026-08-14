using System;

namespace GDL90.Logging
{
    public enum Verbosity : int
    {
        Silent = 0,
        Error = 1,
        Warn = 2,
        Info = 3,
        Debug = 4
    }

    public interface ILogger
    {
        Verbosity LogLevel { get; set; }

        void Debug(string message);
        void Error(string message);
        void Info(string message);
        void Warn(string message);
    }

    public class Logger : ILogger
    {
        public Verbosity LogLevel { get; set; } = Verbosity.Info;
        public Logger()
        {

        }
        public Logger(Verbosity logLevel)
        {
            LogLevel = logLevel;
            //Log($"Logger initialized with log level {LogLevel}.", LogLevel);
        }

        private void Log(string message, Verbosity level = Verbosity.Info, ConsoleColor logColor = ConsoleColor.White)
        {
            if (level <= LogLevel)
            {
                Console.ForegroundColor = logColor;
                Console.WriteLine(message);
                Console.ResetColor();
            }
        }

        public void Warn(string message)
        {
            Log($"WARN: {message}", Verbosity.Warn, ConsoleColor.Yellow);
        }

        public void Error(string message)
        {
            Log($"ERROR: {message}", Verbosity.Error, ConsoleColor.Red);
        }

        public void Info(string message)
        {
            Log($"INFO: {message}", Verbosity.Info, ConsoleColor.White);
        }

        public void Debug(string message)
        {
            Log($"DEBUG: {message}", Verbosity.Debug, ConsoleColor.Cyan);
        }
    }
}