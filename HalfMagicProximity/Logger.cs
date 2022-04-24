namespace HalfMagicProximity
{
    public enum Severity
    {
        Info,
        Warn,
        Error,
        Debug,
    };

    static class Logger
    {
        private const ConsoleColor INFO_COLOR = ConsoleColor.White;
        private const ConsoleColor WARN_COLOR = ConsoleColor.Magenta;
        private const ConsoleColor ERROR_COLOR = ConsoleColor.Red;
        private const ConsoleColor DEBUG_COLOR = ConsoleColor.DarkGray;
        private const ConsoleColor BACK_COLOR = ConsoleColor.Black;

        private static ConsoleColor storedForegroundColor;
        private static ConsoleColor storedBackroundColor;

        public static bool IsDebugEnabled { get; set; } = false;

        static Logger ()
        {
            storedForegroundColor = Console.ForegroundColor;
            storedBackroundColor = Console.BackgroundColor;
        }

        /// <summary>
        /// Logs a message to the console. Includes a date/time stamp and the severity, and changes the text color to indicate severity:
        /// yyyy-mm-dd hh:mm:ss|Severity|Message
        /// </summary>
        /// <param name="severity">The log severity. Debug logs are only reported if IsDebugEnabled = true</param>
        /// <param name="message">The message to output to the console</param>
        public static void Log(Severity severity, string message)
        {
            // Filter out debug messages if they're not being logged
            if (severity == Severity.Debug && !IsDebugEnabled) return;

            // Get the time stamp of the message and format it properly
            string dateTimeStamp = String.Format("{0:s}", DateTime.Now).Replace('T', ' ');

            EnableSeverityColors(severity);

            Console.WriteLine($"{dateTimeStamp}|{severity, -5}|{message}");
            
            // ARGTODO: Output logging message to a log file

            RestoreDefaultColors();
        }

        /// <summary>
        /// Log a message at the Info severity. Text is White
        /// </summary>
        /// <param name="message">The message to output to the console</param>
        public static void Info(string message)
        {
            Log (Severity.Info, message);
        }

        /// <summary>
        /// Log a message at the Warn severity. Text is Magenta
        /// </summary>
        /// <param name="message">The message to output to the console</param>
        public static void Warn(string message)
        {
            Log (Severity.Warn, message);
        }

        /// <summary>
        /// Log a message at the Error severity. Text is Red
        /// </summary>
        /// <param name="message">The message to output to the console</param>
        public static void Error(string message)
        {
            Log (Severity.Error, message);
        }

        /// <summary>
        /// Log a message at the Debug severity. Text is Grey.
        /// Only logged if IsDebugEnabled = true 
        /// </summary>
        /// <param name="message">The message to output to the console</param>
        public static void Debug(string message)
        {
            Log (Severity.Debug, message);
        }

        private static void EnableSeverityColors(Severity severity)
        {
            Console.BackgroundColor = BACK_COLOR;

            switch (severity)
            {
                case Severity.Info:
                    Console.ForegroundColor = INFO_COLOR;
                    break;
                case Severity.Warn:
                    Console.ForegroundColor = WARN_COLOR;
                    break;
                case Severity.Error:
                    Console.ForegroundColor = ERROR_COLOR;
                    break;
                case Severity.Debug:
                    Console.ForegroundColor = DEBUG_COLOR;
                    break;
            }
        }

        private static void RestoreDefaultColors()
        {
            Console.ForegroundColor = storedForegroundColor;
            Console.BackgroundColor = storedBackroundColor;
        }
    }
}
