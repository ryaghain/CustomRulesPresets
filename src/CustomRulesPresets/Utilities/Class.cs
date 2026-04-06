using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using System.Runtime.CompilerServices;

namespace CustomRulesPresets {
    public static class Utilities {
        public static bool do_log_debug = false;

        public enum LogType {
            Error,
            Debug
        }

        public static void log_verbose(LogType log_type, string message, [CallerMemberName] string member_name = "", [CallerLineNumber] int line_number = 0) {
            switch (log_type) {
                case LogType.Error:
                    Plugin.Log.LogError($"{member_name} at line {line_number}: {message}");
                    break;
                case LogType.Debug:
                    Plugin.Log.LogDebug($"{member_name} at line {line_number}: {message}");
                    break;
                default:
                    Plugin.Log.LogInfo(message);
                    break;
            }
        }

        public static void SaveJsonToFile(string json, string filePath) {
            string directory = Path.GetDirectoryName(filePath);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(filePath, json);
        }

        public static string ToJson(Dictionary<GameObject, object> source, bool prettyPrint = true) {
            object normalized_dict = NormalizeGameObjectDictionary(source);
        
            return JsonConvert.SerializeObject(
                normalized_dict,
                prettyPrint ? Formatting.Indented : Formatting.None
            );
        }

        private static object NormalizeGameObjectDictionary(Dictionary<GameObject, object> dict) {
            var result = new Dictionary<string, object>();

            foreach (KeyValuePair<GameObject, object> kvp in dict) {
                string key = GetGameObjectKey(kvp.Key);
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
                return GetGameObjectKey(go);

            // Nested Dictionary<GameObject, object>
            if (value is Dictionary<GameObject, object> nestedGoDict)
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

        private static string GetGameObjectKey(GameObject go) {
            if (go == null)
                return "null";

            return GetHierarchyPath(go.transform);
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

        public static Dictionary<GameObject, object> get_all_children_and_components(GameObject parent) {
			Dictionary<GameObject, object> children = new Dictionary<GameObject, object>();
			for (int index = 0; index < parent.transform.childCount; index++) {
				var child = parent.transform.GetChild(index).gameObject;
                Dictionary<string, object> child_data = new Dictionary<string, object>();
                child_data["components"] = child.GetComponents<Component>().Select(c => c.GetType().ToString()).ToList();
                child_data["children"] = child.transform.childCount > 0 ? get_all_children_and_components(child) : new Dictionary<GameObject, object>();
				children.Add(child, child_data);
			}
			return children;
		}

		//public static Dictionary<GameObject, object> recursive_get_children(GameObject parent) {
		//	Dictionary<GameObject, object> children = new Dictionary<GameObject, object>();
		//	for (int index = 0; index < parent.transform.childCount; index++) {
		//		var child = parent.transform.GetChild(index).gameObject;
		//		children.Add(child, child.transform.childCount > 0 ? recursive_get_children(child) : new Dictionary<GameObject, object>());
		//	}
		//	return children;
		//}
    }
}
