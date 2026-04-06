using System.Collections.Generic;
using Mirror;
using UnityEngine.Assertions;
using static MatchSetupRules;

namespace CustomRulesPresets.Core {
	public static class CustomRulesPresetsManager {

		public static MatchSetupRules instance_match_setup_rules = null!;

		public static List<CustomRulesPreset> custom_rules_presets = new List<CustomRulesPreset>();
		public static int current_selected_preset_index = -1;

		public struct CustomRulesPreset {
			public Dictionary<MatchSetupRules.Rule, float> rules_settings;
			public Dictionary<MatchSetupRules.ItemPoolId, float> item_spawn_chance_weights;

			public CustomRulesPreset() {
				rules_settings = new Dictionary<MatchSetupRules.Rule, float>();
				item_spawn_chance_weights = new Dictionary<MatchSetupRules.ItemPoolId, float>();
			}
		}

		// Creates and returns a new CustomRulesPreset with the current settings from the MatchSetupRules instance.
		public static void preset_create() {
			int new_preset_index = custom_rules_presets.Count;
			custom_rules_presets.Add(new CustomRulesPreset());
			preset_save_settings(new_preset_index);
		}

		public static CustomRulesPreset preset_get(int preset_index) {
			if (preset_index < 0 || preset_index >= custom_rules_presets.Count) {
				Plugin.Log.LogError($"Invalid preset index {preset_index} in preset_get, returning empty preset...");
				return new CustomRulesPreset();
			}

			CustomRulesPreset preset = custom_rules_presets[preset_index];
			if (preset.rules_settings == null || preset.item_spawn_chance_weights == null) {
				Plugin.Log.LogError($"Preset at index {preset_index} is not properly initialized, returning empty preset...");
				return new CustomRulesPreset();
			}

			return preset;
		}
		
		// Deletes the preset at the provided index from the custom_rules_presets list.
		public static Error preset_delete(int preset_index) {
			if (preset_index < 0 || preset_index >= custom_rules_presets.Count) {
				Plugin.Log.LogError($"Invalid preset index {preset_index} in preset_delete.");
				return Error.ArgumentOutOfRange;
			} else if (current_selected_preset_index == preset_index) {
				Plugin.Log.LogError($"Cannot delete preset at index {preset_index} because it is currently selected. Load a different preset before deleting this one.");
				return Error.GenericFailure;
			}

			custom_rules_presets.RemoveAt(preset_index);
			return Error.Success;
		}

		// Loads the settings from the preset at the provided index into the MatchSetupRules instance.
		public static Error preset_load_settings(int preset_index) {
			if (preset_index < 0 || preset_index >= custom_rules_presets.Count) {
				Plugin.Log.LogError($"Invalid preset index {preset_index} in preset_load_settings.");
				return Error.ArgumentOutOfRange;
			} else if (preset_index == current_selected_preset_index) {
				Plugin.Log.LogInfo($"Preset at index {preset_index} is already selected, skipping load.");
				return Error.GenericFailure;
			}

			CustomRulesPreset preset = preset_get(preset_index);
			if (preset.rules_settings.Count == 0 || preset.item_spawn_chance_weights.Count == 0) {
				Plugin.Log.LogError($"Preset at index {preset_index} is not properly initialized.");
				return Error.GenericFailure;
			}

			Error set_rules_error = set_rules(preset.rules_settings);
			Plugin.Log.LogInfo($"Loaded preset {preset_index}'s rules status: {set_rules_error.ToString()}");
			if (set_rules_error != Error.Success) {
				Plugin.Log.LogError($"Failed to load preset {preset_index} due to error in setting rules.");
				return Error.GenericFailure;
			}

			Error set_item_spawn_chance_weights_error = set_item_spawn_chance_weights(preset.item_spawn_chance_weights);
			Plugin.Log.LogInfo($"Loaded preset {preset_index}'s item spawn chance weights status: {set_item_spawn_chance_weights_error.ToString()}");
			if (set_item_spawn_chance_weights_error != Error.Success) {
				Plugin.Log.LogError($"Failed to load preset {preset_index} due to error in setting item spawn chance weights.");
				return Error.GenericFailure;
			}

			current_selected_preset_index = preset_index;
			return set_rules_error;
		}

		// Saves the current settings from the MatchSetupRules instance into the preset at the provided index.
		public static void preset_save_settings(int preset_index) {
			if (preset_index < 0 || preset_index >= custom_rules_presets.Count) {
				Plugin.Log.LogError($"Invalid preset index {preset_index} in preset_save_settings.");
				return;
			} else if (instance_match_setup_rules == null) {
				Plugin.Log.LogError("CustomRulesPresetsManager is not set up properly.");
				return;
			}

			CustomRulesPreset preset = custom_rules_presets[preset_index];

			Dictionary<Rule, float> current_rules_settings = new Dictionary<Rule, float>();
			foreach (KeyValuePair<Rule, float> rule_and_value_kvp in instance_match_setup_rules.rules) {
				current_rules_settings.Add(rule_and_value_kvp.Key, rule_and_value_kvp.Value);
			}
			preset.rules_settings = current_rules_settings;

			Dictionary<ItemPoolId, float> current_item_spawn_chance_weights = new Dictionary<ItemPoolId, float>();
			foreach (KeyValuePair<ItemPoolId, float> id_and_value_kvp in instance_match_setup_rules.spawnChanceWeights) {
				current_item_spawn_chance_weights.Add(id_and_value_kvp.Key, id_and_value_kvp.Value);
			}
			preset.item_spawn_chance_weights = current_item_spawn_chance_weights;
		}

		// Sets the item spawn chance weights in the MatchSetupRules instance with the provided settings.
		public static Error set_item_spawn_chance_weights(Dictionary<MatchSetupRules.ItemPoolId, float> item_spawn_chance_weights) {
			if (instance_match_setup_rules == null) {
				Plugin.Log.LogError("CustomRulesPresetsManager is not set up properly.");
				return Error.GlobalVariableIsNull;
			} else if (item_spawn_chance_weights == null) {
				Plugin.Log.LogError("Provided argument 'item_spawn_chance_weights' is null.");
				return Error.ArgumentNull;
			} else if (item_spawn_chance_weights.Count == 0) {
				Plugin.Log.LogError("Provided argument 'item_spawn_chance_weights' is empty.");
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
				Plugin.Log.LogError("CustomRulesPresetsManager is not set up properly.");
				return Error.GlobalVariableIsNull;
			} else if (rules_settings == null) {
				Plugin.Log.LogError("Provided rules settings are null.");
				return Error.ArgumentNull;
			} else if (rules_settings.Count == 0) {
				Plugin.Log.LogError("Provided rules settings are empty.");
				return Error.ArgumentOutOfRange;
			}

			SyncDictionary<Rule, float> instance_rules_dict = instance_match_setup_rules.rules;
			if (instance_rules_dict == null) {
				Plugin.Log.LogError("Failed to set rules: instance_rules_dict is null.");
				return Error.LocalVariableIsNull;
			}
			instance_rules_dict.Clear();

			foreach (Rule rule in rules_settings.Keys) {
				instance_rules_dict.Add(rule, rules_settings[rule]);
			}

			return Error.Success;
		}

		// Stores the provided MatchSetupRules instance and retrieves the necessary FieldInfo and MethodInfo for later use.
		public static Error setup(MatchSetupRules? new_instance_match_setup_rules) {
			if (new_instance_match_setup_rules == null) {
				Plugin.Log.LogError("Failed to set up CustomRulesPresetsManager: the provided instance is null.");
				return Error.GenericFailure;
			}

			instance_match_setup_rules = new_instance_match_setup_rules;
			if (custom_rules_presets.Count == 0) {
				preset_create();
			}

			Plugin.Log.LogDebug("CustomRulesPresetsManager setup done.");
			return Error.Success;
		}
	}
}
