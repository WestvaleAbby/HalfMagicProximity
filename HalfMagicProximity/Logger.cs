namespace HalfMagicProximity
{
    public enum Severity
    {
        Info,
        Warn,
        Error,
        Debug,
        Prox,
    };

    static class Logger
    {
        private const ConsoleColor INFO_COLOR = ConsoleColor.White;
        private const ConsoleColor WARN_COLOR = ConsoleColor.Magenta;
        private const ConsoleColor ERROR_COLOR = ConsoleColor.Red;
        private const ConsoleColor DEBUG_COLOR = ConsoleColor.DarkGray;
        private const ConsoleColor PROXIMITY_COLOR = ConsoleColor.Cyan;
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
        /// <param name="source">The source of the log message.</param>
        /// <param name="message">The message to output to the console</param>
        public static void Log(Severity severity, string source, string message)
        {
            // Filter out debug messages if they're not being logged
            if (severity == Severity.Debug && !IsDebugEnabled) return;

            EnableSeverityColors(severity);

            // Most Proximity logs will already have the severity and source included, so we don't need to output it again
            if (severity == Severity.Prox && message.Contains("[Proximity]"))
            {
                Console.WriteLine(message);
            }
            else
            {
                Console.WriteLine($"{severity,-5} [{source}] {message}");
            }
            
            // ARGTODO: Output logging message to a log file

            RestoreDefaultColors();
        }

        /// <summary>
        /// Log a message at the Info severity. Text is White
        /// </summary>
        /// <param name="source">The source of the log message.</param>
        /// <param name="message">The message to output to the console</param>
        public static void Info(string source, string message)
        {
            Log(Severity.Info, source, message);
        }

        /// <summary>
        /// Log a message at the Warn severity. Text is Magenta
        /// </summary>
        /// <param name="source">The source of the log message.</param>
        /// <param name="message">The message to output to the console</param>
        public static void Warn(string source, string message)
        {
            Log(Severity.Warn, source, message);
        }

        /// <summary>
        /// Log a message at the Error severity. Text is Red
        /// </summary>
        /// <param name="source">The source of the log message.</param>
        /// <param name="message">The message to output to the console</param>
        public static void Error(string source, string message)
        {
            Log(Severity.Error, source, message);
        }

        /// <summary>
        /// Log a message at the Debug severity. Text is Grey.
        /// Only logged if IsDebugEnabled = true 
        /// </summary>
        /// <param name="source">The source of the log message.</param>
        /// <param name="message">The message to output to the console</param>
        public static void Debug(string source, string message)
        {
            // Filter out debug messages if they're not being logged
            if (!IsDebugEnabled) return;

            Log(Severity.Debug, source, message);
        }

        /// <summary>
        /// Log a message at the Proximity severity. Text is Cyan.
        /// </summary>
        /// <param name="source">The source of the log message.</param>
        /// <param name="message">The message to output to the console</param>
        public static void Proximity(string source, string message)
        {
            Log(Severity.Prox, source, message);
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
                case Severity.Prox:
                    Console.ForegroundColor = PROXIMITY_COLOR;
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
