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
	class KickSpectatorsLogic
	{
		[ObjectCreator.UseCtor]
		public KickSpectatorsLogic(Widget widget, string clientCount, Action okPressed, Action cancelPressed)
		{
			widget.Get<LabelWidget>("TEXT").GetText = () => "Are you sure you want to kick {0} spectators?".F(clientCount);

			widget.Get<ButtonWidget>("OK_BUTTON").OnClick = () =>
			{
				widget.Parent.RemoveChild(widget);
				okPressed();
			};

			widget.Get<ButtonWidget>("CANCEL_BUTTON").OnClick = () =>
			{
				widget.Parent.RemoveChild(widget);
				cancelPressed();
			};
		}
	}
}
