using CustomRulesPresets.Core;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization.Components;
using System.Collections.Generic;

namespace CustomRulesPresets.UI {
	public class ActiveStateListener : MonoBehaviour {
		public Action<bool>? OnActiveChanged;

		private void OnEnable() {
			OnActiveChanged?.Invoke(true);
		}

		private void OnDisable() {
			OnActiveChanged?.Invoke(false);
		}
	}

	public class UIManager {
		public Error construction_error = Error.Success;

		const string NEW_PRESET_OPTION_TEXT = "New Preset";

		private CustomRulesPresetsManager custom_rules_presets_manager => CustomRulesPresetsPlugin.custom_rules_presets_manager;
		public MatchSetupMenu instance_match_setup_menu = null!;

		public GameObject presets_header_container = null!;
		public GameObject presets_top_row_container = null!;
		public GameObject presets_bottom_row_container = null!;

		public TMP_InputField preset_rename_input_field = null!;
		
		public TMP_Dropdown presets_dropdown = null!;
		public bool presets_is_updating = false;
		public int current_selected_preset_index = -1;

		public void add_listeners_to_category_buttons() {
			if (instance_match_setup_menu == null) {
				Utilities.log_verbose(Utilities.LogType.Error, "Cannot add listeners to category buttons because 'instance_match_setup_menu' is null.");
				return;
			}

			string vanilla_presets_path = "Menu/Background/Rules/Header/Presets";
			Transform vanilla_presets = instance_match_setup_menu.menu.transform.Find(vanilla_presets_path);

			if (vanilla_presets == null) {
				Utilities.log_verbose(Utilities.LogType.Error, $"Could not find '{vanilla_presets_path}'.");
				return;
			}

			if (presets_header_container == null) {
				Utilities.log_verbose(Utilities.LogType.Error, "'presets_header_container' is null.");
				return;
			}

			if (presets_top_row_container == null) {
				Utilities.log_verbose(Utilities.LogType.Error, "'presets_top_row_container' is null.");
				return;
			}

			if (presets_bottom_row_container == null) {
				Utilities.log_verbose(Utilities.LogType.Error, "'presets_bottom_row_container' is null.");
				return;
			}

			bool show = false;

			for (int i = 0; i < vanilla_presets.childCount; i++) {
				Transform vanilla_preset = vanilla_presets.GetChild(i);

				Button button = vanilla_preset.GetComponent<Button>();
				Transform selection = vanilla_preset.Find("Selection");
				TMP_Text text = vanilla_preset.Find("Text (TMP)")?.GetComponent<TMP_Text>()!;

				if (button == null || text == null)
					continue;

				string label = text.text.Trim();

				if (label == "Custom") {
					button.onClick.AddListener(() => {
						presets_header_container.SetActive(true);
						presets_top_row_container.SetActive(true);
						presets_bottom_row_container.SetActive(true);
					});
				} else if (label == "Classic" || label == "Pro Golf") {
					button.onClick.AddListener(() => {
						presets_header_container.SetActive(false);
						presets_top_row_container.SetActive(false);
						presets_bottom_row_container.SetActive(false);
					});
				}

				if (selection != null && selection.gameObject.activeSelf && text != null && text.text.Trim() == "Custom") {
                	show = true;
           		}
			}

			presets_header_container.SetActive(show);
			presets_top_row_container.SetActive(show);
			presets_bottom_row_container.SetActive(show);
		}

		public Error clone_and_insert_ui_elements() { // objects start at line 11672
			// Destination layout container in Rules page
			string content_path = "Menu/Background/Rules/Rules/Scroll View/Viewport/Content";
			Transform content_transform = instance_match_setup_menu.menu.transform.Find(content_path);
			if (content_transform == null) {
				Utilities.log_verbose(Utilities.LogType.Error, $"Unable to find object '{content_path}'.");
				return Error.ObjectNotFound;
			}

			// Source for the title header for the presets section
			string time_header_path = "Time";
			Transform source_time_header_container_transform = content_transform.Find(time_header_path);
			if (source_time_header_container_transform == null) {
				Utilities.log_verbose(Utilities.LogType.Error, $"Unable to find object '{time_header_path}'.");
				return Error.ObjectNotFound;
			}

			// Source for the top row of the presets section
			string time_rules_top_row_path = "Time Rules";
			Transform source_time_rules_top_row_container_transform = content_transform.Find(time_rules_top_row_path);
			if (source_time_rules_top_row_container_transform == null) {
				Utilities.log_verbose(Utilities.LogType.Error, $"Unable to find object '{time_rules_top_row_path}'.");
				return Error.ObjectNotFound;
			}

			// Source for the bottom row of the presets section
			string time_rules_bottom_row_path = "Time Rules (1)";
			Transform source_time_rules_bottom_row_container_transform = content_transform.Find(time_rules_bottom_row_path);
			if (source_time_rules_bottom_row_container_transform == null) {
				Utilities.log_verbose(Utilities.LogType.Error, $"Unable to find object '{time_rules_bottom_row_path}'.");
				return Error.ObjectNotFound;
			}

			// Source for the delete preset button
			string reset_container_path = "Reset Container";
			Transform source_reset_container_transform = content_transform.Find(reset_container_path);
			if (source_reset_container_transform == null) {
				Utilities.log_verbose(Utilities.LogType.Error, $"Unable to find object '{reset_container_path}'.");
				return Error.ObjectNotFound;
			}

			// Source for the rename preset button
			string server_name_container_path = "Menu/Background/Match Setup/Header/Server Name";
			Transform source_server_name_container_transform = instance_match_setup_menu.menu.transform.Find(server_name_container_path);
			if (source_server_name_container_transform == null) {
				Utilities.log_verbose(Utilities.LogType.Error, $"Unable to find object '{server_name_container_path}'.");
				return Error.ObjectNotFound;
			}

			// Clone and setup the presets header
			presets_header_container = GameObject.Instantiate(source_time_header_container_transform.gameObject, content_transform);
			presets_header_container.transform.SetSiblingIndex(0);
			reset_styling_to_enabled(presets_header_container);
			presets_header_container.name = "PresetsHeaderContainer";
			var presets_header_container_localization = presets_header_container.GetComponent<LocalizeStringEvent>();
			if (presets_header_container_localization != null) {GameObject.Destroy(presets_header_container_localization);}
			TMP_Text presets_header_container_text = presets_header_container.GetComponent<TMP_Text>()!;
			if (presets_header_container_text == null) {
				Utilities.log_verbose(Utilities.LogType.Error, $"Unable to find the text component of '{presets_header_container}'.");
				return Error.ObjectNotFound;
			}
			presets_header_container_text.text = "Presets";

			// Clone and setup the top row
			presets_top_row_container = GameObject.Instantiate(source_time_rules_top_row_container_transform.gameObject, content_transform);
			presets_top_row_container.transform.SetSiblingIndex(1);
			presets_top_row_container.name = "PresetsTopRowContainer";
			for (int child_index = presets_top_row_container.transform.childCount - 1; child_index >= 0; child_index--) {
    			GameObject.Destroy(presets_top_row_container.transform.GetChild(child_index).gameObject);
			}

			// Clone and setup the preset rename field
			GameObject preset_rename_container = GameObject.Instantiate(source_server_name_container_transform.gameObject, presets_top_row_container.transform);
			reset_styling_to_enabled(preset_rename_container);
			preset_rename_container.name = "PresetRenameContainer";
			preset_rename_input_field = preset_rename_container.GetComponent<TMP_InputField>();
			preset_rename_input_field.name = "PresetRenameInputField";
			preset_rename_input_field.onValueChanged.RemoveAllListeners();
        	preset_rename_input_field.onEndEdit.RemoveAllListeners();
        	preset_rename_input_field.onSubmit.RemoveAllListeners();
        	preset_rename_input_field.onSelect.RemoveAllListeners();
        	preset_rename_input_field.onDeselect.RemoveAllListeners();
			preset_rename_input_field.SetTextWithoutNotify("Preset 1");
			preset_rename_input_field.onSubmit.AddListener(text => {handle_preset_renaming(text);});

			// Clone and setup the preset delete button
			GameObject preset_delete_container = GameObject.Instantiate(source_reset_container_transform.gameObject, presets_top_row_container.transform);
			reset_styling_to_enabled(preset_delete_container);
			preset_delete_container.name = "PresetDeleteContainer";
			Transform preset_delete_button_container_transform = preset_delete_container.transform.Find("Option Container/Reset Probabilites");
			preset_delete_button_container_transform.gameObject.name = "PresetDeleteButtonContainer";
			Button preset_delete_button = preset_delete_button_container_transform.gameObject.GetComponent<Button>();
			preset_delete_button.onClick.RemoveAllListeners();
			preset_delete_button.onClick.AddListener(() => {handle_preset_delete();});
			GameObject preset_delete_button_text_object = preset_delete_button_container_transform.Find("Text").gameObject;
			LocalizeStringEvent preset_delete_button_text_localization = preset_delete_button_text_object.GetComponent<LocalizeStringEvent>();
			if (preset_delete_button_text_localization != null) {GameObject.Destroy(preset_delete_button_text_localization);}
			TextMeshProUGUI preset_delete_button_text = preset_delete_button_text_object.GetComponent<TextMeshProUGUI>();
			preset_delete_button_text.text = "Delete Preset";


			// Tried fixing the streching issue of the rename field with this next part but it didn't work so whatever it looks fine as-is.

			// Now that both the preset rename field and preset delete button are setup, we can copy the styling from the latter to the former.
			//RectTransform preset_rename_rect_transform = preset_rename_container.GetComponent<RectTransform>();
			//RectTransform preset_delete_rect_transform = preset_delete_container.GetComponent<RectTransform>();
			//preset_rename_rect_transform.anchorMin = preset_delete_rect_transform.anchorMin;
        	//preset_rename_rect_transform.anchorMax = preset_delete_rect_transform.anchorMax;
        	//preset_rename_rect_transform.pivot = preset_delete_rect_transform.pivot;
        	//preset_rename_rect_transform.anchoredPosition = preset_delete_rect_transform.anchoredPosition;
        	//preset_rename_rect_transform.sizeDelta = preset_delete_rect_transform.sizeDelta;
        	//preset_rename_rect_transform.offsetMin = preset_delete_rect_transform.offsetMin;
        	//preset_rename_rect_transform.offsetMax = preset_delete_rect_transform.offsetMax;
        	//preset_rename_rect_transform.localScale = Vector3.one;
        	//preset_rename_rect_transform.localRotation = Quaternion.identity;
			//LayoutElement preset_rename_layout_element = preset_rename_container.AddComponent<LayoutElement>();
			//LayoutElement preset_delete_layout_element = preset_delete_container.GetComponent<LayoutElement>();
			//preset_rename_layout_element.minWidth = preset_delete_layout_element.minWidth;
            //preset_rename_layout_element.minHeight = preset_delete_layout_element.minHeight;
            //preset_rename_layout_element.preferredWidth = preset_delete_layout_element.preferredWidth;
            //preset_rename_layout_element.preferredHeight = preset_delete_layout_element.preferredHeight;
            //preset_rename_layout_element.flexibleWidth = preset_delete_layout_element.flexibleWidth;
            //preset_rename_layout_element.flexibleHeight = preset_delete_layout_element.flexibleHeight;
            //preset_rename_layout_element.layoutPriority = preset_delete_layout_element.layoutPriority;
			//RectTransform preset_rename_input_field_icon_rect_transform = preset_rename_input_field.transform.Find("Icon").GetComponent<RectTransform>();
			//preset_rename_input_field_icon_rect_transform.anchorMin = new Vector2(1f, 0.5f);
            //preset_rename_input_field_icon_rect_transform.anchorMax = new Vector2(1f, 0.5f);
            //preset_rename_input_field_icon_rect_transform.pivot = new Vector2(1f, 0.5f);
            //preset_rename_input_field_icon_rect_transform.anchoredPosition = new Vector2(-8f, 0f);
            //preset_rename_input_field_icon_rect_transform.sizeDelta = new Vector2(36f, 36f); // maybe this instead: preset_rename_rect_transform.sizeDelta * 0.75f;

			// Clone and setup the bottom row
			presets_bottom_row_container = GameObject.Instantiate(source_time_rules_bottom_row_container_transform.gameObject, content_transform);
			presets_bottom_row_container.transform.SetSiblingIndex(2);
			presets_bottom_row_container.name = "PresetsBottomRowContainer";
			
			// Setup the preset dropdown
			Transform presets_dropdown_container_transform = presets_bottom_row_container.transform.Find("Max Time Based On Par");
			reset_styling_to_enabled(presets_dropdown_container_transform.gameObject);
			presets_dropdown_container_transform.gameObject.name = "PresetsDropdown";
			Transform presets_dropdown_subcontainer_transform = presets_dropdown_container_transform.Find("Max Time Based On Par Dropdown/Option Contents/Dropdown");
			presets_dropdown = presets_dropdown_subcontainer_transform.GetComponent<TMP_Dropdown>();
			GameObject.Destroy(presets_dropdown_subcontainer_transform.GetComponent("LocalizeDropdown"));
			presets_dropdown.options.Clear();
			presets_dropdown.options.Add(new TMP_Dropdown.OptionData(NEW_PRESET_OPTION_TEXT));
			presets_dropdown.value = 0;
			current_selected_preset_index = 0;
			presets_dropdown.RefreshShownValue();
			presets_dropdown.onValueChanged.RemoveAllListeners();
			presets_dropdown.onValueChanged.AddListener(index => {handle_selected_dropdown_option(index);});
			TMP_Text presets_dropdown_label_text = presets_dropdown_container_transform.Find("Max Time Based On Par Dropdown/Label Text")?.GetComponent<TMP_Text>()!;
			var localize = presets_dropdown_label_text.GetComponent<LocalizeStringEvent>();
			if (localize != null) {GameObject.Destroy(localize);}
			presets_dropdown_label_text.text = "Change Preset";

			// Force relayout
			LayoutRebuilder.ForceRebuildLayoutImmediate(presets_header_container.GetComponent<RectTransform>());
			
			LayoutRebuilder.ForceRebuildLayoutImmediate(preset_rename_container.GetComponent<RectTransform>());
			LayoutRebuilder.ForceRebuildLayoutImmediate(preset_delete_container.GetComponent<RectTransform>());
			LayoutRebuilder.ForceRebuildLayoutImmediate(presets_top_row_container.GetComponent<RectTransform>());

			LayoutRebuilder.ForceRebuildLayoutImmediate(presets_dropdown_container_transform.GetComponent<RectTransform>());
			LayoutRebuilder.ForceRebuildLayoutImmediate(presets_bottom_row_container.GetComponent<RectTransform>());

			LayoutRebuilder.ForceRebuildLayoutImmediate(content_transform.GetComponent<RectTransform>());
			
			add_listeners_to_category_buttons();
			return Error.Success;
		}

		private void handle_preset_delete() {
			if (current_selected_preset_index == presets_dropdown.options.Count - 1) {
				if (Utilities.do_log_debug) {Utilities.log_verbose(Utilities.LogType.Debug, "Preset delete button click detected, but 'New Preset' is selected.");}
				return;
			}

			if (presets_dropdown.options.Count < 3) {
				if (Utilities.do_log_debug) {Utilities.log_verbose(Utilities.LogType.Debug, "Preset delete button click detected, but no preset to switch to, after deleting, exists. Create another preset first.");}
				return;
			}

			if (presets_is_updating) {
				if (Utilities.do_log_debug) {Utilities.log_verbose(Utilities.LogType.Debug, "Preset delete button click detected, but presets are still updating.");}
				return;
			}

			int new_preset_index = current_selected_preset_index > 0 ? current_selected_preset_index - 1 : current_selected_preset_index + 1;
			Error delete_preset_error = custom_rules_presets_manager.custom_rules_presets_data.preset_delete(presets_dropdown.options[current_selected_preset_index].text);
			if (delete_preset_error != Error.Success) {
				Utilities.log_verbose(Utilities.LogType.Error, $"Unable to delete preset at dropdown index '{current_selected_preset_index}'.");
				return;	
			}

			presets_dropdown.options.RemoveAt(current_selected_preset_index);
			custom_rules_presets_manager.preset_load_settings(presets_dropdown.options[new_preset_index].text);
			update_values(new_preset_index);
		}
		
		private void handle_preset_renaming(string new_preset_name) {
			if (string.IsNullOrEmpty(new_preset_name)) {
				Utilities.log_verbose(Utilities.LogType.Error, "Provided new preset name is null or empty.");
				return;
			}

			string old_preset_name = presets_dropdown.options[current_selected_preset_index].text;
			new_preset_name = new_preset_name.Trim();
			Error preset_rename_error = custom_rules_presets_manager.custom_rules_presets_data.preset_rename(old_preset_name, new_preset_name);
			if (preset_rename_error != Error.Success) {
				Utilities.log_verbose(Utilities.LogType.Error, $"Unable to rename preset '{old_preset_name}' to '{new_preset_name}'.");
				update_values(current_selected_preset_index);
				return;

			} else {
				presets_dropdown.options[current_selected_preset_index].text = new_preset_name;
				update_values(current_selected_preset_index);
			}
		}

		private void handle_selected_dropdown_option(int selected_index) {
			presets_is_updating = true;

			if (selected_index < 0 || selected_index >= presets_dropdown.options.Count) {
				presets_is_updating = false;
				return;
			}
			

			string current_selected_preset_name = presets_dropdown.options[current_selected_preset_index].text;
			int new_preset_index = selected_index;
			string new_preset_name = presets_dropdown.options[selected_index].text;

			Error save_error = custom_rules_presets_manager.preset_save_settings(current_selected_preset_name);
			if (save_error != Error.Success) {
				Utilities.log_verbose(Utilities.LogType.Error, $"Failed to save the current preset {current_selected_preset_name} before switching. Restoring previous dropdown selection...");
				update_values(current_selected_preset_index);
				presets_is_updating = false;
				return;
			}
			
			if (new_preset_name == NEW_PRESET_OPTION_TEXT) {
				if (Utilities.do_log_debug) {Utilities.log_verbose(Utilities.LogType.Debug, "New Preset option selected, creating new preset...");}

				List<string> preset_names = custom_rules_presets_manager.custom_rules_presets_data.get_preset_names();
				int new_preset_appended_number = preset_names.Count + 1;
				new_preset_name = $"Preset {new_preset_appended_number}";
				while (preset_names.Contains(new_preset_name)) {
					new_preset_appended_number++;
					new_preset_name = $"Preset {new_preset_appended_number}";
				}
				new_preset_index = presets_dropdown.options.Count - 1;

				Error preset_duplicate_error = custom_rules_presets_manager.custom_rules_presets_data.preset_duplicate(current_selected_preset_name, new_preset_name);
				if (preset_duplicate_error != Error.Success) {
					Utilities.log_verbose(Utilities.LogType.Error, $"Failed to duplicate preset '{current_selected_preset_name}' with error: {preset_duplicate_error.ToString()}");
					update_values(current_selected_preset_index);

				} else {
					insert_new_dropdown_option(new_preset_name, new_preset_index);
					if (Utilities.do_log_debug) {Utilities.log_verbose(Utilities.LogType.Debug, $"Preset '{current_selected_preset_name}' duplicated to '{new_preset_name}'.");}
				}

				presets_is_updating = false;
				return;
			}

			if (!custom_rules_presets_manager.custom_rules_presets_data.has_preset(new_preset_name)) {
				Utilities.log_verbose(Utilities.LogType.Error, $"Selected preset '{new_preset_name}' not found in presets data.");
				update_values(current_selected_preset_index);
				presets_is_updating = false;
				return;
			}

			Error load_error = custom_rules_presets_manager.preset_load_settings(new_preset_name);
			if (load_error != Error.Success) {
				Utilities.log_verbose(Utilities.LogType.Error, $"Failed to load preset {new_preset_name}. Restoring previous dropdown selection...");
				update_values(current_selected_preset_index);
				presets_is_updating = false;
				return;
			}

			Error save_presets_to_file_error = custom_rules_presets_manager.save_presets_to_file();
			if (Utilities.do_log_debug) {Utilities.log_verbose(Utilities.LogType.Debug, $"Save presets to file error: {save_presets_to_file_error}");}

			update_values(new_preset_index);
			presets_is_updating = false;
		}

		public Error insert_new_dropdown_option(string option_text, int option_index = -1, bool set_value = true) {
			if (presets_dropdown == null) {
				Utilities.log_verbose(Utilities.LogType.Error, "Cannot insert new dropdown option because presets_dropdown is null.");
				return Error.GlobalVariableIsNull;
			}

			int actual_option_index = option_index == -1 ? presets_dropdown.options.Count - 1 : option_index;
			presets_dropdown.options.Insert(actual_option_index, new TMP_Dropdown.OptionData(option_text));
			if (set_value) {
				update_values(actual_option_index);
			}
			return Error.Success;
		}

		public void reset_styling_to_enabled(GameObject root) {
			foreach (CanvasGroup cg in root.GetComponentsInChildren<CanvasGroup>(true)) {
				cg.alpha = 1f;
				cg.interactable = true;
				cg.blocksRaycasts = true;
			}

			foreach (Selectable selectable in root.GetComponentsInChildren<Selectable>(true)) {
				selectable.interactable = true;
			}

			foreach (Graphic g in root.GetComponentsInChildren<Graphic>(true)) {
				Color c = g.color;
				c.a = 1f;
				g.color = c;
			}

			foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true)) {
				Color c = text.color;
				c.a = 1f;
				text.color = c;
			}
		}

		public void update_values(int new_index) {
			current_selected_preset_index = new_index;
			presets_dropdown.SetValueWithoutNotify(current_selected_preset_index);
			presets_dropdown.RefreshShownValue();
			preset_rename_input_field.SetTextWithoutNotify(presets_dropdown.options[current_selected_preset_index].text);
		}

		public UIManager(MatchSetupMenu new_instance_match_setup_menu) {
			if (new_instance_match_setup_menu == null) {
				Utilities.log_verbose(Utilities.LogType.Error, "UIManager is not set up properly.");
				construction_error = Error.ArgumentNull;
			} else if (custom_rules_presets_manager == null) {
				Utilities.log_verbose(Utilities.LogType.Error, "custom_rules_presets_manager is null");
			} else {
				instance_match_setup_menu = new_instance_match_setup_menu;

				GameObject instance_match_setup_menu_ui = instance_match_setup_menu.menu;
				ActiveStateListener listener = instance_match_setup_menu_ui.GetComponent<ActiveStateListener>();
				if (listener == null) {listener = instance_match_setup_menu_ui.AddComponent<ActiveStateListener>();}

				listener.OnActiveChanged += isActive => {
					if (!isActive) {
						if (Utilities.do_log_debug) {Utilities.log_verbose(Utilities.LogType.Debug, $"{instance_match_setup_menu_ui.name} was closed, saving currently selected preset...");}
						Error preset_save_error = custom_rules_presets_manager.preset_save_settings(presets_dropdown.options[current_selected_preset_index].text);
						Error save_presets_to_file_error = custom_rules_presets_manager.save_presets_to_file();
						if (Utilities.do_log_debug) {Utilities.log_verbose(Utilities.LogType.Debug, $"Preset save on menu close error: {preset_save_error}");}
						if (Utilities.do_log_debug) {Utilities.log_verbose(Utilities.LogType.Debug, $"Save presets to file error: {save_presets_to_file_error}");}
					}
				};
				
				ConfigManager config_manager = CustomRulesPresetsPlugin.config_manager;
				if (config_manager.do_save_tree_to_disk) {
					Utilities.save_tree_info_to_disk(instance_match_setup_menu.menu);
					config_manager.do_save_tree_to_disk = false;
				}

				if (instance_match_setup_menu.isServer) {
					Error cloning_error = clone_and_insert_ui_elements();
					
					if (custom_rules_presets_manager.custom_rules_presets_data.get_preset_count() == 0){
						Error preset_create_error = custom_rules_presets_manager.custom_rules_presets_data.preset_create("Preset 1");
						if (preset_create_error != Error.Success) {
							Utilities.log_verbose(Utilities.LogType.Error, $"Failed to create default preset 'Preset 1' with error: {preset_create_error.ToString()}");
							return;
						}
						custom_rules_presets_manager.preset_save_settings();
					}

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
					preset_rename_input_field.SetTextWithoutNotify(presets_dropdown.options[current_selected_preset_index].text);
				}

			}
		}
	}
}
