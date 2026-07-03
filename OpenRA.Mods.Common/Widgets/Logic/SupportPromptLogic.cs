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

using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class SupportPromptLogic : ChromeLogic
	{
		const string SupportEmail = "Loosetop@Yahoo.com";

		[ObjectCreator.UseCtor]
		public SupportPromptLogic(Widget widget)
		{
			widget.Get<ButtonWidget>("OK_BUTTON").OnClick = Ui.CloseWindow;

			var emailButton = widget.Get<ButtonWidget>("EMAIL_BUTTON");
			var emailText = emailButton.GetText;
			emailButton.OnClick = () =>
			{
				Game.SetClipboardText(SupportEmail);
				emailButton.GetText = () => "Copied!";
				Game.RunAfterDelay(2000, () => emailButton.GetText = emailText);
			};
		}
	}
}
