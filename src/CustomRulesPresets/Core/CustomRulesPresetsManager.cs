using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using Mirror;
using System;
using System.Collections.Generic;
using System.Reflection;


namespace CustomRulesPresets.Core {
	public static class CustomRulesPresetsManager {

		public static MatchSetupRules? instance_match_setup_rules = null;

		public static FieldInfo? field_match_setup_rules_rules = null;
		public static FieldInfo? field_match_setup_rules_spawn_chance_weights = null;

		public static MethodInfo? method_match_setup_rules_update_rule = null;
		public static MethodInfo? method_match_setup_rules_set_spawn_chance = null;
		public static MethodInfo? method_match_setup_rules_server_update_spawn_chance_value = null;

		public static List<CustomRulesPreset> custom_rules_presets = new List<CustomRulesPreset>();
		public static int current_selected_preset_index = -1;

		public struct CustomRulesPreset {
			public Dictionary<MatchSetupRules.Rule, float> rules_settings;
			public Dictionary<MatchSetupRules.ItemPoolId, float> item_spawn_chance_weights;
		}

		// Creates and returns a new CustomRulesPreset with the current settings from the MatchSetupRules instance.
		public static void preset_create() {
			int new_preset_index = custom_rules_presets.Count;
			custom_rules_presets.Add(new CustomRulesPreset());
			preset_save_settings(new_preset_index);
		}

		// Deletes the preset at the provided index from the custom_rules_presets list.
		public static void preset_delete(int preset_index) {
			if (preset_index < 0 || preset_index >= custom_rules_presets.Count) {
				Plugin.Log.LogError($"Invalid preset index {preset_index} in preset_delete.");
				return;
			} else if (current_selected_preset_index == preset_index) {
				Plugin.Log.LogError($"Cannot delete preset at index {preset_index} because it is currently selected. Load a different preset before deleting this one.");
				return;
			}

			custom_rules_presets.RemoveAt(preset_index);
		}

		// Loads the settings from the preset at the provided index into the MatchSetupRules instance.
		public static void preset_load_settings(int preset_index) {
			if (preset_index < 0 || preset_index >= custom_rules_presets.Count) {
				Plugin.Log.LogError($"Invalid preset index {preset_index} in preset_load_settings.");
				return;
			}
			CustomRulesPreset preset = custom_rules_presets[preset_index];
			set_rules(preset.rules_settings);
			set_item_spawn_chance_weights(preset.item_spawn_chance_weights);
			current_selected_preset_index = preset_index;
		}

		// Saves the current settings from the MatchSetupRules instance into the preset at the provided index.
		public static void preset_save_settings(int preset_index) {
			if (preset_index < 0 || preset_index >= custom_rules_presets.Count) {
				Plugin.Log.LogError($"Invalid preset index {preset_index} in preset_save_settings.");
				return;
			} else if (instance_match_setup_rules == null || method_match_setup_rules_update_rule == null || field_match_setup_rules_rules == null || field_match_setup_rules_spawn_chance_weights == null) {
				Plugin.Log.LogError("CustomRulesPresetsManager is not set up properly.");
				return;
			}

			CustomRulesPreset preset = custom_rules_presets[preset_index];

			object field_rules_value = field_match_setup_rules_rules.GetValue(instance_match_setup_rules);
			if (field_rules_value == null) {
				Plugin.Log.LogError("Failed to get current rules from MatchSetupRules instance.");
				return;
			}

			Dictionary<MatchSetupRules.Rule, float> current_rules_sync_dict = field_rules_value as Dictionary<MatchSetupRules.Rule, float>;
			if (current_rules_sync_dict == null) {
				Plugin.Log.LogError("Current rules from MatchSetupRules instance is not of type SyncDictionary<MatchSetupRules.Rule, float>.");
				return;
			}

			Dictionary<MatchSetupRules.Rule, float> current_rules_settings = new Dictionary<MatchSetupRules.Rule, float>();
			foreach (MatchSetupRules.Rule rule in current_rules_sync_dict.Keys) {
				current_rules_settings.Add(rule, current_rules_sync_dict[rule]);
			}
			preset.rules_settings = current_rules_settings;


			object field_spawn_chance_weights_value = field_match_setup_rules_spawn_chance_weights.GetValue(instance_match_setup_rules);
			if (field_spawn_chance_weights_value == null) {
				Plugin.Log.LogError("Failed to get current spawn chance weights from MatchSetupRules instance.");
				return;
			}

			Dictionary<MatchSetupRules.ItemPoolId, float> current_spawn_chance_weights_sync_dict = field_spawn_chance_weights_value as Dictionary<MatchSetupRules.ItemPoolId, float>;
			if (current_spawn_chance_weights_sync_dict == null) {
				Plugin.Log.LogError("Current spawn chance weights from MatchSetupRules instance is not of type SyncDictionary<ItemPoolId, float>.");
				return;
			}

			Dictionary<MatchSetupRules.ItemPoolId, float> current_item_spawn_chance_weights = new Dictionary<MatchSetupRules.ItemPoolId, float>();
			foreach (MatchSetupRules.ItemPoolId item_pool_id in current_spawn_chance_weights_sync_dict.Keys) {
				current_item_spawn_chance_weights.Add(item_pool_id, current_spawn_chance_weights_sync_dict[item_pool_id]);
			}
			preset.item_spawn_chance_weights = current_item_spawn_chance_weights;
		}

		// Sets the item spawn chance weights in the MatchSetupRules instance with the provided settings.
		public static void set_item_spawn_chance_weights(Dictionary<MatchSetupRules.ItemPoolId, float> item_spawn_chance_weights) {
			if (instance_match_setup_rules == null || field_match_setup_rules_spawn_chance_weights == null || method_match_setup_rules_set_spawn_chance == null || method_match_setup_rules_server_update_spawn_chance_value == null) {
				Plugin.Log.LogError("CustomRulesPresetsManager is not set up properly.");
				return;
			}

			foreach (KeyValuePair<MatchSetupRules.ItemPoolId, float> id_and_weight_kvp in item_spawn_chance_weights) {
				method_match_setup_rules_set_spawn_chance.Invoke(instance_match_setup_rules, new object[] { id_and_weight_kvp.Key.itemPoolIndex, id_and_weight_kvp.Key.itemType, id_and_weight_kvp.Value });
			}

			foreach (MatchSetupRules.ItemPoolId item_pool_id in item_spawn_chance_weights.Keys) {
				method_match_setup_rules_server_update_spawn_chance_value.Invoke(instance_match_setup_rules, new object[] { item_pool_id });
			}
		}

		// Sets the rules in the MatchSetupRules instance with the provided settings.
		public static void set_rules(Dictionary<MatchSetupRules.Rule, float> rules_settings) {
			if (instance_match_setup_rules == null || field_match_setup_rules_rules == null || method_match_setup_rules_update_rule == null) {
				Plugin.Log.LogError("CustomRulesPresetsManager is not set up properly.");
				return;
			}

			Dictionary<MatchSetupRules.Rule, float> new_rules = new Dictionary<MatchSetupRules.Rule, float>();
			field_match_setup_rules_rules.SetValue(instance_match_setup_rules, new_rules);

			foreach (MatchSetupRules.Rule rule in rules_settings.Keys) {
				method_match_setup_rules_update_rule.Invoke(instance_match_setup_rules, new object[] { rule, rules_settings[rule] });
			}
		}

		// Stores the provided MatchSetupRules instance and retrieves the necessary FieldInfo and MethodInfo for later use.
		public static void setup(MatchSetupRules? new_instance_match_setup_rules) {
			if (new_instance_match_setup_rules == null) {
				Plugin.Log.LogError("Failed to set up CustomRulesPresetsManager: the provided instance is null.");
				return;
			}
			instance_match_setup_rules = new_instance_match_setup_rules;

			field_match_setup_rules_rules = typeof(MatchSetupRules).GetField("rules", BindingFlags.NonPublic | BindingFlags.Instance);
			if (field_match_setup_rules_rules == null) {
				Plugin.Log.LogError("Failed to set up CustomRulesPresetsManager: could not find field 'rules' in MatchSetupRules.");
				return;
			}

			field_match_setup_rules_spawn_chance_weights = typeof(MatchSetupRules).GetField("spawnChanceWeights", BindingFlags.NonPublic | BindingFlags.Instance);
			if (field_match_setup_rules_spawn_chance_weights == null) {
				Plugin.Log.LogError("Failed to set up CustomRulesPresetsManager: could not find field 'spawnChanceWeights' in MatchSetupRules.");
				return;
			}

			method_match_setup_rules_update_rule = typeof(MatchSetupRules).GetMethod("UpdateRule", BindingFlags.NonPublic | BindingFlags.Instance);
			if (method_match_setup_rules_update_rule == null) {
				Plugin.Log.LogError("Failed to set up CustomRulesPresetsManager: could not find method 'UpdateRule' in MatchSetupRules.");
				return;
			}

			method_match_setup_rules_set_spawn_chance = typeof(MatchSetupRules).GetMethod("SetSpawnChance", BindingFlags.NonPublic | BindingFlags.Instance);
			if (method_match_setup_rules_set_spawn_chance == null) {
				Plugin.Log.LogError("Failed to set up CustomRulesPresetsManager: could not find method 'SetSpawnChance' in MatchSetupRules.");
				return;
			}

			method_match_setup_rules_server_update_spawn_chance_value = typeof(MatchSetupRules).GetMethod("ServerUpdateSpawnChanceValue", BindingFlags.Public | BindingFlags.Instance);
			if (method_match_setup_rules_server_update_spawn_chance_value == null) {
				Plugin.Log.LogError("Failed to set up CustomRulesPresetsManager: could not find method 'ServerUpdateSpawnChanceValue' in MatchSetupRules.");
				return;
			}
		}
	}
}
