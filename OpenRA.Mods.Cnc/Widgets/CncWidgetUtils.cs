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

namespace OpenRA.Mods.Cnc.Widgets
{
	public static class CncWidgetUtils
	{
		public static void PromptConfirmAction(string title, string text, Action onConfirm, Action onCancel)
		{
			var prompt = Ui.OpenWindow("CONFIRM_PROMPT");
			prompt.Get<LabelWidget>("PROMPT_TITLE").GetText = () => title;
			prompt.Get<LabelWidget>("PROMPT_TEXT").GetText = () => text;

			prompt.Get<ButtonWidget>("CONFIRM_BUTTON").OnClick = () =>
			{
				Ui.CloseWindow();
				onConfirm();
			};

			prompt.Get<ButtonWidget>("CANCEL_BUTTON").OnClick = () =>
			{
				Ui.CloseWindow();
				onCancel();
			};
		}
	}
}
