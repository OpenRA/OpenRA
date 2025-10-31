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
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public static class ConfirmationDialogs
	{
		public static void ButtonPrompt(
			ModData modData,
			string title,
			string text,
			object[] titleArguments = null,
			object[] textArguments = null,
			Action onConfirm = null,
			string confirmText = null,
			Action onCancel = null,
			string cancelText = null,
			Action onOther = null,
			string otherText = null)
		{
			var promptName = onOther != null ? "THREEBUTTON_PROMPT" : "TWOBUTTON_PROMPT";
			var prompt = Ui.OpenWindow(promptName);
			var confirmButton = prompt.GetOrNull<ButtonWidget>("CONFIRM_BUTTON");
			var cancelButton = prompt.GetOrNull<ButtonWidget>("CANCEL_BUTTON");
			var otherButton = prompt.GetOrNull<ButtonWidget>("OTHER_BUTTON");

			var titleMessage = FluentProvider.GetMessage(title, titleArguments);
			prompt.Get<LabelWidget>("PROMPT_TITLE").GetText = () => titleMessage;

			var headerTemplate = prompt.Get<LabelWidget>("PROMPT_TEXT");
			var textMessage = FluentProvider.GetMessage(text, textArguments);
			var headerLines = textMessage.Split('\n');
			var headerHeight = 0;
			foreach (var l in headerLines)
			{
				var line = headerTemplate.Clone();
				line.GetText = () => l;
				line.Bounds.Y += headerHeight;
				prompt.AddChild(line);

				headerHeight += headerTemplate.Bounds.Height;
			}

			prompt.Bounds.Height += headerHeight;
			prompt.Bounds.Y -= headerHeight / 2;

			if (onConfirm != null && confirmButton != null)
			{
				confirmButton.Visible = true;
				confirmButton.Bounds.Y += headerHeight;
				confirmButton.OnClick = () =>
				{
					Ui.CloseWindow();
					onConfirm();
				};

				if (!string.IsNullOrEmpty(confirmText))
				{
					var confirmTextMessage = FluentProvider.GetMessage(confirmText);
					confirmButton.GetText = () => confirmTextMessage;
				}
			}

			if (onCancel != null && cancelButton != null)
			{
				cancelButton.Visible = true;
				cancelButton.Bounds.Y += headerHeight;
				cancelButton.OnClick = () =>
				{
					Ui.CloseWindow();
					onCancel();
				};

				if (!string.IsNullOrEmpty(cancelText))
				{
					var cancelTextMessage = FluentProvider.GetMessage(cancelText);
					cancelButton.GetText = () => cancelTextMessage;
				}
			}

			if (onOther != null && otherButton != null)
			{
				otherButton.Visible = true;
				otherButton.Bounds.Y += headerHeight;
				otherButton.OnClick = onOther;

				if (!string.IsNullOrEmpty(otherText))
				{
					var otherTextMessage = FluentProvider.GetMessage(otherText);
					otherButton.GetText = () => otherTextMessage;
				}
			}
		}

		public static void TextInputPrompt(ModData modData,
			string title, string prompt, string initialText,
			Action<string> onAccept, Action onCancel = null,
			string acceptText = null, string cancelText = null,
			Func<string, bool> inputValidator = null)
		{
			var panel = Ui.OpenWindow("TEXT_INPUT_PROMPT");
			Func<bool> doValidate = null;
			ButtonWidget acceptButton = null, cancelButton = null;

			var titleMessage = FluentProvider.GetMessage(title);
			panel.Get<LabelWidget>("PROMPT_TITLE").GetText = () => titleMessage;

			var promptMessage = FluentProvider.GetMessage(prompt);
			panel.Get<LabelWidget>("PROMPT_TEXT").GetText = () => promptMessage;

			var input = panel.Get<TextFieldWidget>("INPUT_TEXT");
			var isValid = false;
			input.Text = initialText;
			input.IsValid = () => isValid;
			input.OnEnterKey = _ =>
			{
				if (acceptButton.IsDisabled())
					return false;

				acceptButton.OnClick();
				return true;
			};
			input.OnEscKey = _ =>
			{
				if (cancelButton.IsDisabled())
					return false;

				cancelButton.OnClick();
				return true;
			};
			input.TakeKeyboardFocus();
			input.CursorPosition = input.Text.Length;
			input.OnTextEdited = () => doValidate();

			acceptButton = panel.Get<ButtonWidget>("ACCEPT_BUTTON");
			if (!string.IsNullOrEmpty(acceptText))
			{
				var acceptTextMessage = FluentProvider.GetMessage(acceptText);
				acceptButton.GetText = () => acceptTextMessage;
			}

			acceptButton.OnClick = () =>
			{
				if (!doValidate())
					return;

				Ui.CloseWindow();
				onAccept(input.Text);
			};

			cancelButton = panel.Get<ButtonWidget>("CANCEL_BUTTON");
			if (!string.IsNullOrEmpty(cancelText))
			{
				var cancelTextMessage = FluentProvider.GetMessage(cancelText);
				cancelButton.GetText = () => cancelTextMessage;
			}

			cancelButton.OnClick = () =>
			{
				Ui.CloseWindow();
				onCancel?.Invoke();
			};

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
