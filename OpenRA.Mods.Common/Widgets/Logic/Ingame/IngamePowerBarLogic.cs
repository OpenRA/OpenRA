#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
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
		[TranslationReference]
		static readonly string PowerUsage = "power-usage";

		[ObjectCreator.UseCtor]
		public IngamePowerBarLogic(Widget widget, ModData modData, World world)
		{
			var powerManager = world.LocalPlayer.PlayerActor.Trait<PowerManager>();
			var powerBar = widget.Get<ResourceBarWidget>("POWERBAR");

			powerBar.GetProvided = () => powerManager.PowerProvided;
			powerBar.GetUsed = () => powerManager.PowerDrained;
			powerBar.TooltipFormat = modData.Translation.GetString(PowerUsage) + ": {0}/{1}";
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
