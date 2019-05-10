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
		readonly ButtonWidget acceptButton, cancelButton, resetButton, clearButton;
		readonly LabelWidget duplicateNotice, defaultNotice, originalNotice;
		readonly Action onExit;
		Hotkey originalHotkey;
		HotkeyDefinition definition;
		HotkeyManager manager;
		HotkeyEntryWidget hotkeyEntry;

		[ObjectCreator.UseCtor]
		public HotkeyDialogLogic(Widget widget, Action onExit, HotkeyDefinition hotkeyDefinition, HotkeyManager hotkeyManager)
		{
			panel = widget;
			this.onExit = onExit;
			definition = hotkeyDefinition;
			manager = hotkeyManager;
			originalHotkey = manager[definition.Name].GetValue();
			hotkeyEntry = panel.Get<HotkeyEntryWidget>("HOTKEY_ENTRY");
			acceptButton = panel.Get<ButtonWidget>("ACCEPT_BUTTON");
			cancelButton = panel.Get<ButtonWidget>("CANCEL_BUTTON");
			resetButton = panel.Get<ButtonWidget>("RESET_BUTTON");
			clearButton = panel.Get<ButtonWidget>("CLEAR_BUTTON");
			duplicateNotice = panel.Get<LabelWidget>("DUPLICATE_NOTICE");
			defaultNotice = panel.Get<LabelWidget>("DEFAULT_NOTICE");
			originalNotice = panel.Get<LabelWidget>("ORIGINAL_NOTICE");

			panel.Get<LabelWidget>("HOTKEY_LABEL").GetText = () => hotkeyDefinition.Description + ":";

			duplicateNotice.TextColor = ChromeMetrics.Get<Color>("NoticeErrorColor");
			defaultNotice.TextColor = ChromeMetrics.Get<Color>("NoticeInfoColor");
			originalNotice.TextColor = ChromeMetrics.Get<Color>("NoticeInfoColor");
			originalNotice.Text = originalNotice.Text.F(hotkeyDefinition.Default.DisplayString());

			hotkeyEntry.Key = originalHotkey;
			hotkeyEntry.TakeKeyboardFocus();
			hotkeyEntry.OnLoseFocus = Validate;

			acceptButton.OnClick = Accept;
			cancelButton.OnClick = Cancel;
			resetButton.OnClick = Reset;
			clearButton.OnClick = Clear;

			Validate();
		}

		void Validate()
		{
			duplicateNotice.Visible = false;
			defaultNotice.Visible = false;
			originalNotice.Visible = false;

			var duplicateHotkey = manager.GetFirstDuplicate(definition.Name, hotkeyEntry.Key, definition);
			if (duplicateHotkey != null)
			{
				duplicateNotice.GetText = () => duplicateNotice.Text.F(duplicateHotkey.Description);
				duplicateNotice.Visible = true;
				acceptButton.Disabled = true;
			}
			else
			{
				acceptButton.Disabled = hotkeyEntry.Key == originalHotkey;
			}

			if (!hotkeyEntry.Key.IsValid())
				clearButton.Disabled = true;
			else
				clearButton.Disabled = false;

			if (hotkeyEntry.Key == definition.Default || (!hotkeyEntry.Key.IsValid() && !definition.Default.IsValid()))
			{
				defaultNotice.Visible = duplicateNotice.Visible ? false : true;
				resetButton.Disabled = true;
			}
			else
			{
				originalNotice.Visible = duplicateNotice.Visible ? false : true;
				resetButton.Disabled = false;
			}
		}

		void Accept()
		{
			manager.Set(definition.Name, hotkeyEntry.Key);
			Ui.CloseWindow();
			Game.Settings.Save();
			onExit();
		}

		void Cancel()
		{
			Ui.CloseWindow();
		}

		void Reset()
		{
			hotkeyEntry.Key = definition.Default;
			hotkeyEntry.YieldKeyboardFocus();
		}

		void Clear()
		{
			hotkeyEntry.Key = Hotkey.Invalid;
			hotkeyEntry.YieldKeyboardFocus();
		}
	}
}
