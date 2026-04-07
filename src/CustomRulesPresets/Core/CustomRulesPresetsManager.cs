using System.Collections.Generic;
using Mirror;
using Newtonsoft.Json;
using UnityEngine.Assertions;
using static MatchSetupRules;

namespace CustomRulesPresets.Core {
	public static class CustomRulesPresetsManager {
		public enum ItemPoolIndex {
			AheadOfOwnBall = 0,
			InTheLead = 1,
			GreaterThan50MetersBehindLead = 2,
			GreaterThan125MetersBehindLead = 3,
			GreaterThan200MetersBehindLead = 4
		}

		public static MatchSetupRules instance_match_setup_rules = null!;

		public static CustomRulesPresetsData custom_rules_presets_data = new CustomRulesPresetsData();
		public struct CustomRulesPresetsData {
			private List<CustomRulesPreset> data;

			public int count() {return data.Count;}

			public int preset_create(string json = "") {
				if (string.IsNullOrEmpty(json)) {
					data.Add(new CustomRulesPreset());
				} else {
					data.Add(new CustomRulesPreset(json));
				}
				return data.Count - 1;
			}

			public Error preset_delete(int preset_index) {
				if (preset_index < 0 || preset_index >= data.Count) {
					Utilities.log_verbose(Utilities.LogType.Error, $"Invalid preset index {preset_index} in CustomRulesPresetsData.preset_delete.");
					return Error.ArgumentOutOfRange;
				}

				data.RemoveAt(preset_index);
				return Error.Success;
			}
			
			public CustomRulesPreset preset_get(int preset_index) {
				if (preset_index < 0 || preset_index >= data.Count) {
					Utilities.log_verbose(Utilities.LogType.Error, $"Invalid preset index {preset_index} in preset_get, returning empty preset...");
					last_preset_get_error = Error.ArgumentOutOfRange;
					return new CustomRulesPreset();
				}

				CustomRulesPreset preset = data[preset_index];
				if (preset.rules_settings == null || preset.item_spawn_chance_weights == null) {
					Utilities.log_verbose(Utilities.LogType.Error, $"Preset at index {preset_index} is not properly initialized, returning empty preset...");
					last_preset_get_error = Error.GenericFailure;
					return new CustomRulesPreset();
				}

				last_preset_get_error = Error.Success;
				return preset;
			}

			public Error preset_set(int preset_index, CustomRulesPreset new_preset) {
				if (preset_index < 0 || preset_index >= data.Count) {
					Utilities.log_verbose(Utilities.LogType.Error, $"Invalid preset index {preset_index} in preset_set.");
					return Error.ArgumentOutOfRange;
				}

				data[preset_index] = new_preset;
				return Error.Success;
			}

			public bool preset_is_empty(int preset_index) {
				if (preset_index < 0 || preset_index >= data.Count) {
					Utilities.log_verbose(Utilities.LogType.Error, $"Invalid preset index {preset_index} in preset_is_empty, returning false...");
					last_preset_get_error = Error.ArgumentOutOfRange;
					return false;
				}
				return preset_get(preset_index).is_empty();
			}

			public CustomRulesPresetsData() {
				data = new List<CustomRulesPreset>();
			}
			
			public CustomRulesPresetsData(List<CustomRulesPreset> presets) {
				if (presets == null) {
					Utilities.log_verbose(Utilities.LogType.Error, "Provided argument 'presets' is null in CustomRulesPresetsData constructor, initializing with empty list...");
					data = new List<CustomRulesPreset>();
				} else {
					data = presets;
				}
			}

			public CustomRulesPresetsData(string json) {
				if (string.IsNullOrEmpty(json)) {
					Utilities.log_verbose(Utilities.LogType.Error, "Provided argument 'json' is null or empty in CustomRulesPresetsData constructor, initializing with empty CustomRulesPreset...");
					data = new List<CustomRulesPreset>();
				} else {
					data = JsonConvert.DeserializeObject<List<CustomRulesPreset>>(json) ?? new List<CustomRulesPreset>();
				}
			}
		}
		public struct CustomRulesPreset {
			public Dictionary<MatchSetupRules.Rule, float> rules_settings;
			public Dictionary<MatchSetupRules.ItemPoolId, float> item_spawn_chance_weights;

			public bool is_empty() {
				return rules_settings.Count == 0 && item_spawn_chance_weights.Count == 0;
			}

			public CustomRulesPreset() {
				rules_settings = new Dictionary<MatchSetupRules.Rule, float>();
				item_spawn_chance_weights = new Dictionary<MatchSetupRules.ItemPoolId, float>();
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
		public static Error last_preset_get_error = Error.Success;
		public static int current_selected_preset_index = -1;

		// Loads the settings from the preset at the provided index into the MatchSetupRules instance.
		public static Error preset_load_settings(int preset_index) {
			if (preset_index < 0 || preset_index >= custom_rules_presets_data.count()) {
				Utilities.log_verbose(Utilities.LogType.Error, $"Invalid preset index {preset_index} in preset_load_settings.");
				return Error.ArgumentOutOfRange;
			} else if (preset_index == current_selected_preset_index) {
				Plugin.Log.LogInfo($"Preset at index {preset_index} is already selected, skipping load.");
				return Error.Success;
			}

			CustomRulesPreset preset = custom_rules_presets_data.preset_get(preset_index);
			if (last_preset_get_error != Error.Success) {
				Utilities.log_verbose(Utilities.LogType.Error, $"Failed to get preset at index {preset_index} in preset_load_settings with error: {last_preset_get_error.ToString()}");
				return Error.GenericFailure;
			}

			if (Utilities.do_log_debug) {
				Utilities.log_verbose(
					Utilities.LogType.Debug,
					$"Loading preset at index {preset_index}. rules: {Utilities.rules_dict_to_json(preset.rules_settings)}, item spawn chance weights: {Utilities.item_spawn_chance_weights_dict_to_json(preset.item_spawn_chance_weights)}"
				);
			};
		
			if (!preset.is_empty()) {
				Error set_rules_error = set_rules(preset.rules_settings);
				Plugin.Log.LogInfo($"Loaded preset {preset_index}'s rules status: {set_rules_error.ToString()}");
				if (set_rules_error != Error.Success) {
					Utilities.log_verbose(Utilities.LogType.Error, $"Failed to load preset {preset_index} due to error in setting rules.");
					return Error.GenericFailure;
				}

				Error set_item_spawn_chance_weights_error = set_item_spawn_chance_weights(preset.item_spawn_chance_weights);
				Plugin.Log.LogInfo($"Loaded preset {preset_index}'s item spawn chance weights status: {set_item_spawn_chance_weights_error.ToString()}");
				if (set_item_spawn_chance_weights_error != Error.Success) {
					Utilities.log_verbose(Utilities.LogType.Error, $"Failed to load preset {preset_index} due to error in setting item spawn chance weights.");
					return Error.GenericFailure;
				}
			} else {
				Utilities.log_verbose(Utilities.LogType.Error, $"Preset at index {preset_index} is empty. Was there a preset save failure?");
				return Error.GenericFailure;
			}

			current_selected_preset_index = preset_index;

			Plugin.Log.LogInfo($"Successfully loaded preset at index {preset_index}.");
			return Error.Success;
		}

		// Saves the current settings from the MatchSetupRules instance into the preset at the provided index.
		public static Error preset_save_settings(int preset_index) {
			if (preset_index < 0 || preset_index >= custom_rules_presets_data.count()) {
				Utilities.log_verbose(Utilities.LogType.Error, $"Invalid preset index {preset_index} in preset_save_settings.");
				return Error.ArgumentOutOfRange;
			} else if (instance_match_setup_rules == null) {
				Utilities.log_verbose(Utilities.LogType.Error, "CustomRulesPresetsManager is not set up properly.");
				return Error.GlobalVariableIsNull;
			}

			CustomRulesPreset preset = custom_rules_presets_data.preset_get(preset_index);
			if (last_preset_get_error != Error.Success) {
				Utilities.log_verbose(Utilities.LogType.Error, $"Failed to get preset at index {preset_index} in preset_save_settings with error: {last_preset_get_error.ToString()}");
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
			if (Utilities.do_log_debug) {Utilities.log_verbose(Utilities.LogType.Debug, $"Saved rules settings for preset at index {preset_index}: {Utilities.rules_dict_to_json(preset.rules_settings)}");};

			Dictionary<ItemPoolId, float> current_item_spawn_chance_weights = new Dictionary<ItemPoolId, float>();
			foreach (KeyValuePair<ItemPoolId, float> id_and_value_kvp in instance_match_setup_rules.spawnChanceWeights) {
				current_item_spawn_chance_weights.Add(id_and_value_kvp.Key, id_and_value_kvp.Value);
			}
			if (current_item_spawn_chance_weights.Count == 0) {
				Utilities.log_verbose(Utilities.LogType.Error, $"Failed to save item spawn chance weights from the MatchSetupRules instance. Source spawnChanceWeights dict == null: {instance_match_setup_rules.spawnChanceWeights == null}");
				return Error.GenericFailure;
			}
			preset.item_spawn_chance_weights = current_item_spawn_chance_weights;
			if (Utilities.do_log_debug) {Utilities.log_verbose(Utilities.LogType.Debug, $"Saved item spawn chance weights for preset at index {preset_index}: {Utilities.item_spawn_chance_weights_dict_to_json(preset.item_spawn_chance_weights)}");};

			Error preset_set_error = custom_rules_presets_data.preset_set(preset_index, preset);
			if (preset_set_error != Error.Success) {
				Utilities.log_verbose(Utilities.LogType.Error, $"Failed to save preset at index {preset_index} with error: {preset_set_error.ToString()}");
				return Error.GenericFailure;
			}
			Plugin.Log.LogInfo($"Successfully saved preset at index {preset_index}.");
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
			if (custom_rules_presets_data.count() == 0) {
				custom_rules_presets_data.preset_create();
			}

			if (Utilities.do_log_debug) {Utilities.log_verbose(Utilities.LogType.Debug, "CustomRulesPresetsManager setup done.");};
			return Error.Success;
		}
	}
}
