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
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class IngamePowerCounterLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public IngamePowerCounterLogic(Widget widget, World world)
		{
			var powerManager = world.LocalPlayer.PlayerActor.Trait<PowerManager>();
			var power = widget.Get<LabelWithTooltipWidget>("POWER");
			var powerIcon = widget.Get<ImageWidget>("POWER_ICON");

			powerIcon.GetImageName = () => powerManager.ExcessPower < 0 ? "power-critical" : "power-normal";
			power.GetColor = () => powerManager.ExcessPower < 0 ? Color.Red : Color.White;
			power.GetText = () => powerManager.PowerProvided == 1000000 ? "inf" : powerManager.ExcessPower.ToString();
			power.GetTooltipText = () => "Power Usage: " + powerManager.PowerDrained.ToString() +
				(powerManager.PowerProvided != 1000000 ? "/" + powerManager.PowerProvided.ToString() : "");
		}
	}
}
