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
	public class IngamePowerCounterLogic : ChromeLogic
	{
		[TranslationReference]
		static readonly string PowerUsage = "power-usage";

		[ObjectCreator.UseCtor]
		public IngamePowerCounterLogic(Widget widget, ModData modData, World world)
		{
			var powerManager = world.LocalPlayer.PlayerActor.Trait<PowerManager>();
			var power = widget.Get<LabelWithTooltipWidget>("POWER");
			var powerIcon = widget.Get<ImageWidget>("POWER_ICON");

			powerIcon.GetImageName = () => powerManager.ExcessPower < 0 ? "power-critical" : "power-normal";
			power.GetColor = () => powerManager.ExcessPower < 0 ? Color.Red : Color.White;
			power.GetText = () => powerManager.PowerProvided == 1000000 ? "∞" : powerManager.ExcessPower.ToString();
			power.GetTooltipText = () => modData.Translation.GetString(PowerUsage) + ": " + powerManager.PowerDrained.ToString() +
				(powerManager.PowerProvided != 1000000 ? "/" + powerManager.PowerProvided.ToString() : "");
		}
	}
}
