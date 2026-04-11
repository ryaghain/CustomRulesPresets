using System.Collections.Generic;
using BepInEx.Configuration;
using Newtonsoft.Json;
using CustomRulesPresets.UI;
using System.Runtime.Serialization;

namespace CustomRulesPresets.Core {
    public class ConfigManager {
        private readonly ConfigFile _config_file = null!;
        private ConfigEntry<bool> _config_enable_log_debug = null!;
        
        public Error load_config_values() {
            // We want to disable saving our config file every time we bind a setting as it's inefficient and slow
            _config_file.SaveOnConfigSet = false;

            _config_enable_log_debug = _config_file.Bind("Debug", "EnableLogDebugMessages", false, "Whether to log debug messages. Set this to true to enable debug logging.");
            Utilities.do_log_debug = _config_enable_log_debug.Value;
            
            // Get rid of old settings from the config file that are not used anymore
            //ClearOrphanedEntries(); // this doesn't exist???

            // We need to manually save since we disabled `SaveOnConfigSet` earlier
            _config_file.Save(); 

            // And finally, we re-enable `SaveOnConfigSet` so changes to our config entries are written to the config file automatically from now on
            _config_file.SaveOnConfigSet = true; 

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