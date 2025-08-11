using System;
using System.IO;
using System.Linq;
using System.Text;
using AgentWrangler.Services;

namespace AgentWrangler.Services
{
    public static class Logger
    {
        private static readonly object _lock = new();
        private static string LogDirectory => Path.GetDirectoryName(ConfigHelper.GetConfigPath())!;
        private static string LogFilePrefix => "error_log_";
        private static int MaxLogFiles => 10;

        public static void LogError(Exception ex, string? context = null)
        {
            string logDir = LogDirectory;
            Directory.CreateDirectory(logDir);
            string logFile = Path.Combine(logDir, $"{LogFilePrefix}{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}.txt");
            var sb = new StringBuilder();
            sb.AppendLine($"Timestamp: {DateTime.UtcNow:O}");
            if (!string.IsNullOrEmpty(context))
                sb.AppendLine($"Context: {context}");
            sb.AppendLine($"Exception: {ex}");
            lock (_lock)
            {
                File.WriteAllText(logFile, sb.ToString());
                RollLogs(logDir);
            }
        }

        public static void LogError(string message, string? context = null)
        {
            string logDir = LogDirectory;
            Directory.CreateDirectory(logDir);
            string logFile = Path.Combine(logDir, $"{LogFilePrefix}{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}.txt");
            var sb = new StringBuilder();
            sb.AppendLine($"Timestamp: {DateTime.UtcNow:O}");
            if (!string.IsNullOrEmpty(context))
                sb.AppendLine($"Context: {context}");
            sb.AppendLine($"Error: {message}");
            lock (_lock)
            {
                File.WriteAllText(logFile, sb.ToString());
                RollLogs(logDir);
            }
        }

        private static void RollLogs(string logDir)
        {
            var files = Directory.GetFiles(logDir, $"{LogFilePrefix}*.txt")
                .OrderByDescending(f => f)
                .ToList();
            if (files.Count > MaxLogFiles)
            {
                foreach (var file in files.Skip(MaxLogFiles))
                {
                    try { File.Delete(file); } catch { }
                }
            }
        }
    }
}
