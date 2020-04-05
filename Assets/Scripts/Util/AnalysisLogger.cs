using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Util
{
    public static class AnalysisLogger
    {
        private class PrefixLogger
        {
            public string fileName;
            public readonly StringBuilder builder = new StringBuilder(WriteSize);

            public void Flush()
            {
                if (builder.Length == 0) return;
                File.AppendAllText(fileName, builder.ToString());
                builder.Clear();
            }
        }

        private const int WriteSize = 5000;

        private static readonly Dictionary<string, PrefixLogger> Loggers = new Dictionary<string, PrefixLogger>();

        public static void Reset(string prefix)
        {
            File.WriteAllText(GetFileName(prefix), string.Empty);
        }

        public static void FlushAll()
        {
            foreach (PrefixLogger logger in Loggers.Values)
                logger.Flush();
        }

        public static void AddDataPoint(string prefix, params object[] values)
        {
            Loggers.TryGetValue(prefix, out PrefixLogger logger);
            if (logger == null)
            {
                logger = new PrefixLogger
                {
                    fileName = GetFileName(prefix)
                };
                Loggers.Add(prefix, logger);
            }
            logger.builder.Append(string.Join(",", values)).AppendLine();
            if (logger.builder.Length > WriteSize) logger.Flush();
        }

        private static string GetFileName(string prefix)
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"{prefix}Output.csv");
        }
    }
}