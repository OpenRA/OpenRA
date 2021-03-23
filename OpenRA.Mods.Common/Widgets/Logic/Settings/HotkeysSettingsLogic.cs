#region Copyright & License Information
/*
 * Copyright 2007-2021 The OpenRA Developers (see AUTHORS)
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
			var key = template.Clone() as Widget;
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
				hotkeyEntryWidget.TakeKeyboardFocus();
			};

			hotkeyList.AddChild(key);
		}

		Func<bool> InitPanel(Widget panel)
		{
			hotkeyList = panel.Get<ScrollPanelWidget>("HOTKEY_LIST");
			hotkeyList.Layout = new GridLayout(hotkeyList);
			var hotkeyHeader = hotkeyList.Get<ScrollItemWidget>("HEADER");
			var templates = hotkeyList.Get("TEMPLATES");
			hotkeyList.RemoveChildren();

			Func<bool> returnTrue = () => true;
			Action doNothing = () => { };

			if (logicArgs.TryGetValue("HotkeyGroups", out var hotkeyGroups))
			{
				InitHotkeyRemapDialog(panel);

				foreach (var hg in hotkeyGroups.Nodes)
				{
					var templateNode = hg.Value.Nodes.FirstOrDefault(n => n.Key == "Template");
					var typesNode = hg.Value.Nodes.FirstOrDefault(n => n.Key == "Types");
					if (templateNode == null || typesNode == null)
						continue;

					var header = ScrollItemWidget.Setup(hotkeyHeader, returnTrue, doNothing);
					header.Get<LabelWidget>("LABEL").GetText = () => hg.Key;
					hotkeyList.AddChild(header);

					var types = FieldLoader.GetValue<string[]>("Types", typesNode.Value.Value);
					var added = new HashSet<HotkeyDefinition>();
					var template = templates.Get(templateNode.Value.Value);

					foreach (var t in types)
					{
						foreach (var hd in modData.Hotkeys.Definitions.Where(k => k.Types.Contains(t)))
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
			}

			return () =>
			{
				hotkeyEntryWidget.Key = modData.Hotkeys[selectedHotkeyDefinition.Name].GetValue();
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

		void InitHotkeyRemapDialog(Widget panel)
		{
			var label = new CachedTransform<HotkeyDefinition, string>(hd => hd.Description + ":");
			panel.Get<LabelWidget>("HOTKEY_LABEL").GetText = () => label.Update(selectedHotkeyDefinition);

			var duplicateNotice = panel.Get<LabelWidget>("DUPLICATE_NOTICE");
			duplicateNotice.TextColor = ChromeMetrics.Get<Color>("NoticeErrorColor");
			duplicateNotice.IsVisible = () => !isHotkeyValid;
			var duplicateNoticeText = new CachedTransform<HotkeyDefinition, string>(hd => hd != null ? duplicateNotice.Text.F(hd.Description) : duplicateNotice.Text);
			duplicateNotice.GetText = () => duplicateNoticeText.Update(duplicateHotkeyDefinition);

			var defaultNotice = panel.Get<LabelWidget>("DEFAULT_NOTICE");
			defaultNotice.TextColor = ChromeMetrics.Get<Color>("NoticeInfoColor");
			defaultNotice.IsVisible = () => isHotkeyValid && isHotkeyDefault;

			var originalNotice = panel.Get<LabelWidget>("ORIGINAL_NOTICE");
			originalNotice.TextColor = ChromeMetrics.Get<Color>("NoticeInfoColor");
			originalNotice.IsVisible = () => isHotkeyValid && !isHotkeyDefault;
			var originalNoticeText = new CachedTransform<HotkeyDefinition, string>(hd => originalNotice.Text.F(hd.Default.DisplayString()));
			originalNotice.GetText = () => originalNoticeText.Update(selectedHotkeyDefinition);

			var resetButton = panel.Get<ButtonWidget>("RESET_HOTKEY_BUTTON");
			resetButton.IsDisabled = () => isHotkeyDefault;
			resetButton.OnClick = ResetHotkey;

			var clearButton = panel.Get<ButtonWidget>("CLEAR_HOTKEY_BUTTON");
			clearButton.IsDisabled = () => !hotkeyEntryWidget.Key.IsValid();
			clearButton.OnClick = ClearHotkey;

			var overrideButton = panel.Get<ButtonWidget>("OVERRIDE_HOTKEY_BUTTON");
			overrideButton.IsDisabled = () => isHotkeyValid;
			overrideButton.IsVisible = () => !isHotkeyValid;
			overrideButton.OnClick = OverrideHotkey;

			hotkeyEntryWidget = panel.Get<HotkeyEntryWidget>("HOTKEY_ENTRY");
			hotkeyEntryWidget.IsValid = () => isHotkeyValid;
			hotkeyEntryWidget.OnLoseFocus = ValidateHotkey;
			hotkeyEntryWidget.OnEscKey = () =>
			{
				hotkeyEntryWidget.Key = modData.Hotkeys[selectedHotkeyDefinition.Name].GetValue();
			};

			validHotkeyEntryWidth = hotkeyEntryWidget.Bounds.Width;
			invalidHotkeyEntryWidth = validHotkeyEntryWidth - (clearButton.Bounds.X - overrideButton.Bounds.X);
		}

		void ValidateHotkey()
		{
			duplicateHotkeyDefinition = modData.Hotkeys.GetFirstDuplicate(selectedHotkeyDefinition.Name, hotkeyEntryWidget.Key, selectedHotkeyDefinition);
			isHotkeyValid = duplicateHotkeyDefinition == null;
			isHotkeyDefault = hotkeyEntryWidget.Key == selectedHotkeyDefinition.Default || (!hotkeyEntryWidget.Key.IsValid() && !selectedHotkeyDefinition.Default.IsValid());

			if (isHotkeyValid)
			{
				hotkeyEntryWidget.Bounds.Width = validHotkeyEntryWidth;
				SaveHotkey();
			}
			else
			{
				hotkeyEntryWidget.Bounds.Width = invalidHotkeyEntryWidth;
				hotkeyEntryWidget.TakeKeyboardFocus();
			}
		}

		void SaveHotkey()
		{
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
	}
}
