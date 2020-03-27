#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic.Ingame
{
	public class SpritePowerMeterLogic : ChromeLogic
	{
		readonly PowerManager powerManager;
		readonly SpritePowerMeterWidget powerMeter;

		[ObjectCreator.UseCtor]
		public SpritePowerMeterLogic(World world, Widget widget)
		{
			powerManager = world.LocalPlayer.PlayerActor.Trait<PowerManager>();
			powerMeter = widget.Get<SpritePowerMeterWidget>("POWER_BAR");

			powerMeter.GetTooltipText = () => "Power Usage: " + powerManager.PowerDrained.ToString() +
				(powerManager.PowerProvided != 1000000 ? "/" + powerManager.PowerProvided.ToString() : "");
		}

		void CheckFlash()
		{
			var startWarningFlash = powerManager.PowerState != PowerState.Normal;

			if (powerMeter.LastTotalPowerDisplay != powerMeter.TotalPowerDisplay)
			{
				startWarningFlash = true;
				powerMeter.LastTotalPowerDisplay = powerMeter.TotalPowerDisplay;
			}

			if (startWarningFlash && powerMeter.WarningFlash <= 0)
				powerMeter.WarningFlash = powerMeter.WarningFlashDuration;
		}

		public override void Tick()
		{
			powerMeter.LowPower = powerManager.PowerState == PowerState.Low;

			powerMeter.TotalPowerDisplay = Math.Max(powerManager.PowerProvided, powerManager.PowerDrained);

			powerMeter.TotalPowerStep = powerMeter.TotalPowerDisplay / powerMeter.PowerUnitsPerBar;
			powerMeter.PowerUsedStep = powerManager.PowerDrained / powerMeter.PowerUnitsPerBar;
			powerMeter.PowerAvailableStep = powerManager.PowerProvided / powerMeter.PowerUnitsPerBar;

			// Display a percentage if the bar is maxed out
			if (powerMeter.TotalPowerStep > powerMeter.NumberOfBars)
			{
				var powerFraction = powerMeter.NumberOfBars / powerMeter.TotalPowerStep;
				powerMeter.TotalPowerDisplay = (int)(powerMeter.TotalPowerDisplay * powerFraction);
				powerMeter.TotalPowerStep *= powerFraction;
				powerMeter.PowerUsedStep *= powerFraction;
				powerMeter.PowerAvailableStep *= powerFraction;
			}

			CheckFlash();
		}
	}
}
