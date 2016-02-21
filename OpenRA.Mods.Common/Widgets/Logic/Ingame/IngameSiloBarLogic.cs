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

using System.Drawing;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class IngameSiloBarLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public IngameSiloBarLogic(Widget widget, World world)
		{
			var playerResources = world.LocalPlayer.PlayerActor.Trait<PlayerResources>();
			var siloBar = widget.Get<ResourceBarWidget>("SILOBAR");

			siloBar.GetProvided = () => playerResources.ResourceCapacity;
			siloBar.GetUsed = () => playerResources.Resources;
			siloBar.TooltipFormat = "Silo Usage: {0}/{1}";
			siloBar.GetBarColor = () =>
			{
				if (playerResources.Resources == playerResources.ResourceCapacity)
					return Color.Red;

				if (playerResources.Resources >= 0.8 * playerResources.ResourceCapacity)
					return Color.Orange;

				return Color.LimeGreen;
			};
		}
	}
}
