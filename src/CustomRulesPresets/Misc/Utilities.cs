using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using System.Text;

namespace CustomRulesPresets {
    public static class Utilities {
        public static bool do_log_debug = false;

        public enum LogType {
            Error,
            Debug
        }

        public static void log_verbose(LogType log_type, string message) {
            switch (log_type) {
                case LogType.Error:
                    CustomRulesPresetsPlugin.Log.LogError($"{message}\nStack trace: {Environment.StackTrace}");
                    break;
                case LogType.Debug:
                    CustomRulesPresetsPlugin.Log.LogDebug(message);
                    break;
                default:
                    CustomRulesPresetsPlugin.Log.LogError($"Invalid log type {log_type.ToString()} when trying to log message: {$"{message}\nStack trace: {Environment.StackTrace}"}");
                    break;
            }
        }

        public static string item_spawn_chance_weights_dict_to_json(Dictionary<MatchSetupRules.ItemPoolId, float> source, bool prettyPrint = true) {
            Dictionary<string, string> normalized_dict = new Dictionary<string, string>();
            foreach (KeyValuePair<MatchSetupRules.ItemPoolId, float> kvp in source) {
                string key = $"Item Type: {kvp.Key.itemType.ToString()}, Item Index: {kvp.Key.itemPoolIndex}";
                normalized_dict[key] = kvp.Value.ToString();
            }
        
            return JsonConvert.SerializeObject(
                normalized_dict,
                prettyPrint ? Formatting.Indented : Formatting.None
            );
        }

        public static string rules_dict_to_json(Dictionary<MatchSetupRules.Rule, float> source, bool prettyPrint = true) {
            Dictionary<string, string> normalized_dict = new Dictionary<string, string>();
            foreach (KeyValuePair<MatchSetupRules.Rule, float> kvp in source) {
                string key = kvp.Key.ToString();
                normalized_dict[key] = kvp.Value.ToString();
            }
        
            return JsonConvert.SerializeObject(
                normalized_dict,
                prettyPrint ? Formatting.Indented : Formatting.None
            );
        }

        public static Dictionary<GameObject, object> get_all_children_and_components(GameObject root, GameObject? parent = null, int root_path_count = 0) {
			Dictionary<GameObject, object> object_data = new Dictionary<GameObject, object>();

            if (parent == null) {
                Dictionary<string, object> root_data = new Dictionary<string, object>();
                string root_path = root.transform.GetFullPath();
                root_data["path"] = root_path;
                root_data["components"] = root.GetComponents<Component>().Select(c => c.GetType().ToString()).ToList();
                root_data["children"] = root.transform.childCount > 0 ? get_all_children_and_components(root, root, root_path.Count() + 1) : new Dictionary<GameObject, object>(); // +1 count to remove the / prefix in child paths
				object_data.Add(root, root_data);
            
            } else {
                for (int index = 0; index < parent.transform.childCount; index++) {
                    var child = parent.transform.GetChild(index).gameObject;
                    Dictionary<string, object> child_data = new Dictionary<string, object>();
                    child_data["path from root"] = child.transform.GetFullPath().Substring(root_path_count);
                    child_data["components"] = child.GetComponents<Component>().Select(c => c.GetType().ToString()).ToList();
                    child_data["children"] = child.transform.childCount > 0 ? get_all_children_and_components(root, child, root_path_count) : new Dictionary<GameObject, object>();
                    object_data.Add(child, child_data);
                }
            }
			return object_data;
		}

        public static void save_tree_info_to_disk(GameObject root) {
            string file_path = CustomRulesPresetsPlugin.cache_path + $"{root.name}_tree.json";

            FileStream file_stream = new FileStream(file_path, FileMode.Create, FileAccess.Write, FileShare.None);
            StreamWriter stream_writer = new StreamWriter(file_stream, new UTF8Encoding(false));
            stream_writer.Write(JsonConvert.SerializeObject(get_all_children_and_components(root), Formatting.Indented));
            stream_writer.Close();

            log_verbose(LogType.Debug, $"Saved '{root.name}' object tree data to '{file_path}'.");
        }
    }
}
