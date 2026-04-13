using System.Collections.Generic;
using BepInEx.Configuration;
using System.Reflection;
using HarmonyLib;

namespace CustomRulesPresets.Core {
    public class ConfigManager {
        private readonly ConfigFile _config_file = null!;
        private ConfigEntry<bool> _config_enable_log_debug = null!;
        private ConfigEntry<bool> _config_do_save_tree_to_disk = null!;
        public bool do_save_tree_to_disk {get{return _config_do_save_tree_to_disk.Value;} set{_config_do_save_tree_to_disk.Value = value;}}
        
        void clear_orphaned_entries() { 
            // Find the private property `OrphanedEntries` from the type `ConfigFile`
            PropertyInfo orphanedEntriesProp = AccessTools.Property(typeof(ConfigFile), "OrphanedEntries"); 

            // And get the value of that property from our ConfigFile instance
            var orphanedEntries = (Dictionary<ConfigDefinition, string>)orphanedEntriesProp.GetValue(_config_file); 
            
            // And finally, clear the `OrphanedEntries` dictionary
            orphanedEntries.Clear(); 
        }
        
        public Error load_config_values() {
            // We want to disable saving our config file every time we bind a setting as it's inefficient and slow
            _config_file.SaveOnConfigSet = false;

            _config_enable_log_debug = _config_file.Bind("Debug", "EnableLogDebugMessages", false, "Whether to log debug messages. Set this to true to enable debug logging.");
            Utilities.do_log_debug = _config_enable_log_debug.Value;

            _config_do_save_tree_to_disk = _config_file.Bind(
                "Debug",
                "SaveMenuTreeToFileOnNextRun",
                false,
                "When toggled on, will save the rules menu object tree information to your BepInEx folder the next time you load into a lobby, then it will toggle back off."
            );
            
            // Get rid of old settings from the config file that are not used anymore, do this *after* all 'Bind' calls.
            clear_orphaned_entries();

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