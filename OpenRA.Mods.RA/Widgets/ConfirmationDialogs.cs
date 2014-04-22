#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets
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
	}
}
