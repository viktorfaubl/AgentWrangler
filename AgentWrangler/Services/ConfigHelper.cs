using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using AgentWrangler.Services;

namespace AgentWrangler.Services;

public class ConfigHelper
{
    public static string GetConfigPath()
    {
        string configFile = "config.json";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string dir = Path.Combine(appData, "AgentWrangler");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, configFile);
        }
        else // Linux/macOS
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string dir = Path.Combine(home, ".config", "AgentWrangler");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, configFile);
        }
    }

    public static string? ReadApiKey()
    {
        string path = GetConfigPath();
        if (!File.Exists(path)) return null;
        try
        {
            var json = File.ReadAllText(path);
            var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("apiKey", out var keyProp))
                return keyProp.GetString();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "ConfigHelper.ReadApiKey");
        }
        return null;
    }

    public static void SaveApiKey(string apiKey)
    {
        string path = GetConfigPath();
        try
        {
            var json = JsonSerializer.Serialize(new { apiKey });
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "ConfigHelper.SaveApiKey");
        }
    }
}
