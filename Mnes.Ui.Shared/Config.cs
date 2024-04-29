using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Mnes.Ui.Shared;

public static class Config {
   const string DEFAULT_SAVE_FOLDER = "%AppData%/MNES";

   const string DEFAULT_INI_TEXT =
      $"; The location of the save folder. If it doesn't exist, it will be created. You can set this to a local path to make this portable.\n" +
      $"SaveFolder = {DEFAULT_SAVE_FOLDER}";

   const string LOCAL_INI_FILE = "local_config.ini";
   const string CONFIG_FILE = "config.json";

   public static bool Initialized => Settings != null;

   static string _save_folder = "";
   static string SaveFile => Path.Combine(_save_folder, CONFIG_FILE);

   public static UserSettings? Settings { get; private set; }

   public static void InitializeFromDisk() {
      if (Initialized)
         throw new InvalidOperationException("Config already initialized.");

      if (!File.Exists(LOCAL_INI_FILE))
         File.WriteAllText(LOCAL_INI_FILE, DEFAULT_INI_TEXT);

      // Build a configuration object from INI file
      IConfiguration config = new ConfigurationBuilder()
          .AddIniFile(LOCAL_INI_FILE)
          .Build();

      _save_folder = config["SaveFolder"] ?? "";
      if (string.IsNullOrEmpty(_save_folder))
         _save_folder = DEFAULT_SAVE_FOLDER;

      _save_folder = Environment.ExpandEnvironmentVariables(_save_folder);
      if (!Directory.Exists(_save_folder))
         Directory.CreateDirectory(_save_folder);

      UserSettings settings;
      if (!File.Exists(SaveFile)) {
         settings = new();
         File.WriteAllText(
            SaveFile,
            JsonConvert.SerializeObject(settings)
         );
      } else {
         var saveFileText = File.ReadAllText(SaveFile);
         settings =
            JsonConvert.DeserializeObject<UserSettings>(saveFileText)
            ?? throw new InvalidOperationException("Deserializing JSON resulted in null.");
      }

      Settings = settings;
   }

   public static void Save() {
      if (!Initialized)
         throw new InvalidOperationException($"Cannot save config before {nameof(InitializeFromDisk)} has been called.");

      File.WriteAllText(SaveFile, JsonConvert.SerializeObject(Settings));
   }

   /// <summary> Resets the settings to default and saves. </summary>
   public static void Reset() {
      Settings = new();
      Save();
   }
}
