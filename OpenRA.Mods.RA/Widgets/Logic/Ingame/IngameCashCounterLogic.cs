#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets.Logic
{
	public class IngameCashCounterLogic
	{
		[ObjectCreator.UseCtor]
		public IngameCashCounterLogic(Widget widget, World world)
		{
			var playerResources = world.LocalPlayer.PlayerActor.Trait<PlayerResources>();
			var cash = widget.Get<LabelWidget>("CASH");
			var label = cash.Text;

			cash.GetText = () => label.F(playerResources.DisplayCash + playerResources.DisplayResources);
		}
	}
}
