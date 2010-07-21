#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Widgets
{
	public class PowerBinWidget : Widget
	{
		// Power bar 
		static float2 powerOrigin = new float2(42, 205); // Relative to radarOrigin
		static Size powerSize = new Size(138, 5);
		float? lastPowerProvidedPos;
		float? lastPowerDrainedPos;
		string powerCollection;

		public override void DrawInner(World world)
		{
			powerCollection = "power-" + world.LocalPlayer.Country.Race;

			var resources = world.LocalPlayer.PlayerActor.traits.Get<PlayerResources>();

			// Nothing to draw
			if (resources.PowerProvided == 0
				&& resources.PowerDrained == 0)
				return;

			// Draw bar horizontally
			var barStart = powerOrigin + RadarBinWidget.radarOrigin;
			var barEnd = barStart + new float2(powerSize.Width, 0);

			float powerScaleBy = 100;
			var maxPower = Math.Max(resources.PowerProvided, resources.PowerDrained);
			while (maxPower >= powerScaleBy) powerScaleBy *= 2;

			// Current power supply
			var powerLevelTemp = barStart.X + (barEnd.X - barStart.X) * (resources.PowerProvided / powerScaleBy);
			lastPowerProvidedPos = float2.Lerp(lastPowerProvidedPos.GetValueOrDefault(powerLevelTemp), powerLevelTemp, .3f);
			float2 powerLevel = new float2(lastPowerProvidedPos.Value, barStart.Y);

			var color = Color.LimeGreen;
			if (resources.GetPowerState() == PowerState.Low)
				color = Color.Orange;
			if (resources.GetPowerState() == PowerState.Critical)
				color = Color.Red;

			var colorDark = Graphics.Util.Lerp(0.25f, color, Color.Black);
			for (int i = 0; i < powerSize.Height; i++)
			{
				color = (i - 1 < powerSize.Height / 2) ? color : colorDark;
				float2 leftOffset = new float2(0, i);
				float2 rightOffset = new float2(0, i);
				// Indent corners
				if ((i == 0 || i == powerSize.Height - 1) && powerLevel.X - barStart.X > 1)
				{
					leftOffset.X += 1;
					rightOffset.X -= 1;
				}
				Game.Renderer.LineRenderer.DrawLine(Game.viewport.Location + barStart + leftOffset, Game.viewport.Location + powerLevel + rightOffset, color, color);
			}
			Game.Renderer.LineRenderer.Flush();

			// Power usage indicator
			var indicator = ChromeProvider.GetImage( powerCollection, "power-indicator");
			var powerDrainedTemp = barStart.X + (barEnd.X - barStart.X) * (resources.PowerDrained / powerScaleBy);
			lastPowerDrainedPos = float2.Lerp(lastPowerDrainedPos.GetValueOrDefault(powerDrainedTemp), powerDrainedTemp, .3f);
			float2 powerDrainLevel = new float2(lastPowerDrainedPos.Value - indicator.size.X / 2, barStart.Y - 1);

			Game.Renderer.RgbaSpriteRenderer.DrawSprite(indicator, powerDrainLevel, "chrome");
			Game.Renderer.RgbaSpriteRenderer.Flush();
		}
	}
}
