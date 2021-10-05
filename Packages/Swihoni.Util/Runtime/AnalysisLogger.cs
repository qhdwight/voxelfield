using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Swihoni.Util
{
    public static class AnalysisLogger
    {
        private class PrefixLogger
        {
            public string fileName;
            public readonly StringBuilder builder = new(WriteSize);

            public void Flush()
            {
                if (builder.Length == 0) return;
                File.AppendAllText(fileName, builder.ToString());
                builder.Clear();
            }
        }

        private const int WriteSize = 5000;

        private static readonly Dictionary<string, PrefixLogger> Loggers = new();

        [Conditional("UNITY_EDITOR")]
        public static void Reset(string prefix) => File.WriteAllText(GetFileName(prefix), string.Empty);

        [Conditional("UNITY_EDITOR")]
        public static void FlushAll()
        {
            foreach (PrefixLogger logger in Loggers.Values)
                logger.Flush();
        }

        [Conditional("UNITY_EDITOR")]
        public static void AddDataPoint(string prefix, params object[] values)
        {
            Loggers.TryGetValue(prefix, out PrefixLogger logger);
            if (logger == null)
            {
                logger = new PrefixLogger {fileName = GetFileName(prefix)};
                Loggers.Add(prefix, logger);
            }
            logger.builder.Append(string.Join(",", values)).AppendLine();
            if (logger.builder.Length > WriteSize) logger.Flush();
        }

        private static string GetFileName(string prefix) => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"{prefix}Output.csv");
    }
}