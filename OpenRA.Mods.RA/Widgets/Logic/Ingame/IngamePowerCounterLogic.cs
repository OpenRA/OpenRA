#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.RA.Power;
using OpenRA.Widgets;
using System.Drawing;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class IngamePowerCounterLogic
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
			power.GetTooltipText = () => "Power Usage: " + powerManager.PowerDrained.ToString() + (powerManager.PowerProvided != 1000000 ? "/" + powerManager.PowerProvided.ToString() : "");
		}
	}
}
