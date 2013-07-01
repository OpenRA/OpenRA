#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets
{
	public class PowerBinWidget : Widget
	{
		// Power bar
		float2 powerOrigin = new float2(42, 205); // Relative to radarOrigin
		Size powerSize = new Size(138, 5);

		float? lastPowerProvidedPos;
		float? lastPowerDrainedPos;
		string powerCollection;

		readonly string RadarBin = "INGAME_RADAR_BIN";
		readonly PowerManager power;
		readonly World world;

		[ObjectCreator.UseCtor]
		public PowerBinWidget(World world)
		{
			this.world = world;

			if (world.LocalPlayer != null)
				power = world.LocalPlayer.PlayerActor.Trait<PowerManager>();
		}

		static Color GetPowerColor(PowerManager pm)
		{
			if (pm.PowerState == PowerState.Critical) return Color.Red;
			if (pm.PowerState == PowerState.Low) return Color.Orange;
			return Color.LimeGreen;
		}

		const float PowerBarLerpFactor = .2f;

		public override void Draw()
		{
			if( world.LocalPlayer == null ) return;
			if( world.LocalPlayer.WinState != WinState.Undefined ) return;

			var radarBin = Ui.Root.Get<SlidingContainerWidget>(RadarBin);

			powerCollection = "power-" + world.LocalPlayer.Country.Race;

			// Nothing to draw
			if (power.PowerProvided == 0 && power.PowerDrained == 0)
				return;

			// Draw bar horizontally
			var barStart = powerOrigin + radarBin.ChildOrigin;
			var barEnd = barStart + new float2(powerSize.Width, 0);

			float powerScaleBy = 100;
			var maxPower = Math.Max(power.PowerProvided, power.PowerDrained);

			while (maxPower >= powerScaleBy) powerScaleBy *= 2;

			// Current power supply
			var powerLevelTemp = barStart.X + (barEnd.X - barStart.X) * (power.PowerProvided / powerScaleBy);
			lastPowerProvidedPos = float2.Lerp(lastPowerProvidedPos.GetValueOrDefault(powerLevelTemp), powerLevelTemp, PowerBarLerpFactor);
			var powerLevel = new float2(lastPowerProvidedPos.Value, barStart.Y);

			var color = GetPowerColor(power);

			var colorDark = Exts.ColorLerp(0.25f, color, Color.Black);
			for (int i = 0; i < powerSize.Height; i++)
			{
				color = (i - 1 < powerSize.Height / 2) ? color : colorDark;
				var leftOffset = new float2(0, i);
				var rightOffset = new float2(0, i);
				// Indent corners
				if ((i == 0 || i == powerSize.Height - 1) && powerLevel.X - barStart.X > 1)
				{
					leftOffset.X += 1;
					rightOffset.X -= 1;
				}
				Game.Renderer.LineRenderer.DrawLine(barStart + leftOffset, powerLevel + rightOffset, color, color);
			}

			// Power usage indicator
			var indicator = ChromeProvider.GetImage( powerCollection, "power-indicator");
			var powerDrainedTemp = barStart.X + (barEnd.X - barStart.X) * (power.PowerDrained / powerScaleBy);
			lastPowerDrainedPos = float2.Lerp(lastPowerDrainedPos.GetValueOrDefault(powerDrainedTemp), powerDrainedTemp, PowerBarLerpFactor);
			var powerDrainLevel = new float2(lastPowerDrainedPos.Value - indicator.size.X / 2, barStart.Y - 1);

			Game.Renderer.RgbaSpriteRenderer.DrawSprite(indicator, powerDrainLevel);

			// Render the tooltip
			var rect = new Rectangle((int) barStart.X, (int) barStart.Y, powerSize.Width, powerSize.Height);
			
			if (rect.InflateBy(0, 5, 0, 5).Contains(Viewport.LastMousePos))
			{
				var pos = new int2(rect.Left + 5, rect.Top + 5);

				var border = WidgetUtils.GetBorderSizes("dialog4");
				WidgetUtils.DrawPanel("dialog4", rect.InflateBy(0, 0, 0, 50 + border[1]));

				Game.Renderer.Fonts["Bold"].DrawText("Power", pos, Color.White);
				pos += new int2(0, 20);
				Game.Renderer.Fonts["Regular"].DrawText("Provided: {0}\nDrained: {1}".F(power.PowerProvided, power.PowerDrained), pos, Color.White);
			}
		}
	}
}
