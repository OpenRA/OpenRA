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

using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class IngameSiloBarLogic : ChromeLogic
	{
		[TranslationReference("usage", "capacity")]
		const string SiloUsage = "label-silo-usage";

		[ObjectCreator.UseCtor]
		public IngameSiloBarLogic(Widget widget, ModData modData, World world)
		{
			var playerResources = world.LocalPlayer.PlayerActor.Trait<PlayerResources>();
			var siloBar = widget.Get<ResourceBarWidget>("SILOBAR");

			siloBar.GetProvided = () => playerResources.ResourceCapacity;
			siloBar.GetUsed = () => playerResources.Resources;
			siloBar.TooltipTextCached = new CachedTransform<(float Current, float Capacity), string>(usage =>
			{
				return modData.Translation.GetString(
					SiloUsage,
					Translation.Arguments("usage", usage.Current, "capacity", usage.Capacity));
			});
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
