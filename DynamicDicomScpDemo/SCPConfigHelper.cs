using DynamicDicomScpDemo.Models;
using System.Text.Json;

namespace DynamicDicomScpDemo {
    public class SCPConfigHelper {
        public async static Task<SCPConfiguration> SCPConfigFile() {
            var appPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            if (appPath == null) {
                throw new NullReferenceException(nameof(appPath));
            }
            var directory = System.IO.Path.GetDirectoryName(appPath);
            if (directory == null) {
                throw new NullReferenceException(nameof(directory));
            }
            var configPath = Path.Combine(directory, @"Config\scp.config.json");
            if (!File.Exists(configPath)) {
                throw new NullReferenceException(nameof(configPath));
            }
            var configRead = await File.ReadAllTextAsync(configPath);

            var scpConfig = JsonSerializer.Deserialize<SCPConfiguration>(configRead);
            if (scpConfig == null) {
                throw new NullReferenceException(nameof(scpConfig));
            }
            return scpConfig;
        }
    }
}
