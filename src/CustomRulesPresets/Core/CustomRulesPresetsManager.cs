using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Linq;
using static MatchSetupRules;

namespace CustomRulesPresets.Core {
	public class CustomRulesPresetsManager {
		public Error construction_error = Error.Success;

		public string presets_file_path = "";
		public MatchSetupRules instance_match_setup_rules = null!;

		public CustomRulesPresetsData custom_rules_presets_data = null!;
		
		[Serializable]
		public class CustomRulesPresetsData {
			private Dictionary<string, CustomRulesPreset> data;
			public string current_selected_preset_name = "";

			public int get_preset_count() {return data.Count;}

			public List<string> get_preset_names() {return data.Keys.ToList<string>();}

			public string get_preset_names_string() {
				string preset_names = "";
				foreach (string preset_name in data.Keys.ToList<string>()) {preset_names += preset_name + ", ";}
				preset_names = preset_names.Substring(0, preset_names.Length - ", ".Length);
				return preset_names;
			}

			public bool has_preset(string preset_name) {
				return !string.IsNullOrEmpty(preset_name) && data.ContainsKey(preset_name);
			}

			public Error preset_create(string preset_name, bool set_current = true) {
				if (string.IsNullOrEmpty(preset_name)) {
					Utilities.log_verbose(Utilities.LogType.Error, "Provided argument 'preset_name' is null or empty in preset_create.");
					return Error.ArgumentNull;
				}
				data.Add(preset_name, new CustomRulesPreset());
				if (set_current) {
					current_selected_preset_name = preset_name;
				}
				return Error.Success;
			}

			public Error preset_delete(string preset_name) {
				if (!data.ContainsKey(preset_name)) {
					Utilities.log_verbose(Utilities.LogType.Error, $"Preset to be deleted '{preset_name}' not found.");
					return Error.ArgumentOutOfRange;
				}

				if (data.Keys.Count <= 1) {
					Utilities.log_verbose(Utilities.LogType.Error, $"Cannot delete the last preset.");
					return Error.GenericFailure;
				}

				data.Remove(preset_name);
				return Error.Success;
			}

			public Error preset_duplicate(string source_preset_name, string new_preset_name, bool set_new_preset_as_current = true) {
				if (!data.ContainsKey(source_preset_name)) {
					Utilities.log_verbose(Utilities.LogType.Error, $"Source preset '{source_preset_name}' not found.");
					return Error.ArgumentOutOfRange;
				} else if (string.IsNullOrEmpty(new_preset_name)) {
					Utilities.log_verbose(Utilities.LogType.Error, "Provided argument 'new_preset_name' is null or empty.");
					return Error.ArgumentNull;
				} else if (data.ContainsKey(new_preset_name)) {
					Utilities.log_verbose(Utilities.LogType.Error, $"A preset with the name '{new_preset_name}' already exists.");
					return Error.GenericFailure;
				}

				data.Add(new_preset_name, new CustomRulesPreset(data[source_preset_name]));
				if (set_new_preset_as_current) {
					current_selected_preset_name = new_preset_name;
				}
				return Error.Success;
			}
			
			public CustomRulesPreset preset_get(string preset_name) {
				if (!data.ContainsKey(preset_name)) {
					Utilities.log_verbose(Utilities.LogType.Error, $"Preset '{preset_name}' not found.");
					return new CustomRulesPreset();
				}

				CustomRulesPreset preset = data[preset_name];
				if (preset.rules_settings == null || preset.item_spawn_chance_weights == null) {
					Utilities.log_verbose(Utilities.LogType.Error, $"Preset '{preset_name}' is not properly initialized, returning empty preset...");
					return new CustomRulesPreset();
				}

				return preset;
			}

			public bool preset_is_empty(string preset_name) {
				if (!data.ContainsKey(preset_name)) {
					Utilities.log_verbose(Utilities.LogType.Error, $"Preset '{preset_name}' not found.");
					return false;
				}
				return preset_get(preset_name).is_empty();
			}

			public Error preset_rename(string preset_name, string new_preset_name) {
				if (!data.ContainsKey(preset_name)) {
					Utilities.log_verbose(Utilities.LogType.Error, $"Preset '{preset_name}' not found.");
					return Error.ArgumentOutOfRange;
				}

				if (string.IsNullOrEmpty(new_preset_name)) {
					Utilities.log_verbose(Utilities.LogType.Error, "Provided argument 'new_preset_name' is null or empty.");
					return Error.ArgumentNull;
				}

				if (data.ContainsKey(new_preset_name)) {
					Utilities.log_verbose(Utilities.LogType.Error, $"A preset with the name '{new_preset_name}' already exists.");
					return Error.GenericFailure;
				}

				CustomRulesPreset preset = data[preset_name];
				data.Remove(preset_name);
				data.Add(new_preset_name, preset);
				if (current_selected_preset_name == preset_name) {
					current_selected_preset_name = new_preset_name;
				}
				if (Utilities.do_log_debug) {Utilities.log_verbose(Utilities.LogType.Debug, $"Preset renamed from '{preset_name}' to '{new_preset_name}'. Current selected preset: '{current_selected_preset_name}'.");}
				return Error.Success;
			}

			public Error preset_set(string preset_name, CustomRulesPreset new_preset) {
				if (!data.ContainsKey(preset_name)) {
					Utilities.log_verbose(Utilities.LogType.Error, $"Preset '{preset_name}' not found.");
					return Error.ArgumentOutOfRange;
				} else if (new_preset.is_empty()) {
					Utilities.log_verbose(Utilities.LogType.Error, "Invalid new preset, is_empty = true");
					return Error.ArgumentOutOfRange;
				}

				data[preset_name] = new_preset;
				return Error.Success;
			}

			public CustomRulesPresetsData() {
				if (Utilities.do_log_debug) {Utilities.log_verbose(Utilities.LogType.Debug, "CustomRulesPresetsData constructed empty, manually creating 'Preset 1'.");}
				data = new Dictionary<string, CustomRulesPreset>();
			}
		}

		[Serializable]
		public struct CustomRulesPreset {
			public Dictionary<MatchSetupRules.Rule, float> rules_settings;
			public Dictionary<MatchSetupRules.ItemPoolId, float> item_spawn_chance_weights;
			
			
			public Dictionary<string, object> get_data() {
				Dictionary<string, object> combined_data = new Dictionary<string, object>();
				combined_data["rules_settings"] = rules_settings;

				Dictionary<string, float> item_weights_with_string_keys = new Dictionary<string, float>();
				foreach (KeyValuePair<MatchSetupRules.ItemPoolId, float> item_and_spawn_chance_weight in item_spawn_chance_weights) {
					item_weights_with_string_keys[$"{item_and_spawn_chance_weight.Key.itemType.ToString()}, {item_and_spawn_chance_weight.Key.itemPoolIndex}"] = item_and_spawn_chance_weight.Value;
				}
				combined_data["item_spawn_chance_weights"] = item_weights_with_string_keys;

				return combined_data;
			}

			public bool is_empty() {
				return rules_settings.Count == 0 && item_spawn_chance_weights.Count == 0;
			}

			public CustomRulesPreset() {
				rules_settings = new Dictionary<MatchSetupRules.Rule, float>();
				item_spawn_chance_weights = new Dictionary<MatchSetupRules.ItemPoolId, float>();
			}

			public CustomRulesPreset(CustomRulesPreset source) {
				rules_settings = source.rules_settings.ToDictionary(entry => entry.Key, entry => entry.Value);
				item_spawn_chance_weights = source.item_spawn_chance_weights.ToDictionary(entry => entry.Key, entry => entry.Value);
			}
		}

		public Error load_presets_from_file() {
			if (!File.Exists(presets_file_path)){
				if (Utilities.do_log_debug) {Utilities.log_verbose(Utilities.LogType.Debug, $"File '{presets_file_path}' does not exist, skipping load.");}
				return Error.FileDoesNotExist;
			}

			BinaryFormatter formatter = new BinaryFormatter();
			using Stream stream = new FileStream(presets_file_path, FileMode.Open, FileAccess.Read, FileShare.Read);
			CustomRulesPresetsData deserialized_object = (CustomRulesPresetsData) formatter.Deserialize(stream);

			if (deserialized_object == null) {
				Utilities.log_verbose(Utilities.LogType.Error, $"Failed to deserialize presets from file: {presets_file_path}");
				return Error.GenericFailure;

			} else {
				custom_rules_presets_data = deserialized_object;
				preset_load_settings(custom_rules_presets_data.current_selected_preset_name);

				if (Utilities.do_log_debug) {Utilities.log_verbose(Utilities.LogType.Debug, $"Successfully loaded presets '{custom_rules_presets_data.get_preset_names_string()}' from '{presets_file_path}'.");}

				return Error.Success;
			}
		}
		
		public Error preset_load_settings(string preset_name) {
			if (preset_name == null) {
				Utilities.log_verbose(Utilities.LogType.Error, $"Provided argument '{preset_name}' is null in preset_load_settings.");
				return Error.ArgumentNull;

			} else if (!custom_rules_presets_data.has_preset(preset_name)) {
				Utilities.log_verbose(Utilities.LogType.Error, $"Preset '{preset_name}' does not exist.");
				return Error.ArgumentOutOfRange;
			}

			CustomRulesPreset preset = custom_rules_presets_data.preset_get(preset_name);
			if (preset.is_empty()) {
				Utilities.log_verbose(Utilities.LogType.Error, $"Failed to get preset '{preset_name}' or preset is empty.");
				return Error.GenericFailure;

			} else {
				Error set_rules_error = set_rules(preset.rules_settings);
				if (Utilities.do_log_debug) {Utilities.log_verbose(Utilities.LogType.Debug, $"Loaded preset '{preset_name}'s rules status: {set_rules_error.ToString()}");}
				if (set_rules_error != Error.Success) {
					Utilities.log_verbose(Utilities.LogType.Error, $"Failed to load preset '{preset_name}' due to error in setting rules.");
					return Error.GenericFailure;
				}

				Error set_item_spawn_chance_weights_error = set_item_spawn_chance_weights(preset.item_spawn_chance_weights);
				if (Utilities.do_log_debug) {Utilities.log_verbose(Utilities.LogType.Debug, $"Loaded preset '{preset_name}'s item spawn chance weights status: {set_item_spawn_chance_weights_error.ToString()}");}
				if (set_item_spawn_chance_weights_error != Error.Success) {
					Utilities.log_verbose(Utilities.LogType.Error, $"Failed to load preset '{preset_name}' due to error in setting item spawn chance weights.");
					return Error.GenericFailure;
				}
			}

			custom_rules_presets_data.current_selected_preset_name = preset_name;

			return Error.Success;
		}

		public Error preset_save_settings(string preset_name = "") {
			if (preset_name == "") {preset_name = custom_rules_presets_data.current_selected_preset_name;}

			if (!custom_rules_presets_data.has_preset(preset_name)) {
				Utilities.log_verbose(Utilities.LogType.Error, $"Preset '{preset_name}' does not exist.");
				return Error.ArgumentOutOfRange;
			} else if (instance_match_setup_rules == null) {
				Utilities.log_verbose(Utilities.LogType.Error, "CustomRulesPresetsManager is not set up properly.");
				return Error.GlobalVariableIsNull;
			}

			CustomRulesPreset preset = custom_rules_presets_data.preset_get(preset_name);

			Dictionary<Rule, float> current_rules_settings = new Dictionary<Rule, float>();
			foreach (KeyValuePair<Rule, float> rule_and_value_kvp in instance_match_setup_rules.rules) {
				current_rules_settings.Add(rule_and_value_kvp.Key, rule_and_value_kvp.Value);
			}

			if (current_rules_settings.Count == 0) {
				Utilities.log_verbose(Utilities.LogType.Error, $"Failed to save rules settings from the MatchSetupRules instance. Source rules dict == null: {instance_match_setup_rules.rules == null}");
				return Error.GenericFailure;
			}

			preset.rules_settings = current_rules_settings;
			//if (Utilities.do_log_debug) {Utilities.log_verbose(Utilities.LogType.Debug, $"Saved rules settings for preset '{preset_name}': {Utilities.rules_dict_to_json(preset.rules_settings)}");};

			Dictionary<ItemPoolId, float> current_item_spawn_chance_weights = new Dictionary<ItemPoolId, float>();
			foreach (KeyValuePair<ItemPoolId, float> id_and_value_kvp in instance_match_setup_rules.spawnChanceWeights) {
				current_item_spawn_chance_weights.Add(id_and_value_kvp.Key, id_and_value_kvp.Value);
			}

			if (current_item_spawn_chance_weights.Count == 0) {
				Utilities.log_verbose(Utilities.LogType.Error, $"Failed to save item spawn chance weights from the MatchSetupRules instance. Source spawnChanceWeights dict == null: {instance_match_setup_rules.spawnChanceWeights == null}");
				return Error.GenericFailure;
			}

			preset.item_spawn_chance_weights = current_item_spawn_chance_weights;
			//if (Utilities.do_log_debug) {Utilities.log_verbose(Utilities.LogType.Debug, $"Saved item spawn chance weights for preset '{preset_name}': {Utilities.item_spawn_chance_weights_dict_to_json(preset.item_spawn_chance_weights)}");};

			Error preset_set_error = custom_rules_presets_data.preset_set(preset_name, preset);
			if (preset_set_error != Error.Success) {
				Utilities.log_verbose(Utilities.LogType.Error, $"Failed to save preset '{preset_name}' with error: {preset_set_error.ToString()}");
				return Error.GenericFailure;
			}

			return Error.Success;
		}

		public Error save_presets_to_file() {
            if (custom_rules_presets_data == null) {
                Utilities.log_verbose(Utilities.LogType.Error, $"presets_data is null. Cannot save to config.");
                return Error.ArgumentNull;
            }

            BinaryFormatter formatter = new BinaryFormatter();
            using FileStream stream = new FileStream(presets_file_path, FileMode.Create, FileAccess.Write, FileShare.None);
            formatter.Serialize(stream, custom_rules_presets_data);

            if (Utilities.do_log_debug) {Utilities.log_verbose(Utilities.LogType.Debug, $"Presets '{custom_rules_presets_data.get_preset_names_string()}' saved to '{presets_file_path}'.");}
            return Error.Success;
        }

		// Sets the item spawn chance weights in the MatchSetupRules instance with the provided settings.
		public Error set_item_spawn_chance_weights(Dictionary<MatchSetupRules.ItemPoolId, float> item_spawn_chance_weights) {
			if (instance_match_setup_rules == null) {
				Utilities.log_verbose(Utilities.LogType.Error, "CustomRulesPresetsManager is not set up properly.");
				return Error.GlobalVariableIsNull;
			
			} else if (item_spawn_chance_weights == null) {
				Utilities.log_verbose(Utilities.LogType.Error, "Provided argument 'item_spawn_chance_weights' is null.");
				return Error.ArgumentNull;

			} else if (item_spawn_chance_weights.Count == 0) {
				Utilities.log_verbose(Utilities.LogType.Error, "Provided argument 'item_spawn_chance_weights' is empty.");
				return Error.ArgumentOutOfRange;
			}

			foreach (KeyValuePair<MatchSetupRules.ItemPoolId, float> id_and_weight_kvp in item_spawn_chance_weights) {
				instance_match_setup_rules.SetSpawnChance(id_and_weight_kvp.Key.itemPoolIndex, id_and_weight_kvp.Key.itemType, id_and_weight_kvp.Value);
			}

			foreach (MatchSetupRules.ItemPoolId item_pool_id in item_spawn_chance_weights.Keys) {
				instance_match_setup_rules.ServerUpdateSpawnChanceValue(item_pool_id);
			}

			return Error.Success;
		}

		// Sets the rules in the MatchSetupRules instance with the provided settings.
		public Error set_rules(Dictionary<Rule, float> rules_settings) {
			if (instance_match_setup_rules == null) {
				Utilities.log_verbose(Utilities.LogType.Error, "CustomRulesPresetsManager is not set up properly.");
				return Error.GlobalVariableIsNull;

			} else if (rules_settings == null) {
				Utilities.log_verbose(Utilities.LogType.Error, "Provided rules settings are null.");
				return Error.ArgumentNull;

			} else if (rules_settings.Count == 0) {
				Utilities.log_verbose(Utilities.LogType.Error, "Provided rules settings are empty.");
				return Error.ArgumentOutOfRange;
			}

			foreach (KeyValuePair<Rule, float> rule_and_value_kvp in rules_settings) {
				instance_match_setup_rules.SetValue(rule_and_value_kvp.Key, rule_and_value_kvp.Value);

				SliderOption value3;
				if (instance_match_setup_rules.onOffDropdownLookup.TryGetValue(rule_and_value_kvp.Key, out var value2)) {
					value2.SetValue((!instance_match_setup_rules.GetValueAsBoolInternal(rule_and_value_kvp.Key)) ? 1 : 0);

				} else if (instance_match_setup_rules.sliderLookup.TryGetValue(rule_and_value_kvp.Key, out value3)) {
					value3.SetValue(instance_match_setup_rules.GetValueInternal(rule_and_value_kvp.Key));
				}

				instance_match_setup_rules.UpdateRule(rule_and_value_kvp.Key);

				if (rule_and_value_kvp.Key == Rule.ConsoleCommands) {
					instance_match_setup_rules.CheckAndShowCheatsWarning();
				}
			}

			return Error.Success;
		}

		public CustomRulesPresetsManager(MatchSetupRules? new_instance_match_setup_rules) {
			if (new_instance_match_setup_rules == null) {
				Utilities.log_verbose(Utilities.LogType.Error, "Failed to set up CustomRulesPresetsManager: the provided instance is null.");
				construction_error = Error.ArgumentNull;

			} else {
				instance_match_setup_rules = new_instance_match_setup_rules;
				presets_file_path = CustomRulesPresetsPlugin.cache_path + "SavedCustomRulesPresets.dat";

				Error load_presets_error = load_presets_from_file();
				if (load_presets_error != Error.Success) {
					custom_rules_presets_data = new CustomRulesPresetsData();
				}
				
				if (Utilities.do_log_debug) {
					Utilities.log_verbose(Utilities.LogType.Debug, $"Loaded presets file has current selected name '{custom_rules_presets_data.current_selected_preset_name}'. CustomRulesPresetsManager setup done.");
				}
			}
		}
	}
}
