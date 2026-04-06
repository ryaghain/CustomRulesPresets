using CustomRulesPresets.Core;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization.Components;

namespace CustomRulesPresets.UI {
	public static class UIManager {
		public static MatchSetupMenu instance_match_setup_menu = null!;
		public static GameObject presets_options = null!;
		public static UnityEngine.Events.UnityAction on_presets_tab_click = null!;

		[Serializable]
		public class PresetData {
			public string name_text = "Preset ";
			public Action<string> on_name_text_changed = null!;
			public Action on_save_icon_click = null!;
			public Action on_load_icon_click = null!;
			public Action on_delete_icon_click = null!;
		}
		public static List<PresetData> preset_entries = new List<PresetData>();

		public static GameObject InsertDropdownAboveItemProbabilities(GameObject matchSetupMenu, string leftLabel, string firstOptionText) {
			// Source widget to clone
			Transform source = matchSetupMenu.transform.Find("Menu/Background/Match Setup/Columns/Mode/Dropdown Option Variant");

			if (source == null) {
				Debug.LogError("Source dropdown option not found.");
				return null!;
			}

			// Destination layout container in Rules page
			Transform content = matchSetupMenu.transform.Find("Menu/Background/Rules/Rules/Scroll View/Viewport/Content");

			if (content == null) {
				Debug.LogError("Rules content container not found.");
				return null!;
			}

			// Anchor object we want to insert before
			Transform itemProbabilities = content.Find("Item probabilities");
			if (itemProbabilities == null) {
				Debug.LogError("'Item probabilities' section not found.");
				return null!;
			}

			// Clone and parent into Rules content
			GameObject clone = GameObject.Instantiate(source.gameObject, content);
			clone.name = leftLabel + " Dropdown";

			// Place directly above "Item probabilities"
			clone.transform.SetSiblingIndex(itemProbabilities.GetSiblingIndex());

			// Optional: normalize transform for layout-driven UI
			RectTransform cloneRt = clone.GetComponent<RectTransform>();
			cloneRt.localScale = Vector3.one;
			cloneRt.localRotation = Quaternion.identity;

			// Change left label text
			TMP_Text labelText = clone.transform.Find("Label Text")?.GetComponent<TMP_Text>()!;
			if (labelText != null) {
				var localize = labelText.GetComponent<LocalizeStringEvent>();
				if (localize != null)
					GameObject.Destroy(localize);

				labelText.text = leftLabel;
			}

			// Configure dropdown options
			TMP_Dropdown dropdown = clone.transform.Find("Option Contents/Dropdown")?.GetComponent<TMP_Dropdown>()!;
			if (dropdown != null) {
				dropdown.options.Clear();
				dropdown.options.Add(new TMP_Dropdown.OptionData(firstOptionText));
				dropdown.options.Add(new TMP_Dropdown.OptionData("New"));
				dropdown.value = 0;
				dropdown.RefreshShownValue();
			} else {
				Plugin.Log.LogError("Dropdown component not found in cloned widget.");
			}

			// Strongly recommended: add a LayoutElement so it sizes like surrounding rows
			LayoutElement le = clone.GetComponent<LayoutElement>();
			if (le == null)
				le = clone.AddComponent<LayoutElement>();

			// These values may need tweaking to match nearby dropdown rows
			le.minHeight = 44f;
			le.preferredHeight = 44f;
			le.flexibleHeight = 0f;

			// Force relayout
			LayoutRebuilder.ForceRebuildLayoutImmediate(content as RectTransform);

			return clone;
		}

		private static void SetupDynamicDropdown(TMP_Dropdown dropdown, string firstOptionText) {
			dropdown.options = new List<TMP_Dropdown.OptionData> {new TMP_Dropdown.OptionData(firstOptionText), new TMP_Dropdown.OptionData("New")};

			dropdown.value = 0;
			dropdown.RefreshShownValue();

			dropdown.onValueChanged.RemoveAllListeners();
			dropdown.onValueChanged.AddListener(index => {HandleDropdownSelection(dropdown, index);});
		}

		private static void HandleDropdownSelection(TMP_Dropdown dropdown, int selectedIndex) {
			if (selectedIndex < 0 || selectedIndex >= dropdown.options.Count)
				return;

			string selectedText = dropdown.options[selectedIndex].text;

			if (selectedText != "New")
				return;

			// Example generated name; change to your own naming scheme
			string generatedName = $"Preset {dropdown.options.Count	- 1}";

			InsertBeforeNew(dropdown, generatedName);

			// Select the newly inserted item
			int newIndex = dropdown.options.Count - 2;
			dropdown.SetValueWithoutNotify(newIndex);
			dropdown.RefreshShownValue();
		}

		public static void InsertBeforeNew(TMP_Dropdown dropdown, string text) {
			int newIndex = Mathf.Max(0, dropdown.options.Count - 1);
			dropdown.options.Insert(newIndex, new TMP_Dropdown.OptionData(text));
			dropdown.RefreshShownValue();
		}

		public static void reset() {
			instance_match_setup_menu = null!;
			preset_entries.Clear();
			Plugin.Log.LogDebug("UIManager has been reset.");
		}

		public static int setup(MatchSetupMenu new_instance_match_setup_menu) {
			if (new_instance_match_setup_menu == null) {
				Plugin.Log.LogError("UIManager is not set up properly.");
				return 1;
			}
			instance_match_setup_menu = new_instance_match_setup_menu;
			if (instance_match_setup_menu.isServer) {
				//Dictionary<GameObject, object> all_children = ChildrenVisualizer.get_all_children_and_components(instance_match_setup_menu.menu);
				//ChildrenVisualizer.SaveJsonToFile(ChildrenVisualizer.ToJson(all_children), "Debug/MatchSetupMenu_all_children.json");

				presets_options = InsertDropdownAboveItemProbabilities(instance_match_setup_menu.menu, "Presets", "Preset 1");
			}

			Plugin.Log.LogDebug("Setting up CustomRulesPresetsManager...");
			int setup_error_code = CustomRulesPresetsManager.setup(instance_match_setup_menu.rules);
			Plugin.Log.LogDebug($"CustomRulesPresetsManager exted setup with code: {setup_error_code}");

			return 0;
		}
	}
}
