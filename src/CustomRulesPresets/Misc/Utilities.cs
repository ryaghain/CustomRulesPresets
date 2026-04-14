using System;
using System.Collections;
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

        private static object NormalizeGameObjectDictionary(Dictionary<object, object> dict) {
            var result = new Dictionary<string, object>();

            foreach (KeyValuePair<object, object> kvp in dict) {
                string key = GetObjectKey(kvp.Key);
                result[key] = NormalizeValue(kvp.Value);
            }

            return result;
        }

        private static object NormalizeValue(object value) {
            if (value == null)
                return null!;

            // Primitive-like types that serialize cleanly
            Type t = value.GetType();
            if (value is string || value is bool || value is byte || value is sbyte || value is short || value is ushort || value is int || value is uint || value is long || value is ulong || value is float || value is double || value is decimal) {
                return value;
            }

            // Enum -> string
            if (t.IsEnum)
                return value.ToString();

            // GameObject value -> string representation
            if (value is GameObject go)
                return GetObjectKey(go);

            // Nested Dictionary<GameObject, object>
            if (value is Dictionary<object, object> nestedGoDict)
                return NormalizeGameObjectDictionary(nestedGoDict);

            // Generic IDictionary with non-GameObject keys
            if (value is IDictionary dictionary) {
                var result = new Dictionary<string, object>();

                foreach (DictionaryEntry entry in dictionary)
                {
                    string key = entry.Key?.ToString() ?? "null";
                    result[key] = NormalizeValue(entry.Value);
                }

                return result;
            }

            // Lists / arrays
            if (value is IEnumerable enumerable && value is not string) {
                var list = new List<object>();

                foreach (object item in enumerable)
                    list.Add(NormalizeValue(item));

                return list;
            }

            // Common Unity structs
            if (value is Vector2 v2)
                return new Dictionary<string, object> { ["x"] = v2.x, ["y"] = v2.y };

            if (value is Vector3 v3)
                return new Dictionary<string, object> { ["x"] = v3.x, ["y"] = v3.y, ["z"] = v3.z };

            if (value is Vector4 v4)
                return new Dictionary<string, object> { ["x"] = v4.x, ["y"] = v4.y, ["z"] = v4.z, ["w"] = v4.w };

            if (value is Quaternion q)
                return new Dictionary<string, object> { ["x"] = q.x, ["y"] = q.y, ["z"] = q.z, ["w"] = q.w };

            if (value is Color c)
                return new Dictionary<string, object> { ["r"] = c.r, ["g"] = c.g, ["b"] = c.b, ["a"] = c.a };

            // Fallback: stringify unknown objects
            return value.ToString();
        }

        private static string GetObjectKey(object go) {
            if (go == null) {
                return "null";
            } else if (go is GameObject gameObject) {
                return GetHierarchyPath(gameObject.transform);
            } else if (go is MatchSetupRules.ItemPoolId itemPoolId) {
                return $"Item Type: {itemPoolId.itemType.ToString()}, Item Index: {itemPoolId.itemPoolIndex}";
            } else {
                return go.ToString() ?? "null";
            };
        }

        private static string GetHierarchyPath(Transform transform) {
            if (transform == null)
                return "null";

            var names = new Stack<string>();
            Transform current = transform;

            while (current != null) {
                names.Push(current.name);
                current = current.parent;
            }

            return string.Join("/", names);
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
            string json_string = JsonConvert.SerializeObject(get_all_children_and_components(root), Formatting.Indented);

            FileStream file_stream = new FileStream(file_path, FileMode.Create, FileAccess.Write, FileShare.None);
            StreamWriter stream_writer = new StreamWriter(file_stream, new UTF8Encoding(false)); // Encoding.UTF8.GetByteCount(json_string)
            stream_writer.Write(json_string);
            stream_writer.Close();

            log_verbose(LogType.Debug, $"Saved '{root.name}' object tree data to '{file_path}'.");
        }
    }
}
