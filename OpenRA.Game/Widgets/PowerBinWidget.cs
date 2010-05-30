#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion


using System;
using System.Drawing;
using System.Linq;
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
		
		public override Widget Clone() { throw new NotImplementedException("Why are you Cloning PowerBin?"); }
		
		public override void Draw(World world)
		{
			powerCollection = "power-" + world.LocalPlayer.Country.Race;
			
			var resources = world.LocalPlayer.PlayerActor.traits.Get<PlayerResources>();

			// Nothing to draw
			if (resources.PowerProvided == 0
				&& resources.PowerDrained == 0)
				return;

			var renderer = Game.chrome.renderer;
			var lineRenderer = Game.chrome.lineRenderer;
			var rgbaRenderer = renderer.RgbaSpriteRenderer;

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
				lineRenderer.DrawLine(Game.viewport.Location + barStart + leftOffset, Game.viewport.Location + powerLevel + rightOffset, color, color);
			}
			lineRenderer.Flush();

			// Power usage indicator
			var indicator = ChromeProvider.GetImage(renderer, powerCollection, "power-indicator");
			var powerDrainedTemp = barStart.X + (barEnd.X - barStart.X) * (resources.PowerDrained / powerScaleBy);
			lastPowerDrainedPos = float2.Lerp(lastPowerDrainedPos.GetValueOrDefault(powerDrainedTemp), powerDrainedTemp, .3f);
			float2 powerDrainLevel = new float2(lastPowerDrainedPos.Value - indicator.size.X / 2, barStart.Y - 1);

			rgbaRenderer.DrawSprite(indicator, powerDrainLevel, "chrome");
			rgbaRenderer.Flush();
		}
	}
}
