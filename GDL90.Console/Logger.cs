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
        }

        private void Log(string message, Verbosity level = Verbosity.Info, System.ConsoleColor logColor = System.ConsoleColor.White)
        {
            if (level <= LogLevel)
            {
                System.Console.ForegroundColor = logColor;
                System.Console.WriteLine(message);
                System.Console.ResetColor();
            }
        }

        public void Warn(string message)
        {
            Log($" WARN: {message}", Verbosity.Warn, System.ConsoleColor.Yellow);
        }

        public void Error(string message)
        {
            Log($"ERROR: {message}", Verbosity.Error, System.ConsoleColor.Red);
        }

        public void Info(string message)
        {
            Log($" INFO: {message}", Verbosity.Info, System.ConsoleColor.White);
        }

        public void Debug(string message)
        {
            Log($"DEBUG: {message}", Verbosity.Debug, System.ConsoleColor.Cyan);
        }
    }
}