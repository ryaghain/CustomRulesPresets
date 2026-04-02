using CustomRulesPresets.Core;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CustomRulesPresets.UI {
	public static class UIManager {
		public static MatchSetupMenu? instance_match_setup_menu = null;
		public static UIBuilder instance_ui_builder = new UIBuilder();

		[Serializable]
		public class PresetData {
			public string name_text = "Preset ";
			public Action<string>? on_name_text_changed;
			public Action? on_save_icon_click;
			public Action? on_load_icon_click;
			public Action? on_delete_icon_click;
		}
		public static List<PresetData> preset_entries = new List<PresetData>();

		public static void reset() {
			instance_match_setup_menu = null;
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
				instance_ui_builder.build(instance_match_setup_menu.rulesTab, preset_entries, CreateSolidSprite(), CreateSolidSprite(), CreateSolidSprite());
			}

			Plugin.Log.LogDebug("Setting up CustomRulesPresetsManager...");
			int setup_error_code = CustomRulesPresetsManager.setup(instance_match_setup_menu.rules);
			Plugin.Log.LogDebug($"CustomRulesPresetsManager exted setup with code: {setup_error_code}");

			return 0;
		}

		public static Sprite CreateSolidSprite() {
			var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
			tex.SetPixel(0, 0, Color.magenta);
			tex.Apply();

			return Sprite.Create(
				tex,
				new Rect(0, 0, 1, 1),
				new Vector2(0.5f, 0.5f)
			);
		}
	}

	public class UIBuilder: MonoBehaviour {
		public CanvasGroup? _popupGroup = null;
		public RectTransform? presets_selector_content_rect_transform_content = null;

		public void build(CanvasGroup host_canvas_group, List<UIManager.PresetData> preset_entries, Sprite save_icon, Sprite load_icon, Sprite delete_icon) {
			// Root under the existing CanvasGroup object
			GameObject preset_selector_root = new GameObject("PresetSelectorRoot", typeof(RectTransform));
			preset_selector_root.transform.SetParent(host_canvas_group.transform, false);
			RectTransform preset_selector_root_rect_transform = (RectTransform)preset_selector_root.transform;
			stretch(preset_selector_root_rect_transform);

			// Header button
			Button button_toggle_presets_selector = create_button(preset_selector_root.transform, "ButtonTogglePresetsSelector", "Options");
			RectTransform button_toggle_presets_selector_rect_transform = (RectTransform)button_toggle_presets_selector.transform;
			button_toggle_presets_selector_rect_transform.anchorMin = new Vector2(0, 1);
			button_toggle_presets_selector_rect_transform.anchorMax = new Vector2(0, 1);
			button_toggle_presets_selector_rect_transform.pivot = new Vector2(0, 1);
			button_toggle_presets_selector_rect_transform.anchoredPosition = new Vector2(20, -20);
			button_toggle_presets_selector_rect_transform.sizeDelta = new Vector2(260, 36);

			// Popup panel
			GameObject popup_panel_presets_selector = new GameObject("PopupPanelPresetsSelector", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
			popup_panel_presets_selector.transform.SetParent(preset_selector_root.transform, false);
			RectTransform popup_panel_presets_selector_rect_transform = (RectTransform)popup_panel_presets_selector.transform;
			popup_panel_presets_selector_rect_transform.anchorMin = new Vector2(0, 1);
			popup_panel_presets_selector_rect_transform.anchorMax = new Vector2(0, 1);
			popup_panel_presets_selector_rect_transform.pivot = new Vector2(0, 1);
			popup_panel_presets_selector_rect_transform.anchoredPosition = new Vector2(20, -60);
			popup_panel_presets_selector_rect_transform.sizeDelta = new Vector2(420, 300);

			Image popup_panel_presets_selector_image = popup_panel_presets_selector.GetComponent<Image>();
			popup_panel_presets_selector_image.color = new Color(0f, 0f, 0f, 0.85f);

			_popupGroup = popup_panel_presets_selector.GetComponent<CanvasGroup>();
			set_popup_visible(false);

			// Scroll view
			GameObject scroll_view_presets_selector = new GameObject("ScrollViewPresetsSelector", typeof(RectTransform), typeof(Image), typeof(Mask), typeof(ScrollRect));
			scroll_view_presets_selector.transform.SetParent(popup_panel_presets_selector.transform, false);
			RectTransform scroll_view_presets_selector_rect_transform = (RectTransform)scroll_view_presets_selector.transform;
			stretch_with_padding(scroll_view_presets_selector_rect_transform, 8);

			Image presets_selector_viewport_image = scroll_view_presets_selector.GetComponent<Image>();
			presets_selector_viewport_image.color = new Color(1f, 1f, 1f, 0.03f);
			scroll_view_presets_selector.GetComponent<Mask>().showMaskGraphic = false;

			GameObject presets_selector_content = new GameObject("PresetsSelectorContent", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
			presets_selector_content.transform.SetParent(scroll_view_presets_selector.transform, false);
			presets_selector_content_rect_transform_content = (RectTransform)presets_selector_content.transform;

			presets_selector_content_rect_transform_content.anchorMin = new Vector2(0, 1);
			presets_selector_content_rect_transform_content.anchorMax = new Vector2(1, 1);
			presets_selector_content_rect_transform_content.pivot = new Vector2(0.5f, 1);

			var layout = presets_selector_content.GetComponent<VerticalLayoutGroup>();
			layout.spacing = 6;
			layout.padding = new RectOffset(6, 6, 6, 6);
			layout.childControlHeight = true;
			layout.childControlWidth = true;
			layout.childForceExpandHeight = false;
			layout.childForceExpandWidth = true;

			var fitter = presets_selector_content.GetComponent<ContentSizeFitter>();
			fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

			var scrollRect = scroll_view_presets_selector.GetComponent<ScrollRect>();
			scrollRect.content = presets_selector_content_rect_transform_content;
			scrollRect.viewport = presets_selector_content_rect_transform_content;
			scrollRect.horizontal = false;
			scrollRect.vertical = true;

			for (int index = 0; index < preset_entries.Count; index++)
				create_row(presets_selector_content_rect_transform_content, index, save_icon, load_icon, delete_icon);

			button_toggle_presets_selector.onClick.AddListener(() => set_popup_visible(!_popupGroup.interactable));
		}

		private void create_row(Transform parent, int preset_index, Sprite save_icon, Sprite load_icon, Sprite delete_icon) {
			UIManager.PresetData preset_data = UIManager.preset_entries[preset_index];

			var preset_entry = new GameObject("OptionRow", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
			preset_entry.transform.SetParent(parent, false);

			var rowImage = preset_entry.GetComponent<Image>();
			rowImage.color = new Color(1f, 1f, 1f, 0.06f);

			var rowLayout = preset_entry.GetComponent<HorizontalLayoutGroup>();
			rowLayout.spacing = 6;
			rowLayout.padding = new RectOffset(6, 6, 6, 6);
			rowLayout.childAlignment = TextAnchor.MiddleLeft;
			rowLayout.childControlHeight = true;
			rowLayout.childControlWidth = false;
			rowLayout.childForceExpandWidth = false;
			rowLayout.childForceExpandHeight = false;

			var rowRt = (RectTransform)preset_entry.transform;
			rowRt.sizeDelta = new Vector2(0, 40);

			var input = create_text_mesh_pro_input_field(preset_entry.transform, preset_data.name_text + preset_index);
			var inputLE = input.gameObject.AddComponent<LayoutElement>();
			inputLE.flexibleWidth = 1f;
			inputLE.minWidth = 180f;

			input.onValueChanged.AddListener(value => preset_data.on_name_text_changed?.Invoke(value));

			var save_button = create_icon_button(preset_entry.transform, save_icon);
			save_button.onClick.AddListener(() => preset_data.on_save_icon_click?.Invoke());

			var load_button = create_icon_button(preset_entry.transform, load_icon);
			load_button.onClick.AddListener(() => preset_data.on_load_icon_click?.Invoke());

			var delete_button = create_icon_button(preset_entry.transform, delete_icon);
			delete_button.onClick.AddListener(() => preset_data.on_delete_icon_click?.Invoke());
		}

		private Button create_button(Transform parent, string name, string label_text) {
			GameObject button = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
			button.transform.SetParent(parent, false);

			Image button_image = button.GetComponent<Image>();
			button_image.color = new Color(1f, 1f, 1f, 0.12f);

			GameObject button_label = new GameObject("ButtonLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
			button_label.transform.SetParent(button.transform, false);
			RectTransform button_label_rect_transform = (RectTransform)button_label.transform;
			stretch_with_padding(button_label_rect_transform, 8);

			TextMeshProUGUI button_label_text_mesh_pro = button_label.GetComponent<TextMeshProUGUI>();
			button_label_text_mesh_pro.text = label_text;
			button_label_text_mesh_pro.fontSize = 20;
			button_label_text_mesh_pro.alignment = TextAlignmentOptions.MidlineLeft;

			return button.GetComponent<Button>();
		}

		private Button create_icon_button(Transform parent, Sprite icon) {
			GameObject button_icon = new GameObject("ButtonIcon", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
			button_icon.transform.SetParent(parent, false);

			LayoutElement button_icon_layout_element = button_icon.GetComponent<LayoutElement>();
			button_icon_layout_element.preferredWidth = 28;
			button_icon_layout_element.preferredHeight = 28;

			Image button_icon_image = button_icon.GetComponent<Image>();
			button_icon_image.sprite = icon;
			button_icon_image.preserveAspect = true;
			button_icon_image.color = Color.white;

			return button_icon.GetComponent<Button>();
		}

		private TMP_InputField create_text_mesh_pro_input_field(Transform parent, string initial_text) {
			GameObject root_text_mesh_input_field = new GameObject("InputFieldTMP", typeof(RectTransform), typeof(Image), typeof(TMP_InputField), typeof(LayoutElement));
			root_text_mesh_input_field.transform.SetParent(parent, false);

			Image backgorund_image = root_text_mesh_input_field.GetComponent<Image>();
			backgorund_image.color = new Color(1f, 1f, 1f, 0.08f);

			GameObject input_field_text_area = new GameObject("InputFieldTextArea", typeof(RectTransform), typeof(RectMask2D));
			input_field_text_area.transform.SetParent(root_text_mesh_input_field.transform, false);
			stretch_with_padding((RectTransform)input_field_text_area.transform, 6);

			GameObject preset_name_placeholder_text = new GameObject("PlaceholderText", typeof(RectTransform), typeof(TextMeshProUGUI));
			preset_name_placeholder_text.transform.SetParent(input_field_text_area.transform, false);
			stretch((RectTransform)preset_name_placeholder_text.transform);
			var placeholder = preset_name_placeholder_text.GetComponent<TextMeshProUGUI>();
			placeholder.text = "Enter text...";
			placeholder.fontSize = 18;
			placeholder.color = new Color(1f, 1f, 1f, 0.35f);

			GameObject preset_name_text = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
			preset_name_text.transform.SetParent(input_field_text_area.transform, false);
			stretch((RectTransform)preset_name_text.transform);
			TextMeshProUGUI preset_name_text_mesh_pro = preset_name_text.GetComponent<TextMeshProUGUI>();
			preset_name_text_mesh_pro.text = initial_text;
			preset_name_text_mesh_pro.fontSize = 18;
			preset_name_text_mesh_pro.color = Color.white;

			var input = root_text_mesh_input_field.GetComponent<TMP_InputField>();
			input.textViewport = (RectTransform)input_field_text_area.transform;
			input.textComponent = preset_name_text_mesh_pro;
			input.placeholder = placeholder;
			input.text = initial_text;

			return input;
		}

		private void set_popup_visible(bool visible) {
			if (_popupGroup == null) {
				Plugin.Log.LogError("UIManager is not set up properly.");
				return;
			}
			_popupGroup.alpha = visible ? 1f : 0f;
			_popupGroup.interactable = visible;
			_popupGroup.blocksRaycasts = visible;
		}

		private static void stretch(RectTransform rect_transform) {
			rect_transform.anchorMin = Vector2.zero;
			rect_transform.anchorMax = Vector2.one;
			rect_transform.offsetMin = Vector2.zero;
			rect_transform.offsetMax = Vector2.zero;
		}

		private static void stretch_with_padding(RectTransform rect_transform, float pad) {
			rect_transform.anchorMin = Vector2.zero;
			rect_transform.anchorMax = Vector2.one;
			rect_transform.offsetMin = new Vector2(pad, pad);
			rect_transform.offsetMax = new Vector2(-pad, -pad);
		}
	}
}
