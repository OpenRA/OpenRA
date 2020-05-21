#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
		public static void ButtonPrompt(
			string title,
			string text,
			Action onConfirm = null,
			Action onCancel = null,
			Action onOther = null,
			string confirmText = null,
			string cancelText = null,
			string otherText = null)
		{
			var promptName = onOther != null ? "THREEBUTTON_PROMPT" : "TWOBUTTON_PROMPT";
			var prompt = Ui.OpenWindow(promptName);
			var confirmButton = prompt.GetOrNull<ButtonWidget>("CONFIRM_BUTTON");
			var cancelButton = prompt.GetOrNull<ButtonWidget>("CANCEL_BUTTON");
			var otherButton = prompt.GetOrNull<ButtonWidget>("OTHER_BUTTON");

			prompt.Get<LabelWidget>("PROMPT_TITLE").GetText = () => title;

			var headerTemplate = prompt.Get<LabelWidget>("PROMPT_TEXT");
			var headerLines = text.Replace("\\n", "\n").Split('\n');
			var headerHeight = 0;
			foreach (var l in headerLines)
			{
				var line = (LabelWidget)headerTemplate.Clone();
				line.GetText = () => l;
				line.Node.Top = (int)line.Node.LayoutY + headerHeight;
				line.Node.CalculateLayout();
				prompt.AddChild(line);

				headerHeight += (int)headerTemplate.Node.LayoutHeight;
			}

			prompt.Node.Height = (int)prompt.Node.LayoutHeight + headerHeight;
			prompt.Node.Top = (int)prompt.Node.LayoutY - headerHeight / 2;
			prompt.Node.CalculateLayout();

			if (onConfirm != null && confirmButton != null)
			{
				confirmButton.VisibilityFunction = () => true;
				confirmButton.Node.Top = (int)confirmButton.Node.LayoutY + headerHeight;
				confirmButton.Node.CalculateLayout();
				confirmButton.OnClick = () =>
				{
					Ui.CloseWindow();
					onConfirm();
				};

				if (!string.IsNullOrEmpty(confirmText))
					confirmButton.GetText = () => confirmText;
			}

			if (onCancel != null && cancelButton != null)
			{
				cancelButton.VisibilityFunction = () => true;
				cancelButton.Node.Top = (int)cancelButton.Node.LayoutY + headerHeight;
				cancelButton.Node.CalculateLayout();
				cancelButton.OnClick = () =>
				{
					Ui.CloseWindow();
					if (onCancel != null)
						onCancel();
				};

				if (!string.IsNullOrEmpty(cancelText) && cancelButton != null)
					cancelButton.GetText = () => cancelText;
			}

			if (onOther != null && otherButton != null)
			{
				otherButton.VisibilityFunction = () => true;
				otherButton.Node.Top = (int)otherButton.Node.LayoutY + headerHeight;
				otherButton.Node.CalculateLayout();
				otherButton.OnClick = () =>
				{
					if (onOther != null)
						onOther();
				};

				if (!string.IsNullOrEmpty(otherText) && otherButton != null)
					otherButton.GetText = () => otherText;
			}
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
