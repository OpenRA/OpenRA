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

namespace OpenRA.Mods.Common.Widgets
{
	public class SpritePowerMeterLogic : ChromeLogic
	{
		readonly PowerManager powerManager;
		readonly SpritePowerMeterWidget powerMeter;
		readonly Widget sidebarProduction;

		int barHeight;
		bool bypassAnimation;
		int lastMeterCheck;
		int warningFlash = 0;
		int lastTotalPowerDisplay;

		[ObjectCreator.UseCtor]
		public SpritePowerMeterLogic(Widget widget, World world)
		{
			powerManager = world.LocalPlayer.PlayerActor.Trait<PowerManager>();
			powerMeter = widget.Get<SpritePowerMeterWidget>("POWERMETER");

			sidebarProduction = powerMeter.Parent.Get(powerMeter.ParentContainer);
		}

		void CalculateMeterBarDimensions()
		{
			// Height of power meter in pixels
			var newBarHeight = 0;
			foreach (var child in sidebarProduction.Children)
				if (child.Id == powerMeter.MeterAlongside)
					newBarHeight += child.Bounds.Height;

			if (newBarHeight != barHeight)
			{
				barHeight = newBarHeight;

				// Don't animate the meter after changing sidebars
				bypassAnimation = true;
			}
		}

		void CheckBarNumber()
		{
			var meterDistance = powerMeter.MeterHeight;
			var numberOfBars = decimal.Floor(barHeight / meterDistance);

			if (powerMeter.Children.Count == numberOfBars)
				return;

			powerMeter.Children.Clear();

			// Create a list of new bars
			for (int i = 0; i < numberOfBars; i++)
			{
				var newPower = new ImageWidget();
				newPower.ImageCollection = powerMeter.ImageCollection;
				newPower.ImageName = powerMeter.NoPowerImage;

				// AddFactionSuffixLogic could be added here
				newPower.Bounds.Y = -(i * meterDistance) + barHeight + powerMeter.Bounds.Y;
				newPower.Bounds.X = powerMeter.Bounds.X;
				newPower.GetImageName = () => newPower.ImageName;
				powerMeter.Children.Add(newPower);
			}
		}

		void CheckFlash(PowerManager powerManager, int totalPowerDisplay)
		{
			var startWarningFlash = powerManager.PowerState != PowerState.Normal;

			if (lastTotalPowerDisplay != totalPowerDisplay)
			{
				startWarningFlash = true;
				lastTotalPowerDisplay = totalPowerDisplay;
			}

			if (startWarningFlash && warningFlash <= 0)
				warningFlash = 10;
		}

		public override void Tick()
		{
			CalculateMeterBarDimensions();
			CheckBarNumber();

			// If just changed power level or low power, flash the last bar meter
			lastMeterCheck++;
			if (lastMeterCheck < powerMeter.TickWait)
				return;

			lastMeterCheck = 0;

			// Number of power units represent each bar
			var stepSize = powerMeter.PowerUnitsPerBar;

			var totalPowerDisplay = Math.Max(powerManager.PowerProvided, powerManager.PowerDrained);

			var totalPowerStep = decimal.Floor(totalPowerDisplay / stepSize);
			var powerUsedStep = decimal.Floor(powerManager.PowerDrained / stepSize);
			var powerAvailableStep = decimal.Floor(powerManager.PowerProvided / stepSize);

			// Display a percentage if the bar is maxed out
			if (totalPowerStep > powerMeter.Children.Count)
			{
				var powerFraction = (float)powerMeter.Children.Count / (float)totalPowerStep;
				totalPowerDisplay = (int)((float)totalPowerDisplay * powerFraction);
				totalPowerStep = (int)((float)totalPowerStep * powerFraction);
				powerUsedStep = (int)((float)powerUsedStep * powerFraction);
				powerAvailableStep = (int)((float)powerAvailableStep * powerFraction);
			}

			CheckFlash(powerManager, totalPowerDisplay);

			for (var i = 0; i < powerMeter.Children.Count; i++)
			{
				var image = powerMeter.Children[i] as ImageWidget;
				if (image == null)
					continue;

				if (i > totalPowerStep || totalPowerStep == 0)
				{
					image.ImageName = powerMeter.NoPowerImage;
					continue;
				}

				var targetIcon = powerMeter.AvailablePowerImage;

				if (i < powerUsedStep)
					targetIcon = powerMeter.UsedPowerImage;

				if (i > powerAvailableStep)
					targetIcon = powerMeter.OverUsedPowerImage;

				if (i == totalPowerStep && powerManager.PowerState == PowerState.Low)
					targetIcon = powerMeter.OverUsedPowerImage;

				// Flash the top bar if something is wrong
				if (i == totalPowerStep)
				{
					if (warningFlash % 2 != 0)
						targetIcon = powerMeter.FlashPowerImage;
					if (warningFlash > 0)
						warningFlash--;
				}

				// We exit if updating a bar meter. This gives a nice animation effect
				if (image.ImageName != targetIcon)
				{
					image.ImageName = targetIcon;
					if (!bypassAnimation)
						return;
				}
			}

			bypassAnimation = false;
		}
	}
}
