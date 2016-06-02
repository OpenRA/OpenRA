#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public static class ConfirmationDialogs
	{
		public static void PromptConfirmAction(
			string title,
			string text,
			Action onConfirm,
			Action onCancel = null,
			Action onOther = null,
			string confirmText = null,
			string cancelText = null,
			string otherText = null)
		{
			var promptName = onOther != null ? "CONFIRM_PROMPT_THREEBUTTON" : "CONFIRM_PROMPT_TWOBUTTON";
			var prompt = Ui.OpenWindow(promptName);
			var confirmButton = prompt.Get<ButtonWidget>("CONFIRM_BUTTON");
			var cancelButton = prompt.GetOrNull<ButtonWidget>("CANCEL_BUTTON");
			var otherButton = prompt.GetOrNull<ButtonWidget>("OTHER_BUTTON");

			prompt.Get<LabelWidget>("PROMPT_TITLE").GetText = () => title;
			prompt.Get<LabelWidget>("PROMPT_TEXT").GetText = () => text;
			if (!string.IsNullOrEmpty(confirmText))
				confirmButton.GetText = () => confirmText;
			if (!string.IsNullOrEmpty(otherText) && otherButton != null)
				otherButton.GetText = () => otherText;
			if (!string.IsNullOrEmpty(cancelText) && cancelButton != null)
				cancelButton.GetText = () => cancelText;

			confirmButton.OnClick = () =>
			{
				Ui.CloseWindow();
				onConfirm();
			};

			if (onCancel != null && cancelButton != null)
			{
				cancelButton.IsVisible = () => true;
				cancelButton.OnClick = () =>
				{
					Ui.CloseWindow();
					if (onCancel != null)
						onCancel();
				};
			}
			else if (cancelButton != null)
				cancelButton.IsVisible = () => false;

			if (onOther != null && otherButton != null)
			{
				otherButton.IsVisible = () => true;
				otherButton.OnClick = () =>
				{
					if (onOther != null)
						onOther();
                };
			}
			else if (otherButton != null)
				otherButton.IsVisible = () => false;
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
