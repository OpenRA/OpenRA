#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public static class ConfirmationDialogs
	{
		public static void PromptConfirmAction(string title, string text, Action onConfirm, Action onCancel = null, string confirmText = null, string cancelText = null)
		{
			var prompt = Ui.OpenWindow("CONFIRM_PROMPT");
			prompt.Get<LabelWidget>("PROMPT_TITLE").GetText = () => title;
			prompt.Get<LabelWidget>("PROMPT_TEXT").GetText = () => text;
			if (!string.IsNullOrEmpty(confirmText))
				prompt.Get<ButtonWidget>("CONFIRM_BUTTON").GetText = () => confirmText;
			if (!string.IsNullOrEmpty(cancelText))
				prompt.Get<ButtonWidget>("CANCEL_BUTTON").GetText = () => cancelText;

			prompt.Get<ButtonWidget>("CONFIRM_BUTTON").OnClick = () =>
			{
				Ui.CloseWindow();
				onConfirm();
			};

			prompt.Get<ButtonWidget>("CANCEL_BUTTON").OnClick = () =>
			{
				Ui.CloseWindow();
				if (onCancel != null)
					onCancel();
			};
		}

		public static void CancelPrompt(string title, string text, Action onCancel = null, string cancelText = null)
		{
			var prompt = Ui.OpenWindow("CANCEL_PROMPT");
			prompt.Get<LabelWidget>("PROMPT_TITLE").GetText = () => title;
			prompt.Get<LabelWidget>("PROMPT_TEXT").GetText = () => text;

			if (!string.IsNullOrEmpty(cancelText))
				prompt.Get<ButtonWidget>("CANCEL_BUTTON").GetText = () => cancelText;

			prompt.Get<ButtonWidget>("CANCEL_BUTTON").OnClick = () =>
			{
				Ui.CloseWindow();
				if (onCancel != null)
					onCancel();
			};
		}
		/**
		 * open confirmation dialog for mission / game restart
		 */
		public static void PromptAbortMission(World world, string title, string text, Action onAbort, Action onCancel = null, Action closeMenu = null)
		{
			var isMultiplayer = !world.LobbyInfo.IsSinglePlayer && !world.IsReplay;
			var prompt = Ui.OpenWindow("ABORT_MISSION_PROMPT");
			prompt.Get<LabelWidget>("PROMPT_TITLE").GetText = () => title;
			prompt.Get<LabelWidget>("PROMPT_TEXT").GetText = () => text;
			prompt.Get<ButtonWidget>("ABORT_BUTTON").OnClick = () =>
			{
				Ui.CloseWindow();
				onAbort();
			};

			var restartButton = prompt.Get<ButtonWidget>("RESTART_BUTTON");
			restartButton.IsVisible = () => !isMultiplayer;
			restartButton.OnClick = () =>
			{
				if (closeMenu != null)
					closeMenu();

				Ui.CloseWindow();
				Game.RestartGame();
			};

			prompt.Get<ButtonWidget>("CANCEL_BUTTON").OnClick = () =>
			{
				Ui.CloseWindow();
				if (onCancel != null)
					onCancel();
			};
		}

		public static void TextInputPrompt(
			string title, string prompt, string initialText,
			Action<string> onAccept, Action onCancel = null,
			string acceptText = null, string cancelText = null,
			Func<string, bool> inputValidator = null)
		{
			var panel = Ui.OpenWindow("TEXT_INPUT_PROMPT");
			Func<bool> doValidate = null;
			ButtonWidget acceptButton = null, cancelButton = null;

			// Title
			panel.Get<LabelWidget>("PROMPT_TITLE").GetText = () => title;

			// Prompt
			panel.Get<LabelWidget>("PROMPT_TEXT").GetText = () => prompt;

			// Text input
			var input = panel.Get<TextFieldWidget>("INPUT_TEXT");
			var isValid = false;
			input.Text = initialText;
			input.IsValid = () => isValid;
			input.OnEnterKey = () =>
			{
				if (acceptButton.IsDisabled())
					return false;

				acceptButton.OnClick();
				return true;
			};
			input.OnEscKey = () =>
			{
				if (cancelButton.IsDisabled())
					return false;

				cancelButton.OnClick();
				return true;
			};
			input.TakeKeyboardFocus();
			input.CursorPosition = input.Text.Length;
			input.OnTextEdited = () => doValidate();

			// Buttons
			acceptButton = panel.Get<ButtonWidget>("ACCEPT_BUTTON");
			if (!string.IsNullOrEmpty(acceptText))
				acceptButton.GetText = () => acceptText;

			acceptButton.OnClick = () =>
			{
				if (!doValidate())
					return;

				Ui.CloseWindow();
				onAccept(input.Text);
			};

			cancelButton = panel.Get<ButtonWidget>("CANCEL_BUTTON");
			if (!string.IsNullOrEmpty(cancelText))
				cancelButton.GetText = () => cancelText;

			cancelButton.OnClick = () =>
			{
				Ui.CloseWindow();
				if (onCancel != null)
					onCancel();
			};

			// Validation
			doValidate = () =>
			{
				if (inputValidator == null)
					return true;

				isValid = inputValidator(input.Text);
				if (isValid)
				{
					acceptButton.Disabled = false;
					return true;
				}

				acceptButton.Disabled = true;
				return false;
			};

			doValidate();
		}
	}
}
