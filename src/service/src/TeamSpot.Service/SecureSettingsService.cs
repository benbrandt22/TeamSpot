using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace TeamSpot.Service
{
    /// <summary>
    /// Provides secure loading, updating, and saving of application settings with encryption.
    /// </summary>
    /// <remarks>
    /// The <see cref="TSettings"/> object is serialized to JSON and encrypted using Windows DPAPI before
    /// being saved to disk, and decrypted and deserialized when loaded. File is stored in the common application data folder,
    /// so it is shared across all users of the machine, but protected with DPAPI so only this machine can decrypt it.
    /// </remarks>
    /// <typeparam name="TSettings">The type representing the application settings to manage.</typeparam>
    public class SecureSettingsService<TSettings>
    {
        private string _settingsDirectory;
        private string _settingsFilename = $"{typeof(TSettings).Name}.json";
        private ILogger<SecureSettingsService<TSettings>> _logger;

        public SecureSettingsService(ILogger<SecureSettingsService<TSettings>> logger)
        {
            _logger = logger;
            var assembly = Assembly.GetExecutingAssembly();

            var appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var appName = assembly.GetName().Name!;

            _settingsDirectory = Path.Combine(appDataDir, appName);

            SettingsPath = Path.Combine(_settingsDirectory, _settingsFilename);
        }

        public string SettingsPath { get; private set; }

        /// <summary>
        /// Loads and returns the settings object
        /// </summary>
        public TSettings Settings => LoadSettings();

        /// <summary>
        /// Loads the settings, applies the specified updates, and saves the updated settings back to disk.
        /// </summary>
        public void Update(Action<TSettings> settingsUpdates)
        {
            var settings = LoadSettings();
            settingsUpdates(settings);
            SaveSettings(settings);
        }

        private TSettings LoadSettings()
        {
            TSettings settings;
            try
            {
                EnsureDirectoryExists();
                var fileText = File.ReadAllText(SettingsPath);
                var decryptedFileText = Unprotect(fileText);
                settings = JsonSerializer.Deserialize<TSettings>(decryptedFileText)!;
            }
            catch (Exception ex)
            {
                // revert to default settings if any error occurs, such as file not found, decryption failure, deserialization failure, etc.
                _logger.LogWarning("Error loading settings, reverting to default settings. {msg}", ex.Message);
                settings = Activator.CreateInstance<TSettings>();
            }
            return settings;
        }

        private void SaveSettings(TSettings settings)
        {
            var settingsJson = JsonSerializer.Serialize(settings);
            var encryptedFileText = Protect(settingsJson);
            EnsureDirectoryExists();
            File.WriteAllText(SettingsPath, encryptedFileText);
        }

        private void EnsureDirectoryExists()
        {
            if (!Directory.Exists(_settingsDirectory))
            {
                Directory.CreateDirectory(_settingsDirectory);
            }
        }

        private string Protect(string plaintext)
        {
            var textAsBytes = Encoding.UTF8.GetBytes(plaintext);
            var protectedBytes = ProtectedData.Protect(textAsBytes, null, DataProtectionScope.LocalMachine);
            var protectedBase64Text = Convert.ToBase64String(protectedBytes);
            return protectedBase64Text;
        }

        private string Unprotect(string protectedBase64Text)
        {
            var protectedbytes = Convert.FromBase64String(protectedBase64Text);
            var unprotectedbytes = ProtectedData.Unprotect(protectedbytes, null, DataProtectionScope.LocalMachine);
            var plainText = Encoding.UTF8.GetString(unprotectedbytes);
            return plainText;
        }

    }
}
