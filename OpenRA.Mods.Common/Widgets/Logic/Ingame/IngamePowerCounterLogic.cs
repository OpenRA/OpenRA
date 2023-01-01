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
	public class IngamePowerCounterLogic : ChromeLogic
	{
		[TranslationReference("usage", "capacity")]
		const string PowerUsage = "label-power-usage";

		[TranslationReference]
		const string Infinite = "label-infinite-power";

		[ObjectCreator.UseCtor]
		public IngamePowerCounterLogic(Widget widget, ModData modData, World world)
		{
			var developerMode = world.LocalPlayer.PlayerActor.Trait<DeveloperMode>();

			var powerManager = world.LocalPlayer.PlayerActor.Trait<PowerManager>();
			var power = widget.Get<LabelWithTooltipWidget>("POWER");
			var powerIcon = widget.Get<ImageWidget>("POWER_ICON");
			var unlimitedCapacity = modData.Translation.GetString(Infinite);

			powerIcon.GetImageName = () => powerManager.ExcessPower < 0 ? "power-critical" : "power-normal";
			power.GetColor = () => powerManager.ExcessPower < 0 ? Color.Red : Color.White;
			power.GetText = () => developerMode.UnlimitedPower ? unlimitedCapacity : powerManager.ExcessPower.ToString();

			var tooltipTextCached = new CachedTransform<(string, string), string>(((string usage, string capacity) args) =>
			{
				return modData.Translation.GetString(
					PowerUsage,
					Translation.Arguments("usage", args.usage, "capacity", args.capacity));
			});

			power.GetTooltipText = () =>
			{
				var capacity = developerMode.UnlimitedPower ? unlimitedCapacity : powerManager.PowerProvided.ToString();

				return tooltipTextCached.Update((powerManager.PowerDrained.ToString(), capacity));
			};
		}
	}
}
