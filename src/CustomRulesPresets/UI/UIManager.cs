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
	public class UIManager {
		public Error construction_error = Error.Success;

		const string NEW_PRESET_OPTION_TEXT = "New Preset";

		private CustomRulesPresetsManager custom_rules_presets_manager => CustomRulesPresetsPlugin.custom_rules_presets_manager;
		public MatchSetupMenu instance_match_setup_menu = null!;
		public GameObject presets_options = null!;
		public TMP_Dropdown presets_dropdown = null!;
		public int current_selected_preset_index = -1;

		public GameObject InsertDropdownAboveItemProbabilities(GameObject matchSetupMenu, string leftLabel) {
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
			presets_dropdown = clone.transform.Find("Option Contents/Dropdown")?.GetComponent<TMP_Dropdown>()!;
			if (presets_dropdown != null) {
				// This custom component is a common reason the list gets rebuilt.
				var localizeDropdown = presets_dropdown.GetComponent("LocalizeDropdown");
				if (localizeDropdown != null)
					GameObject.Destroy(localizeDropdown);

				presets_dropdown.options.Clear();
				presets_dropdown.options.Add(new TMP_Dropdown.OptionData(NEW_PRESET_OPTION_TEXT));
				presets_dropdown.value = 0;
				current_selected_preset_index = 0;
				presets_dropdown.RefreshShownValue();
				presets_dropdown.onValueChanged.RemoveAllListeners();
				presets_dropdown.onValueChanged.AddListener(index => {HandleDropdownSelection(index);});
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

		public void BindCategoryButtons(GameObject matchSetupMenu,GameObject clonedDropdown) {
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

		private void HandleDropdownSelection(int selected_index) {
			if (selected_index < 0 || selected_index >= presets_dropdown.options.Count)
				return;

			string current_selected_preset_name = presets_dropdown.options[current_selected_preset_index].text;
			int new_preset_index = selected_index;
			string new_preset_name = presets_dropdown.options[selected_index].text;

			Error save_error = custom_rules_presets_manager.preset_save_settings(current_selected_preset_name);
			if (save_error != Error.Success) {
				Utilities.log_verbose(Utilities.LogType.Error, $"Failed to save the current preset {current_selected_preset_name} before switching. Restoring previous dropdown selection...");
				presets_dropdown.SetValueWithoutNotify(current_selected_preset_index);
				presets_dropdown.RefreshShownValue();
				return;
			}
			
			if (new_preset_name == NEW_PRESET_OPTION_TEXT) {
				if (Utilities.do_log_debug) {Utilities.log_verbose(Utilities.LogType.Debug, "New Preset option selected, creating new preset...");}

				new_preset_name = $"Preset {custom_rules_presets_manager.custom_rules_presets_data.get_preset_count() + 1}";
				new_preset_index = presets_dropdown.options.Count - 1;

				Error preset_duplicate_error = custom_rules_presets_manager.custom_rules_presets_data.preset_duplicate(current_selected_preset_name, new_preset_name);
				if (preset_duplicate_error != Error.Success) {
					Utilities.log_verbose(Utilities.LogType.Error, $"Failed to duplicate preset '{current_selected_preset_name}' with error: {preset_duplicate_error.ToString()}");
					presets_dropdown.SetValueWithoutNotify(current_selected_preset_index);
					presets_dropdown.RefreshShownValue();
				} else {
					insert_new_dropdown_option(new_preset_name, new_preset_index);
				}

				return;
			}

			if (!custom_rules_presets_manager.custom_rules_presets_data.has_preset(new_preset_name)) {
				Utilities.log_verbose(Utilities.LogType.Error, $"Selected preset '{new_preset_name}' not found in presets data.");
				presets_dropdown.SetValueWithoutNotify(current_selected_preset_index);
				presets_dropdown.RefreshShownValue();
				return;
			}

			Error load_error = custom_rules_presets_manager.preset_load_settings(new_preset_name);
			if (load_error != Error.Success) {
				Utilities.log_verbose(Utilities.LogType.Error, $"Failed to load preset {new_preset_name}. Restoring previous dropdown selection...");
				presets_dropdown.SetValueWithoutNotify(current_selected_preset_index);
				presets_dropdown.RefreshShownValue();
				return;
			}

			Error save_presets_to_config_error = custom_rules_presets_manager.save_presets_to_file();

			current_selected_preset_index = new_preset_index;
		}

		public Error insert_new_dropdown_option(string option_text, int option_index = -1, bool set_value = true) {
			if (presets_dropdown == null) {
				Utilities.log_verbose(Utilities.LogType.Error, "Cannot insert new dropdown option because presets_dropdown is null.");
				return Error.GlobalVariableIsNull;
			}

			int actual_option_index = option_index == -1 ? presets_dropdown.options.Count - 1 : option_index;
			presets_dropdown.options.Insert(actual_option_index, new TMP_Dropdown.OptionData(option_text));
			if (set_value) {
				presets_dropdown.SetValueWithoutNotify(actual_option_index);
				presets_dropdown.RefreshShownValue();
			}
			return Error.Success;
		}

		public UIManager(MatchSetupMenu new_instance_match_setup_menu) {
			if (new_instance_match_setup_menu == null) {
				Utilities.log_verbose(Utilities.LogType.Error, "UIManager is not set up properly.");
				construction_error = Error.ArgumentNull;
			} else if (custom_rules_presets_manager == null) {
				Utilities.log_verbose(Utilities.LogType.Error, "custom_rules_presets_manager is null");
			} else {
				instance_match_setup_menu = new_instance_match_setup_menu;
				
				if (instance_match_setup_menu.isServer) {
					//Dictionary<GameObject, object> all_children = ChildrenVisualizer.get_all_children_and_components(instance_match_setup_menu.menu);
					//ChildrenVisualizer.SaveJsonToFile(ChildrenVisualizer.ToJson(all_children), "Debug/MatchSetupMenu_all_children.json");

					presets_options = InsertDropdownAboveItemProbabilities(instance_match_setup_menu.menu, "Presets");
					BindCategoryButtons(instance_match_setup_menu.menu, presets_options);
					
					foreach (string preset_name in custom_rules_presets_manager.custom_rules_presets_data.get_preset_names()) {
						Error insert_option_error = insert_new_dropdown_option(preset_name, -1, false);
						if (insert_option_error != Error.Success) {
							Utilities.log_verbose(Utilities.LogType.Error, $"Failed to insert dropdown option for preset '{preset_name}' with error: {insert_option_error.ToString()}");
						}
					}

					int index_to_load = 0;
					string current_selected_preset_name = custom_rules_presets_manager.custom_rules_presets_data.current_selected_preset_name;
					if (custom_rules_presets_manager.custom_rules_presets_data.has_preset(current_selected_preset_name)) {
						int option_count = presets_dropdown.options.Count;
						for (int index = 0; index < option_count; index++) {
							if (presets_dropdown.options[index].text == current_selected_preset_name) {
								index_to_load = index;
								break;
							}
						}

					} else {
						Utilities.log_verbose(Utilities.LogType.Error, $"Could not find preset '{current_selected_preset_name}', defaulting to index 0...");
						current_selected_preset_name = presets_dropdown.options[index_to_load].text;
					}

					Error load_error = custom_rules_presets_manager.preset_load_settings(current_selected_preset_name);
					if (load_error != Error.Success) {
						Utilities.log_verbose(Utilities.LogType.Error, $"Failed to load preset {current_selected_preset_name} during setup, defaulting to index 0...");
						current_selected_preset_index = 0;

						if (index_to_load == 0 || custom_rules_presets_manager.preset_load_settings(presets_dropdown.options[0].text) != Error.Success) {
							Utilities.log_verbose(
								Utilities.LogType.Error,
								$"Failed to load backup preset during setup. I recommend closing the game, delete the file {custom_rules_presets_manager.presets_file_path}, then launch the game. *This will delete all but the current rules settings*."
							);
						}

					} else {
						current_selected_preset_index = index_to_load;
					}

					presets_dropdown.SetValueWithoutNotify(current_selected_preset_index);
					presets_dropdown.RefreshShownValue();
				}

			}
		}
	}
}
