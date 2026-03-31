using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static MatchSetupMenu;

namespace CustomRulesPresets.Core {
    public static class CustomRulesPresetsManager {
        
        private static MatchSetupRules? match_setup_rules = null;

		private struct CustomRulesPreset {
			Dictionary<MatchSetupRules.Rule, object> rules;
			Dictionary<MatchSetupRules.ItemPoolId, float> item_spawn_weights;
		} 
		public static void Inject(MatchSetupRules match_setup_rules) {

        }

        private static Dictionary<MatchSetupRules.Rule, object> CreateNewPreset(string preset_name) {
			Dictionary<MatchSetupRules.Rule, object> current_rules = new Dictionary<MatchSetupRules.Rule, object>();
			foreach (MatchSetupRules.Rule rule in MatchSetupRules.Rule.GetValues(typeof(MatchSetupRules.Rule))) {
				current_rules.Add(rule, MatchSetupRules.GetValue(rule));
			}

			
			

			if (SteamRemoteStorage.FileExists("Lobby.json")) {
				byte[] lobby_file_bytes = SteamRemoteStorage.FileRead("Lobby.json");
				string lobby_file_json = Encoding.UTF8.GetString(lobby_file_bytes);
				MatchSetupMenu.ServerValues server_values = JsonUtility.FromJson<ServerValues>(lobby_file_json);
			}
			Dictionary<MatchSetupRules.ItemPoolId, float> current_item_spawn_weights = new Dictionary<MatchSetupRules.ItemPoolId, float>();
			foreach (ItemType item_type in ItemType.GetValues(typeof(ItemType))) {
				current_item_spawn_weights.Add(item_type, MatchSetupRules.GetItemSpawnWeight(item_type));
			}

			return current_rules;
		}
	}
}
