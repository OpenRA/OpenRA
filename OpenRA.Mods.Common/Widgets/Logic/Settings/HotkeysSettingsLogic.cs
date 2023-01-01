#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class HotkeysSettingsLogic : ChromeLogic
	{
		readonly ModData modData;
		readonly Dictionary<string, MiniYaml> logicArgs;

		ScrollPanelWidget hotkeyList;
		ButtonWidget selectedHotkeyButton;
		HotkeyEntryWidget hotkeyEntryWidget;
		HotkeyDefinition duplicateHotkeyDefinition, selectedHotkeyDefinition;
		int validHotkeyEntryWidth;
		int invalidHotkeyEntryWidth;
		bool isHotkeyValid;
		bool isHotkeyDefault;

		string currentContext = "Any";
		readonly HashSet<string> contexts = new HashSet<string>() { "Any" };
		readonly Dictionary<string, HashSet<string>> hotkeyGroups = new Dictionary<string, HashSet<string>>();
		TextFieldWidget filterInput;

		Widget headerTemplate;
		Widget template;
		Widget emptyListMessage;
		Widget remapDialog;

		static HotkeysSettingsLogic() { }

		[ObjectCreator.UseCtor]
		public HotkeysSettingsLogic(Action<string, string, Func<Widget, Func<bool>>, Func<Widget, Action>> registerPanel, string panelID, string label, ModData modData, Dictionary<string, MiniYaml> logicArgs)
		{
			this.modData = modData;
			this.logicArgs = logicArgs;

			registerPanel(panelID, label, InitPanel, ResetPanel);
		}

		void BindHotkeyPref(HotkeyDefinition hd, Widget template)
		{
			var key = template.Clone();
			key.Id = hd.Name;
			key.IsVisible = () => true;

			key.Get<LabelWidget>("FUNCTION").GetText = () => hd.Description + ":";

			var remapButton = key.Get<ButtonWidget>("HOTKEY");
			WidgetUtils.TruncateButtonToTooltip(remapButton, modData.Hotkeys[hd.Name].GetValue().DisplayString());

			remapButton.IsHighlighted = () => selectedHotkeyDefinition == hd;

			var hotkeyValidColor = ChromeMetrics.Get<Color>("HotkeyColor");
			var hotkeyInvalidColor = ChromeMetrics.Get<Color>("HotkeyColorInvalid");

			remapButton.GetColor = () =>
			{
				return hd.HasDuplicates ? hotkeyInvalidColor : hotkeyValidColor;
			};

			if (selectedHotkeyDefinition == hd)
			{
				selectedHotkeyButton = remapButton;
				hotkeyEntryWidget.Key = modData.Hotkeys[hd.Name].GetValue();
				ValidateHotkey();
			}

			remapButton.OnClick = () =>
			{
				selectedHotkeyDefinition = hd;
				selectedHotkeyButton = remapButton;
				hotkeyEntryWidget.Key = modData.Hotkeys[hd.Name].GetValue();
				ValidateHotkey();

				if (hd.Readonly)
					hotkeyEntryWidget.YieldKeyboardFocus();
				else
					hotkeyEntryWidget.TakeKeyboardFocus();
			};

			hotkeyList.AddChild(key);
		}

		Func<bool> InitPanel(Widget panel)
		{
			hotkeyList = panel.Get<ScrollPanelWidget>("HOTKEY_LIST");
			hotkeyList.Layout = new GridLayout(hotkeyList);
			headerTemplate = hotkeyList.Get("HEADER");
			template = hotkeyList.Get("TEMPLATE");
			emptyListMessage = panel.Get("HOTKEY_EMPTY_LIST");
			remapDialog = panel.Get("HOTKEY_REMAP_DIALOG");

			foreach (var hd in modData.Hotkeys.Definitions)
				contexts.UnionWith(hd.Contexts);

			filterInput = panel.Get<TextFieldWidget>("FILTER_INPUT");
			filterInput.OnTextEdited = () => InitHotkeyList();
			filterInput.OnEscKey = _ =>
			{
				if (string.IsNullOrEmpty(filterInput.Text))
					filterInput.YieldKeyboardFocus();
				else
				{
					filterInput.Text = "";
					filterInput.OnTextEdited();
				}

				return true;
			};

			var contextDropdown = panel.GetOrNull<DropDownButtonWidget>("CONTEXT_DROPDOWN");
			if (contextDropdown != null)
			{
				contextDropdown.OnMouseDown = _ => ShowContextDropdown(contextDropdown);
				var contextName = new CachedTransform<string, string>(GetContextDisplayName);
				contextDropdown.GetText = () => contextName.Update(currentContext);
			}

			if (logicArgs.TryGetValue("HotkeyGroups", out var hotkeyGroupsYaml))
			{
				foreach (var hg in hotkeyGroupsYaml.Nodes)
				{
					var typesNode = hg.Value.Nodes.FirstOrDefault(n => n.Key == "Types");
					if (typesNode != null)
						hotkeyGroups.Add(hg.Key, FieldLoader.GetValue<HashSet<string>>("Types", typesNode.Value.Value));
				}

				InitHotkeyRemapDialog(panel);
				InitHotkeyList();
			}

			return () =>
			{
				hotkeyEntryWidget.Key =
					selectedHotkeyDefinition != null ?
					modData.Hotkeys[selectedHotkeyDefinition.Name].GetValue() :
					Hotkey.Invalid;

				hotkeyEntryWidget.ForceYieldKeyboardFocus();

				return false;
			};
		}

		Action ResetPanel(Widget panel)
		{
			return () =>
			{
				foreach (var hd in modData.Hotkeys.Definitions)
				{
					modData.Hotkeys.Set(hd.Name, hd.Default);
					WidgetUtils.TruncateButtonToTooltip(panel.Get(hd.Name).Get<ButtonWidget>("HOTKEY"), hd.Default.DisplayString());
				}
			};
		}

		void InitHotkeyList()
		{
			hotkeyList.RemoveChildren();
			selectedHotkeyDefinition = null;

			foreach (var hg in hotkeyGroups)
			{
				var typesInGroup = hg.Value;
				var keysInGroup = modData.Hotkeys.Definitions
					.Where(hd => IsHotkeyVisibleInFilter(hd) && hd.Types.Overlaps(typesInGroup));

				if (!keysInGroup.Any())
					continue;

				var header = headerTemplate.Clone();
				header.Get<LabelWidget>("LABEL").GetText = () => hg.Key;
				hotkeyList.AddChild(header);

				var added = new HashSet<HotkeyDefinition>();

				foreach (var type in typesInGroup)
				{
					foreach (var hd in keysInGroup.Where(k => k.Types.Contains(type)))
					{
						if (added.Add(hd))
						{
							if (selectedHotkeyDefinition == null)
								selectedHotkeyDefinition = hd;

							BindHotkeyPref(hd, template);
						}
					}
				}
			}

			emptyListMessage.Visible = selectedHotkeyDefinition == null;
			remapDialog.Visible = selectedHotkeyDefinition != null;

			hotkeyList.ScrollToTop();
		}

		void InitHotkeyRemapDialog(Widget panel)
		{
			var label = panel.Get<LabelWidget>("HOTKEY_LABEL");
			var labelText = new CachedTransform<HotkeyDefinition, string>(hd => hd?.Description + ":");
			label.IsVisible = () => selectedHotkeyDefinition != null;
			label.GetText = () => labelText.Update(selectedHotkeyDefinition);

			var duplicateNotice = panel.Get<LabelWidget>("DUPLICATE_NOTICE");
			duplicateNotice.TextColor = ChromeMetrics.Get<Color>("NoticeErrorColor");
			duplicateNotice.IsVisible = () => !isHotkeyValid;
			var duplicateNoticeText = new CachedTransform<HotkeyDefinition, string>(hd =>
				hd != null ?
				duplicateNotice.Text.F(hd.Description, hd.Contexts.First(c => selectedHotkeyDefinition.Contexts.Contains(c))) :
				"");
			duplicateNotice.GetText = () => duplicateNoticeText.Update(duplicateHotkeyDefinition);

			var originalNotice = panel.Get<LabelWidget>("ORIGINAL_NOTICE");
			originalNotice.TextColor = ChromeMetrics.Get<Color>("NoticeInfoColor");
			originalNotice.IsVisible = () => isHotkeyValid && !isHotkeyDefault;
			var originalNoticeText = new CachedTransform<HotkeyDefinition, string>(hd => originalNotice.Text.F(hd?.Default.DisplayString()));
			originalNotice.GetText = () => originalNoticeText.Update(selectedHotkeyDefinition);

			var readonlyNotice = panel.Get<LabelWidget>("READONLY_NOTICE");
			readonlyNotice.TextColor = ChromeMetrics.Get<Color>("NoticeInfoColor");
			readonlyNotice.IsVisible = () => selectedHotkeyDefinition.Readonly;

			var resetButton = panel.Get<ButtonWidget>("RESET_HOTKEY_BUTTON");
			resetButton.IsDisabled = () => isHotkeyDefault || selectedHotkeyDefinition.Readonly;
			resetButton.OnClick = ResetHotkey;

			var clearButton = panel.Get<ButtonWidget>("CLEAR_HOTKEY_BUTTON");
			clearButton.IsDisabled = () => selectedHotkeyDefinition.Readonly || !hotkeyEntryWidget.Key.IsValid();
			clearButton.OnClick = ClearHotkey;

			var overrideButton = panel.Get<ButtonWidget>("OVERRIDE_HOTKEY_BUTTON");
			overrideButton.IsDisabled = () => isHotkeyValid;
			overrideButton.IsVisible = () => !isHotkeyValid && !duplicateHotkeyDefinition.Readonly;
			overrideButton.OnClick = OverrideHotkey;

			hotkeyEntryWidget = panel.Get<HotkeyEntryWidget>("HOTKEY_ENTRY");
			hotkeyEntryWidget.IsValid = () => isHotkeyValid;
			hotkeyEntryWidget.OnLoseFocus = ValidateHotkey;
			hotkeyEntryWidget.OnEscKey = _ =>
			{
				hotkeyEntryWidget.Key = modData.Hotkeys[selectedHotkeyDefinition.Name].GetValue();
			};
			hotkeyEntryWidget.IsDisabled = () => selectedHotkeyDefinition.Readonly;

			validHotkeyEntryWidth = hotkeyEntryWidget.Bounds.Width;
			invalidHotkeyEntryWidth = validHotkeyEntryWidth - (clearButton.Bounds.X - overrideButton.Bounds.X);
		}

		void ValidateHotkey()
		{
			if (selectedHotkeyDefinition == null)
				return;

			duplicateHotkeyDefinition = modData.Hotkeys.GetFirstDuplicate(selectedHotkeyDefinition, hotkeyEntryWidget.Key);
			isHotkeyValid = duplicateHotkeyDefinition == null || selectedHotkeyDefinition.Readonly;
			isHotkeyDefault = hotkeyEntryWidget.Key == selectedHotkeyDefinition.Default || (!hotkeyEntryWidget.Key.IsValid() && !selectedHotkeyDefinition.Default.IsValid());

			if (isHotkeyValid)
			{
				hotkeyEntryWidget.Bounds.Width = validHotkeyEntryWidth;
				SaveHotkey();
			}
			else
			{
				hotkeyEntryWidget.Bounds.Width = duplicateHotkeyDefinition.Readonly ? validHotkeyEntryWidth : invalidHotkeyEntryWidth;
				hotkeyEntryWidget.TakeKeyboardFocus();
			}
		}

		void SaveHotkey()
		{
			if (selectedHotkeyDefinition.Readonly)
				return;

			WidgetUtils.TruncateButtonToTooltip(selectedHotkeyButton, hotkeyEntryWidget.Key.DisplayString());
			modData.Hotkeys.Set(selectedHotkeyDefinition.Name, hotkeyEntryWidget.Key);
			Game.Settings.Save();
		}

		void ResetHotkey()
		{
			hotkeyEntryWidget.Key = selectedHotkeyDefinition.Default;
			hotkeyEntryWidget.YieldKeyboardFocus();
		}

		void ClearHotkey()
		{
			hotkeyEntryWidget.Key = Hotkey.Invalid;
			hotkeyEntryWidget.YieldKeyboardFocus();
		}

		void OverrideHotkey()
		{
			var duplicateHotkeyButton = hotkeyList.Get<ContainerWidget>(duplicateHotkeyDefinition.Name).Get<ButtonWidget>("HOTKEY");
			WidgetUtils.TruncateButtonToTooltip(duplicateHotkeyButton, Hotkey.Invalid.DisplayString());
			modData.Hotkeys.Set(duplicateHotkeyDefinition.Name, Hotkey.Invalid);
			Game.Settings.Save();
			hotkeyEntryWidget.YieldKeyboardFocus();
		}

		bool IsHotkeyVisibleInFilter(HotkeyDefinition hd)
		{
			var filter = filterInput.Text;
			var isFilteredByName = string.IsNullOrWhiteSpace(filter) || hd.Description.Contains(filter, StringComparison.OrdinalIgnoreCase);
			var isFilteredByContext = currentContext == "Any" || hd.Contexts.Contains(currentContext);

			return isFilteredByName && isFilteredByContext;
		}

		bool ShowContextDropdown(DropDownButtonWidget dropdown)
		{
			hotkeyEntryWidget.YieldKeyboardFocus();

			var contextName = new CachedTransform<string, string>(GetContextDisplayName);
			ScrollItemWidget SetupItem(string context, ScrollItemWidget itemTemplate)
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => currentContext == context,
					() => { currentContext = context; InitHotkeyList(); });

				item.Get<LabelWidget>("LABEL").GetText = () => contextName.Update(context);
				return item;
			}

			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 280, contexts, SetupItem);

			return true;
		}

		static string GetContextDisplayName(string context)
		{
			if (string.IsNullOrEmpty(context))
				return "Any";

			return context;
		}
	}
}
