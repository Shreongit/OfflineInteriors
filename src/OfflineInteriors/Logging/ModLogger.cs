using System;
using System.IO;
using System.Text;
using GTA;

namespace OfflineInteriors.Logging
{
    public static class ModLogger
    {
        private static readonly object SyncRoot = new object();

        private static readonly string LogDirectory =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OfflineInteriors");

        private static readonly string LogFile =
            Path.Combine(LogDirectory, "OfflineInteriors.log");

        public static void Info(string message)
        {
            Write("INFO", message, null);
        }

        public static void Warning(string message)
        {
            Write("WARN", message, null);
        }

        public static void Error(string message, Exception exception = null)
        {
            Write("ERROR", message, exception);
        }

        private static void Write(
            string level,
            string message,
            Exception exception)
        {
            string line = string.Format(
                "[{0:yyyy-MM-dd HH:mm:ss}] [{1}] {2}",
                DateTime.Now,
                level,
                message ?? string.Empty);

            if (exception != null)
            {
                line += Environment.NewLine + exception;
            }

            try
            {
                lock (SyncRoot)
                {
                    Directory.CreateDirectory(LogDirectory);

                    File.AppendAllText(
                        LogFile,
                        line + Environment.NewLine,
                        Encoding.UTF8);
                }
            }
            catch (IOException)
            {
                // Logging must never crash the game script.
            }
            catch (UnauthorizedAccessException)
            {
                // Continue running if the log directory is not writable.
            }
        }
    }
}