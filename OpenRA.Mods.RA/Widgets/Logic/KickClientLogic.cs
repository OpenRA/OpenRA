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

namespace OpenRA.Mods.RA.Widgets.Logic
{
	class KickClientLogic
	{
		[ObjectCreator.UseCtor]
		public KickClientLogic(Widget widget, string clientName, Action<bool> okPressed, Action cancelPressed)
		{
			widget.Get<LabelWidget>("TITLE").GetText = () => "Kick {0}?".F(clientName);

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
