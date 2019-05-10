#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class HotkeyDialogLogic : ChromeLogic
	{
		readonly Widget panel;
		readonly ButtonWidget resetButton, clearButton, cancelButton;
		readonly LabelWidget duplicateNotice, defaultNotice, originalNotice;
		readonly Action onSave;
		readonly HotkeyDefinition definition;
		readonly HotkeyManager manager;
		readonly HotkeyEntryWidget hotkeyEntry;
		readonly bool isFirstValidation = true;
		Hotkey currentHotkey;
		HotkeyDefinition duplicateHotkey;
		bool isValid = false;
		bool isValidating = false;

		[ObjectCreator.UseCtor]
		public HotkeyDialogLogic(Widget widget, Action onSave, HotkeyDefinition hotkeyDefinition, HotkeyManager hotkeyManager)
		{
			panel = widget;
			this.onSave = onSave;
			definition = hotkeyDefinition;
			manager = hotkeyManager;
			currentHotkey = manager[definition.Name].GetValue();
			hotkeyEntry = panel.Get<HotkeyEntryWidget>("HOTKEY_ENTRY");
			resetButton = panel.Get<ButtonWidget>("RESET_BUTTON");
			clearButton = panel.Get<ButtonWidget>("CLEAR_BUTTON");
			cancelButton = panel.Get<ButtonWidget>("CANCEL_BUTTON");
			duplicateNotice = panel.Get<LabelWidget>("DUPLICATE_NOTICE");
			defaultNotice = panel.Get<LabelWidget>("DEFAULT_NOTICE");
			originalNotice = panel.Get<LabelWidget>("ORIGINAL_NOTICE");

			panel.Get<LabelWidget>("HOTKEY_LABEL").GetText = () => hotkeyDefinition.Description + ":";

			duplicateNotice.TextColor = ChromeMetrics.Get<Color>("NoticeErrorColor");
			duplicateNotice.GetText = () =>
			{
				return (duplicateHotkey != null) ? duplicateNotice.Text.F(duplicateHotkey.Description) : duplicateNotice.Text;
			};
			defaultNotice.TextColor = ChromeMetrics.Get<Color>("NoticeInfoColor");
			originalNotice.TextColor = ChromeMetrics.Get<Color>("NoticeInfoColor");
			originalNotice.Text = originalNotice.Text.F(hotkeyDefinition.Default.DisplayString());

			resetButton.OnClick = Reset;
			clearButton.OnClick = Clear;
			cancelButton.OnClick = Cancel;

			hotkeyEntry.Key = currentHotkey;
			hotkeyEntry.IsValid = () => isValid;
			hotkeyEntry.OnTakeFocus = OnHotkeyEntryTakeFocus;
			hotkeyEntry.OnLoseFocus = OnHotkeyEntryLoseFocus;
			hotkeyEntry.OnEscape = Cancel;
			hotkeyEntry.OnReturn = Cancel;
			hotkeyEntry.TakeKeyboardFocus();

			Validate();
			isFirstValidation = false;
		}

		void OnHotkeyEntryTakeFocus()
		{
			cancelButton.Disabled = manager.GetFirstDuplicate(definition.Name, currentHotkey, definition) != null;
		}

		void OnHotkeyEntryLoseFocus()
		{
			cancelButton.Disabled = true;
			if (!isValidating)
				Validate();
		}

		void Validate()
		{
			isValidating = true;

			duplicateHotkey = manager.GetFirstDuplicate(definition.Name, hotkeyEntry.Key, definition);
			isValid = duplicateHotkey == null;

			duplicateNotice.Visible = !isValid;
			clearButton.Disabled = !hotkeyEntry.Key.IsValid();

			if (hotkeyEntry.Key == definition.Default || (!hotkeyEntry.Key.IsValid() && !definition.Default.IsValid()))
			{
				defaultNotice.Visible = !duplicateNotice.Visible;
				originalNotice.Visible = false;
				resetButton.Disabled = true;
			}
			else
			{
				defaultNotice.Visible = false;
				originalNotice.Visible = !duplicateNotice.Visible;
				resetButton.Disabled = false;
			}

			if (isValid && !isFirstValidation)
			{
				currentHotkey = hotkeyEntry.Key;
				hotkeyEntry.YieldKeyboardFocus();
				Save();
			}
			else
				hotkeyEntry.TakeKeyboardFocus();

			isValidating = false;
		}

		void Save()
		{
			manager.Set(definition.Name, hotkeyEntry.Key);
			Game.Settings.Save();
			onSave();
		}

		void Cancel()
		{
			cancelButton.Disabled = true;
			hotkeyEntry.Key = currentHotkey;
			Validate();
		}

		void Reset()
		{
			hotkeyEntry.Key = definition.Default;
			Validate();
		}

		void Clear()
		{
			hotkeyEntry.Key = Hotkey.Invalid;
			Validate();
		}
	}
}
