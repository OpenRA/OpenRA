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

namespace OpenRA.Mods.Common.Widgets.Logic
{
	sealed class KickClientLogic : ChromeLogic
	{
		[TranslationReference("player")]
		const string KickClient = "dialog-kick-client.prompt";

		[ObjectCreator.UseCtor]
		public KickClientLogic(Widget widget, string clientName, Action<bool> okPressed, Action cancelPressed)
		{
			var kickMessage = TranslationProvider.GetString(KickClient, Translation.Arguments("player", clientName));
			widget.Get<LabelWidget>("TITLE").GetText = () => kickMessage;

			var tempBan = false;
			var preventRejoiningCheckbox = widget.Get<CheckboxWidget>("PREVENT_REJOINING_CHECKBOX");
			preventRejoiningCheckbox.IsChecked = () => tempBan;
			preventRejoiningCheckbox.OnClick = () => tempBan ^= true;

			widget.Get<ButtonWidget>("OK_BUTTON").OnClick = () =>
			{
				widget.Parent.RemoveChild(widget);
				okPressed(tempBan);
			};

			widget.Get<ButtonWidget>("CANCEL_BUTTON").OnClick = () =>
			{
				widget.Parent.RemoveChild(widget);
				cancelPressed();
			};
		}
	}
}
