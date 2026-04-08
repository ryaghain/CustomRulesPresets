using BepInEx.Configuration;

namespace CustomRulesPresets.Core {
    public class ConfigManager {
        private ConfigEntry<bool> _config_enable_log_debug = null!;
        private readonly ConfigFile _config_file = null!;

        public Error load_config_values() {
            _config_enable_log_debug = _config_file.Bind("Debug", "EnableLogDebugMessages", false, "Whether to log debug messages. Set this to true to enable debug logging.");
            Utilities.do_log_debug = _config_enable_log_debug.Value;
            return Error.Success;
        }

        public ConfigManager(ConfigFile new_config_file) {
            if (new_config_file == null) {
                Utilities.log_verbose(Utilities.LogType.Error, "Config file provided to ConfigManager constructor is null.");
            } else {
                _config_file = new_config_file;
            }
        }
    }
}