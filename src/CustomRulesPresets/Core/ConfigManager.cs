using System.Collections.Generic;
using BepInEx.Configuration;
using Newtonsoft.Json;
using CustomRulesPresets.UI;

namespace CustomRulesPresets.Core {
    public class ConfigManager {
        private readonly ConfigFile _config_file = null!;
        private ConfigEntry<bool> _config_enable_log_debug = null!;
        private ConfigEntry<string> _config_presets = null!;
        
        public Error load_config_values() {
            // We want to disable saving our config file every time we bind a setting as it's inefficient and slow
            _config_file.SaveOnConfigSet = false;

            _config_enable_log_debug = _config_file.Bind("Debug", "EnableLogDebugMessages", false, "Whether to log debug messages. Set this to true to enable debug logging.");
            Utilities.do_log_debug = _config_enable_log_debug.Value;

            _config_presets = _config_file.Bind("Presets", "PresetsEntries", "", "This is where your presets are stored. You can edit this if you want to manually add/edit presets, but I wouldn't recommend it unless you know what you're doing.");
            string config_presets_json = _config_presets.Value;
            if (Utilities.do_log_debug) {Utilities.log_verbose(Utilities.LogType.Debug, $"Loaded presets JSON from config: {config_presets_json}");};
            if (!string.IsNullOrEmpty(config_presets_json) && config_presets_json != "{}") {
                Dictionary<string, string> deserialized_config_presets = JsonConvert.DeserializeObject<Dictionary<string, string>>(config_presets_json)!;
                foreach (KeyValuePair<string, string> preset_name_and_json_kvp in deserialized_config_presets) {
                    Error preset_create_error = CustomRulesPresetsManager.custom_rules_presets_data.preset_create(preset_name_and_json_kvp.Key, preset_name_and_json_kvp.Value);
                    if (preset_create_error != Error.Success) {
                        Utilities.log_verbose(Utilities.LogType.Error, $"Failed to create preset '{preset_name_and_json_kvp.Key}' loaded from config file with error: {preset_create_error.ToString()}");
                        if (Utilities.do_log_debug) {Utilities.log_verbose(Utilities.LogType.Debug, $"Preset JSON that failed to load: {preset_name_and_json_kvp.Value}");};
                    }
                }
            } else {
                CustomRulesPresetsManager.custom_rules_presets_data.preset_create("Preset 1");
            }
            
            // Get rid of old settings from the config file that are not used anymore
            //ClearOrphanedEntries(); // this doesn't exist???

            // We need to manually save since we disabled `SaveOnConfigSet` earlier
            _config_file.Save(); 

            // And finally, we re-enable `SaveOnConfigSet` so changes to our config entries are written to the config file automatically from now on
            _config_file.SaveOnConfigSet = true; 

            return Error.Success;
        }

        public Error save_presets_to_config(string preset_json) {
            if (preset_json == null) {
                Utilities.log_verbose(Utilities.LogType.Error, $"Preset JSON is null. Cannot save to config.");
                return Error.ArgumentNull;
            } else if (_config_presets == null) {
                Utilities.log_verbose(Utilities.LogType.Error, $"Config entry for presets is null. Cannot save to config.");
                return Error.GlobalVariableIsNull;
            }

            _config_presets.Value = preset_json;

            if (Utilities.do_log_debug) {CustomRulesPresetsPlugin.Log.LogInfo("Presets saved to config successfully.");}
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