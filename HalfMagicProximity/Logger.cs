namespace HalfMagicProximity
{
    public enum Severity
    {
        Info,
        Warn,
        Error,
        Debug,
        Trace,
        Prox,
    };

    /// <summary>
    /// Logger takes all output from this program and formats it nicely and outputs it to console
    /// </summary>
    static class Logger
    {
        private const ConsoleColor InfoColor = ConsoleColor.DarkGreen;
        private const ConsoleColor WarnColor = ConsoleColor.Magenta;
        private const ConsoleColor ErrorColor = ConsoleColor.Red;
        private const ConsoleColor DebugColor = ConsoleColor.Gray;
        private const ConsoleColor TraceColor = ConsoleColor.DarkGray;
        private const ConsoleColor ProximityColor = ConsoleColor.Cyan;
        private const ConsoleColor BackgroundColor = ConsoleColor.Black;

        private static ConsoleColor storedForegroundColor;
        private static ConsoleColor storedBackroundColor;

        public static bool IsTraceEnabled { get; set; } = false;

        static Logger ()
        {
            storedForegroundColor = Console.ForegroundColor;
            storedBackroundColor = Console.BackgroundColor;
        }

        /// <summary>
        /// Logs a message to the console. Includes a date/time stamp and the severity, and changes the text color to indicate severity:
        /// yyyy-mm-dd hh:mm:ss|Severity|Message
        /// </summary>
        /// <param name="severity">The log severity. Trace logs are only reported if IsTraceEnabled = true</param>
        /// <param name="source">The source of the log message.</param>
        /// <param name="message">The message to output to the console</param>
        public static void Log(Severity severity, string source, string message)
        {
            // Filter out trace messages if they're not being logged
            if (severity == Severity.Trace && !IsTraceEnabled) return;

            // Get the time stamp of the message and format it properly
            string dateTimeStamp = String.Format("{0:s}", DateTime.Now).Replace('T', ' ');

            EnableSeverityColors(severity);

            string content = $"{dateTimeStamp} {severity,-5} [{source}] {message}";

            // Proximity logs will already have the severity and source included, so we don't need to output it again
            if (severity == Severity.Prox)
                content = $"{dateTimeStamp} {message}";

            Console.WriteLine(content);

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
        /// Log a message at the Debug severity. Text is Grey
        /// </summary>
        /// <param name="source">The source of the log message.</param>
        /// <param name="message">The message to output to the console</param>
        public static void Debug(string source, string message)
        {
            Log(Severity.Debug, source, message);
        }

        /// <summary>
        /// Log a message at the Trace severity. Text is Dark Grey.
        /// Only logged if IsTraceEnabled = true 
        /// </summary>
        /// <param name="source">The source of the log message.</param>
        /// <param name="message">The message to output to the console</param>
        public static void Trace(string source, string message)
        {
            // Filter out debug messages if they're not being logged
            if (!IsTraceEnabled) return;

            Log(Severity.Trace, source, message);
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
            Console.BackgroundColor = BackgroundColor;

            switch (severity)
            {
                case Severity.Info:
                    Console.ForegroundColor = InfoColor;
                    break;
                case Severity.Warn:
                    Console.ForegroundColor = WarnColor;
                    break;
                case Severity.Error:
                    Console.ForegroundColor = ErrorColor;
                    break;
                case Severity.Debug:
                    Console.ForegroundColor = DebugColor;
                    break;
                case Severity.Trace:
                    Console.ForegroundColor = TraceColor;
                    break;
                case Severity.Prox:
                    Console.ForegroundColor = ProximityColor;
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
