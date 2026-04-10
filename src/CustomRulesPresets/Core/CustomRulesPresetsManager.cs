using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using Newtonsoft.Json;
using UnityEngine.Assertions;
using static MatchSetupRules;

namespace CustomRulesPresets.Core {
	public static class CustomRulesPresetsManager {
		public static MatchSetupRules instance_match_setup_rules = null!;

		public static Error last_preset_get_error = Error.Success;
		public static string current_selected_preset_name = "";
		public static CustomRulesPresetsData custom_rules_presets_data = new CustomRulesPresetsData();
		
		[Serializable]
		public struct CustomRulesPresetsData {
			private Dictionary<string, CustomRulesPreset> data;

			public int get_preset_count() {return data.Count;}

			public List<string> get_preset_names() {return data.Keys.ToList<string>();}

			public bool has_preset(string preset_name) {
				return data.ContainsKey(preset_name);
			}

			public Error preset_create(string preset_name, string preset_data_json) {
				if (string.IsNullOrEmpty(preset_name)) {
					Utilities.log_verbose(Utilities.LogType.Error, "Provided argument 'preset_name' is null or empty in preset_create.");
					return Error.ArgumentNull;
				} else if (string.IsNullOrEmpty(preset_data_json)) {
					Utilities.log_verbose(Utilities.LogType.Error, "Provided argument 'preset_data_json' is null or empty in preset_create.");
					return Error.ArgumentNull;
				}
				data.Add(preset_name, new CustomRulesPreset(preset_data_json));
				return Error.Success;
			}

			public Error preset_create(string preset_name) {
				if (string.IsNullOrEmpty(preset_name)) {
					Utilities.log_verbose(Utilities.LogType.Error, "Provided argument 'preset_name' is null or empty in preset_create.");
					return Error.ArgumentNull;
				}
				data.Add(preset_name, new CustomRulesPreset());
				return Error.Success;
			}

			public Error preset_delete(string preset_name) {
				if (!data.ContainsKey(preset_name)) {
					Utilities.log_verbose(Utilities.LogType.Error, $"Preset '{preset_name}' not found in CustomRulesPresetsData.preset_delete.");
					return Error.ArgumentOutOfRange;
				}

				data.Remove(preset_name);
				return Error.Success;
			}

			public Error preset_duplicate(string source_preset_name, string new_preset_name) {
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
				return Error.Success;
			}
			
			public CustomRulesPreset preset_get(string preset_name) {
				if (!data.ContainsKey(preset_name)) {
					Utilities.log_verbose(Utilities.LogType.Error, $"Preset '{preset_name}' not found.");
					last_preset_get_error = Error.ArgumentOutOfRange;
					return new CustomRulesPreset();
				}

				CustomRulesPreset preset = data[preset_name];
				if (preset.rules_settings == null || preset.item_spawn_chance_weights == null) {
					Utilities.log_verbose(Utilities.LogType.Error, $"Preset '{preset_name}' is not properly initialized, returning empty preset...");
					last_preset_get_error = Error.GenericFailure;
					return new CustomRulesPreset();
				}

				last_preset_get_error = Error.Success;
				return preset;
			}

			public Error preset_set(string preset_name, CustomRulesPreset new_preset) {
				if (!data.ContainsKey(preset_name)) {
					Utilities.log_verbose(Utilities.LogType.Error, $"Preset '{preset_name}' not found in CustomRulesPresetsData.preset_set.");
					return Error.ArgumentOutOfRange;
				} else if (new_preset.is_empty()) {
					Utilities.log_verbose(Utilities.LogType.Error, $"Invalid new preset {new_preset.to_json()}.");
					return Error.ArgumentOutOfRange;
				}

				data[preset_name] = new_preset;
				return Error.Success;
			}

			public bool preset_is_empty(string preset_name) {
				if (!data.ContainsKey(preset_name)) {
					Utilities.log_verbose(Utilities.LogType.Error, $"Preset '{preset_name}' not found in CustomRulesPresetsData.preset_is_empty.");
					last_preset_get_error = Error.ArgumentOutOfRange;
					return false;
				}
				return preset_get(preset_name).is_empty();
			}

			public string to_json() {
				return JsonConvert.SerializeObject(this);
			}

			public CustomRulesPresetsData() {
				data = new Dictionary<string, CustomRulesPreset>();
			}
			
			public CustomRulesPresetsData(Dictionary<string, CustomRulesPreset> existing_presets) {
				if (existing_presets == null) {
					Utilities.log_verbose(Utilities.LogType.Error, "Provided argument 'presets' is null in CustomRulesPresetsData constructor, initializing with empty dict...");
					data = new Dictionary<string, CustomRulesPreset>();
				} else {
					data = existing_presets;
				}
			}

			public CustomRulesPresetsData(Dictionary<string, string> existing_presets_json) {
				if (existing_presets_json == null) {
					Utilities.log_verbose(Utilities.LogType.Error, "Provided argument 'presets_json' is null in CustomRulesPresetsData constructor, initializing with empty dict...");
					data = new Dictionary<string, CustomRulesPreset>();
				} else {
					data = new Dictionary<string, CustomRulesPreset>();
					foreach (var kvp in existing_presets_json) {
						data[kvp.Key] = new CustomRulesPreset(kvp.Value);
					}
				}
			}
		}

		[Serializable]
		public struct CustomRulesPreset {
			public Dictionary<MatchSetupRules.Rule, float> rules_settings;
			public Dictionary<MatchSetupRules.ItemPoolId, float> item_spawn_chance_weights;
			
			public bool is_empty() {
				return rules_settings.Count == 0 && item_spawn_chance_weights.Count == 0;
			}

			public string to_json() {
				return JsonConvert.SerializeObject(this);
			}

			public CustomRulesPreset() {
				rules_settings = new Dictionary<MatchSetupRules.Rule, float>();
				item_spawn_chance_weights = new Dictionary<MatchSetupRules.ItemPoolId, float>();
			}

			public CustomRulesPreset(CustomRulesPreset source) {
				rules_settings = source.rules_settings.ToDictionary(entry => entry.Key, entry => entry.Value);
				item_spawn_chance_weights = source.item_spawn_chance_weights.ToDictionary(entry => entry.Key, entry => entry.Value);
			}

			public CustomRulesPreset(string json) {
				if (string.IsNullOrEmpty(json)) {
					Utilities.log_verbose(Utilities.LogType.Error, "Provided argument 'json' is null or empty in CustomRulesPreset constructor, initializing with empty preset...");
					rules_settings = new Dictionary<MatchSetupRules.Rule, float>();
					item_spawn_chance_weights = new Dictionary<MatchSetupRules.ItemPoolId, float>();
					return;
				}

				CustomRulesPreset? deserialized_preset = JsonConvert.DeserializeObject<CustomRulesPreset>(json);
				if (deserialized_preset == null) {
					Utilities.log_verbose(Utilities.LogType.Error, "Failed to deserialize the provided JSON string in CustomRulesPreset constructor, initializing with empty preset...");
					rules_settings = new Dictionary<MatchSetupRules.Rule, float>();
					item_spawn_chance_weights = new Dictionary<MatchSetupRules.ItemPoolId, float>();
				} else {
					rules_settings = deserialized_preset.Value.rules_settings ?? new Dictionary<MatchSetupRules.Rule, float>();
					item_spawn_chance_weights = deserialized_preset.Value.item_spawn_chance_weights ?? new Dictionary<MatchSetupRules.ItemPoolId, float>();
				}
			}
		}

		public static Error preset_load_settings(string preset_name) {
			if (preset_name == null) {
				Utilities.log_verbose(Utilities.LogType.Error, "Provided argument 'preset_name' is null in preset_load_settings.");
				return Error.ArgumentNull;
			}
			 else if (!custom_rules_presets_data.has_preset(preset_name)) {
				Utilities.log_verbose(Utilities.LogType.Error, $"Preset '{preset_name}' does not exist.");
				return Error.ArgumentOutOfRange;
			} else if (preset_name == current_selected_preset_name) {
				if (Utilities.do_log_debug) {Utilities.log_verbose(Utilities.LogType.Debug, $"Preset '{preset_name}' is already selected, skipping load.");}
				return Error.Success;
			}

			CustomRulesPreset preset = custom_rules_presets_data.preset_get(preset_name);
			if (last_preset_get_error != Error.Success) {
				Utilities.log_verbose(Utilities.LogType.Error, $"Failed to get preset '{preset_name}' in preset_load_settings with error: {last_preset_get_error.ToString()}");
				return Error.GenericFailure;
			}

			if (Utilities.do_log_debug) {
				Utilities.log_verbose(
					Utilities.LogType.Debug,
					$"Loading preset '{preset_name}'. rules: {Utilities.rules_dict_to_json(preset.rules_settings)}, item spawn chance weights: {Utilities.item_spawn_chance_weights_dict_to_json(preset.item_spawn_chance_weights)}"
				);
			};
		
			if (preset.is_empty()) {
				Utilities.log_verbose(Utilities.LogType.Error, $"Preset '{preset_name}' is empty. Was there a preset save failure?");
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

			current_selected_preset_name = preset_name;

			return Error.Success;
		}

		public static Error preset_save_settings(string preset_name) {
			if (!custom_rules_presets_data.has_preset(preset_name)) {
				Utilities.log_verbose(Utilities.LogType.Error, $"Preset '{preset_name}' does not exist.");
				return Error.ArgumentOutOfRange;
			} else if (instance_match_setup_rules == null) {
				Utilities.log_verbose(Utilities.LogType.Error, "CustomRulesPresetsManager is not set up properly.");
				return Error.GlobalVariableIsNull;
			}

			CustomRulesPreset preset = custom_rules_presets_data.preset_get(preset_name);
			if (last_preset_get_error != Error.Success) {
				Utilities.log_verbose(Utilities.LogType.Error, $"Failed to get preset '{preset_name}' with error: {last_preset_get_error.ToString()}");
				return Error.GenericFailure;
			}

			Dictionary<Rule, float> current_rules_settings = new Dictionary<Rule, float>();
			foreach (KeyValuePair<Rule, float> rule_and_value_kvp in instance_match_setup_rules.rules) {
				current_rules_settings.Add(rule_and_value_kvp.Key, rule_and_value_kvp.Value);
			}
			if (current_rules_settings.Count == 0) {
				Utilities.log_verbose(Utilities.LogType.Error, $"Failed to save rules settings from the MatchSetupRules instance. Source rules dict == null: {instance_match_setup_rules.rules == null}");
				return Error.GenericFailure;
			}
			preset.rules_settings = current_rules_settings;
			if (Utilities.do_log_debug) {Utilities.log_verbose(Utilities.LogType.Debug, $"Saved rules settings for preset '{preset_name}': {Utilities.rules_dict_to_json(preset.rules_settings)}");};

			Dictionary<ItemPoolId, float> current_item_spawn_chance_weights = new Dictionary<ItemPoolId, float>();
			foreach (KeyValuePair<ItemPoolId, float> id_and_value_kvp in instance_match_setup_rules.spawnChanceWeights) {
				current_item_spawn_chance_weights.Add(id_and_value_kvp.Key, id_and_value_kvp.Value);
			}
			if (current_item_spawn_chance_weights.Count == 0) {
				Utilities.log_verbose(Utilities.LogType.Error, $"Failed to save item spawn chance weights from the MatchSetupRules instance. Source spawnChanceWeights dict == null: {instance_match_setup_rules.spawnChanceWeights == null}");
				return Error.GenericFailure;
			}
			preset.item_spawn_chance_weights = current_item_spawn_chance_weights;
			if (Utilities.do_log_debug) {Utilities.log_verbose(Utilities.LogType.Debug, $"Saved item spawn chance weights for preset '{preset_name}': {Utilities.item_spawn_chance_weights_dict_to_json(preset.item_spawn_chance_weights)}");};

			Error preset_set_error = custom_rules_presets_data.preset_set(preset_name, preset);
			if (preset_set_error != Error.Success) {
				Utilities.log_verbose(Utilities.LogType.Error, $"Failed to save preset '{preset_name}' with error: {preset_set_error.ToString()}");
				return Error.GenericFailure;
			}

			CustomRulesPresetsPlugin.config_manager.save_presets_to_config(custom_rules_presets_data.to_json()); // this is probably overkill, but I think it's better than trying to save once at game close but the game crashes before the presets can be saved

			return Error.Success;
		}

		// Sets the item spawn chance weights in the MatchSetupRules instance with the provided settings.
		public static Error set_item_spawn_chance_weights(Dictionary<MatchSetupRules.ItemPoolId, float> item_spawn_chance_weights) {
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
		public static Error set_rules(Dictionary<Rule, float> rules_settings) {
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

		// Stores the provided MatchSetupRules instance and retrieves the necessary FieldInfo and MethodInfo for later use.
		public static Error setup(MatchSetupRules? new_instance_match_setup_rules) {
			if (new_instance_match_setup_rules == null) {
				Utilities.log_verbose(Utilities.LogType.Error, "Failed to set up CustomRulesPresetsManager: the provided instance is null.");
				return Error.GenericFailure;
			}

			instance_match_setup_rules = new_instance_match_setup_rules;

			if (Utilities.do_log_debug) {Utilities.log_verbose(Utilities.LogType.Debug, "CustomRulesPresetsManager setup done. ConfigManager should load any saved presets next.");};
			return Error.Success;
		}
	}
}
