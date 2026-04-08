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
		const string NEW_PRESET_OPTION_TEXT = "New Preset";

		public static MatchSetupMenu instance_match_setup_menu = null!;
		public static GameObject presets_options = null!;
		public static int current_selected_preset_index = -1;


		//[Serializable]
		//public class PresetData {
		//	public string name_text = "Preset ";
		//	public Action<string> on_name_text_changed = null!;
		//	public Action on_save_icon_click = null!;
		//	public Action on_load_icon_click = null!;
		//	public Action on_delete_icon_click = null!;
		//}
		//public static List<PresetData> preset_entries = new List<PresetData>();

		public static GameObject InsertDropdownAboveItemProbabilities(GameObject matchSetupMenu, string leftLabel, string firstOptionText) {
			// Source widget to clone
			Transform source = matchSetupMenu.transform.Find("Menu/Background/Match Setup/Columns/Mode/Dropdown Option Variant");

			if (source == null) {
				Utilities.log_verbose(Utilities.LogType.Error, "Source dropdown option not found.");
				return null!;
			}

			// Destination layout container in Rules page
			Transform content = matchSetupMenu.transform.Find("Menu/Background/Rules/Rules/Scroll View/Viewport/Content");

			if (content == null) {
				Utilities.log_verbose(Utilities.LogType.Error, "Rules content container not found.");
				return null!;
			}

			// Anchor object we want to insert before
			Transform itemProbabilities = content.Find("Item probabilities");
			if (itemProbabilities == null) {
				Utilities.log_verbose(Utilities.LogType.Error, "'Item probabilities' section not found.");
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
				// This custom component is a common reason the list gets rebuilt.
				var localizeDropdown = dropdown.GetComponent("LocalizeDropdown");
				if (localizeDropdown != null)
					GameObject.Destroy(localizeDropdown);

				dropdown.options.Clear();
				dropdown.options.Add(new TMP_Dropdown.OptionData(firstOptionText));
				dropdown.options.Add(new TMP_Dropdown.OptionData(NEW_PRESET_OPTION_TEXT));
				dropdown.value = 0;
				current_selected_preset_index = 0;
				dropdown.RefreshShownValue();
				dropdown.onValueChanged.RemoveAllListeners();
				dropdown.onValueChanged.AddListener(index => {HandleDropdownSelection(dropdown, index);});
			} else {
				Utilities.log_verbose(Utilities.LogType.Error, "Dropdown component not found in cloned widget.");
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

		public static void BindCategoryButtons(GameObject matchSetupMenu,GameObject clonedDropdown) {
			Transform categories = matchSetupMenu.transform.Find("Menu/Background/Rules/Header/Categories");

			if (categories == null) {
				Utilities.log_verbose(Utilities.LogType.Error, "Could not find Rules/Header/Categories.");
				return;
			}

			if (clonedDropdown == null) {
				Utilities.log_verbose(Utilities.LogType.Error, "clonedDropdown is null.");
				return;
			}

			bool show = false;

			for (int i = 0; i < categories.childCount; i++) {
				Transform category = categories.GetChild(i);

				Button button = category.GetComponent<Button>();
				Transform selection = category.Find("Selection");
				TMP_Text text = category.Find("Text (TMP)")?.GetComponent<TMP_Text>()!;

				if (button == null || text == null)
					continue;

				string label = text.text.Trim();

				if (label == "Custom") {
					button.onClick.AddListener(() => {clonedDropdown.SetActive(true);});
				} else if (label == "Classic" || label == "Pro Golf") {
					button.onClick.AddListener(() => {clonedDropdown.SetActive(false);});
				}

				if (selection != null && selection.gameObject.activeSelf && text != null && text.text.Trim() == "Custom") {
                	show = true;
           		}
			}

			clonedDropdown.SetActive(show);
		}

		private static void HandleDropdownSelection(TMP_Dropdown dropdown, int selected_index) {
			if (selected_index < 0 || selected_index >= dropdown.options.Count)
				return;

			int new_preset_index = selected_index;
			string selectedText = dropdown.options[selected_index].text;

			if (selectedText == NEW_PRESET_OPTION_TEXT) {
				dropdown.options.Insert(dropdown.options.Count - 1, new TMP_Dropdown.OptionData($"Preset {dropdown.options.Count}"));
				new_preset_index = CustomRulesPresetsManager.custom_rules_presets_data.preset_create();

				dropdown.SetValueWithoutNotify(selected_index);
				dropdown.RefreshShownValue();
			}
			Error save_error = CustomRulesPresetsManager.preset_save_settings(current_selected_preset_index);
			if (save_error != Error.Success) {
				Utilities.log_verbose(Utilities.LogType.Error, $"Failed to save the current preset at index {current_selected_preset_index} before switching. Restoring previous dropdown selection...");
				dropdown.SetValueWithoutNotify(current_selected_preset_index);
				dropdown.RefreshShownValue();
				return;
			}

			Error load_error = CustomRulesPresetsManager.preset_load_settings(new_preset_index);
			if (load_error != Error.Success) {
				Utilities.log_verbose(Utilities.LogType.Error, $"Failed to load preset at index {new_preset_index}. Restoring previous dropdown selection...");
				dropdown.SetValueWithoutNotify(current_selected_preset_index);
				dropdown.RefreshShownValue();
				return;
			}

			current_selected_preset_index = new_preset_index;
		}

		public static void reset() {
			instance_match_setup_menu = null!;
			presets_options = null!;
			current_selected_preset_index = -1;
			//preset_entries.Clear();
			if (Utilities.do_log_debug) {Utilities.log_verbose(Utilities.LogType.Debug, "UIManager has been reset.");};
		}

		public static Error setup(MatchSetupMenu new_instance_match_setup_menu) {
			if (new_instance_match_setup_menu == null) {
				Utilities.log_verbose(Utilities.LogType.Error, "UIManager is not set up properly.");
				return Error.GenericFailure;
			}
			instance_match_setup_menu = new_instance_match_setup_menu;
			if (instance_match_setup_menu.isServer) {
				//Dictionary<GameObject, object> all_children = ChildrenVisualizer.get_all_children_and_components(instance_match_setup_menu.menu);
				//ChildrenVisualizer.SaveJsonToFile(ChildrenVisualizer.ToJson(all_children), "Debug/MatchSetupMenu_all_children.json");

				presets_options = InsertDropdownAboveItemProbabilities(instance_match_setup_menu.menu, "Presets", "Preset 1");
				BindCategoryButtons(instance_match_setup_menu.menu, presets_options);
			}

			if (Utilities.do_log_debug) {Utilities.log_verbose(Utilities.LogType.Debug, "Setting up CustomRulesPresetsManager...");};
			Error setup_error_code = CustomRulesPresetsManager.setup(instance_match_setup_menu.rules);
			if (Utilities.do_log_debug) {Utilities.log_verbose(Utilities.LogType.Debug, $"CustomRulesPresetsManager exited setup with error: {setup_error_code.ToString()}");};

			return Error.Success;
		}
	}
}
