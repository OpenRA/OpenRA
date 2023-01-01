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
	public class IngamePowerBarLogic : ChromeLogic
	{
		[TranslationReference("usage", "capacity")]
		const string PowerUsage = "label-power-usage";

		[TranslationReference]
		const string Infinite = "label-infinite-power";

		[ObjectCreator.UseCtor]
		public IngamePowerBarLogic(Widget widget, ModData modData, World world)
		{
			var developerMode = world.LocalPlayer.PlayerActor.Trait<DeveloperMode>();
			var powerManager = world.LocalPlayer.PlayerActor.Trait<PowerManager>();
			var powerBar = widget.Get<ResourceBarWidget>("POWERBAR");

			powerBar.GetProvided = () => developerMode.UnlimitedPower ? -1 : powerManager.PowerProvided;
			powerBar.GetUsed = () => powerManager.PowerDrained;
			powerBar.TooltipTextCached = new CachedTransform<(float Current, float Capacity), string>(usage =>
			{
				var capacity = developerMode.UnlimitedPower ?
					modData.Translation.GetString(Infinite) :
					powerManager.PowerProvided.ToString();

				return modData.Translation.GetString(
					PowerUsage,
					Translation.Arguments("usage", usage.Current, "capacity", capacity));
			});

			powerBar.GetBarColor = () =>
			{
				if (powerManager.PowerState == PowerState.Critical)
					return Color.Red;
				if (powerManager.PowerState == PowerState.Low)
					return Color.Orange;
				return Color.LimeGreen;
			};
		}
	}
}
