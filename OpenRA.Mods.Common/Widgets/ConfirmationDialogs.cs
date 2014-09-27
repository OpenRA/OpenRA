#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
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

		public static void TextInputPrompt(
			string title, string prompt, string initialText,
			Action<string> onAccept, Action onCancel = null,
			string acceptText = null, string cancelText = null,
			Func<string, bool> inputValidator = null)
		{
			var panel = Ui.OpenWindow("TEXT_INPUT_PROMPT");
			Func<bool> doValidate = null;
			ButtonWidget acceptButton = null, cancelButton = null;

			//
			// Title
			//
			panel.Get<LabelWidget>("PROMPT_TITLE").GetText = () => title;

			//
			// Prompt
			//
			panel.Get<LabelWidget>("PROMPT_TEXT").GetText = () => prompt;

			//
			// Text input
			//
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

			//
			// Buttons
			//
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

			//
			// Validation
			//
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
