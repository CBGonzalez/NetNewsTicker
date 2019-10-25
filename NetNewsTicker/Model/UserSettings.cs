using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace NetNewsTicker.Model
{
    public class UserSettings
    {
        private const string settingsFileName = "NETNewsTicker\\NetNewsTicker.json";
        private static string fullPath = string.Empty;

        public int Service { get; set; } = 0;

        public int Page { get; set; } = 0;

        public double Refresh { get; set; } = 5.0;

        public bool Primary { get; set; } = true;

        public bool Top { get; set; } = true;

        public static UserSettings LoadSettings()
        {
            var fileSettings = new UserSettings();
            fullPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), settingsFileName);
            if (File.Exists(fullPath))
            {
                byte[] jsonBytes = File.ReadAllBytes(fullPath);
                fileSettings = JsonSerializer.Deserialize<UserSettings>(jsonBytes);
            }
            return fileSettings;
        }

        public static async Task<bool> SaveSettings(UserSettings settings)
        {
            bool result = false;
            if(settings != null && fullPath.Length > 0)
            {
                await File.WriteAllTextAsync(fullPath, JsonSerializer.Serialize(settings)).ConfigureAwait(false);
                result = true;
            }
            return result;
        }
    }
}
