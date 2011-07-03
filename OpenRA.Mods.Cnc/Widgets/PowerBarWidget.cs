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

namespace OpenRA.Mods.Cnc.Widgets
{
	public class PowerBarWidget : Widget
	{
		float? lastProvidedFrac;
		float? lastDrainedFrac;

		readonly PowerManager pm;
		[ObjectCreator.UseCtor]
		public PowerBarWidget( [ObjectCreator.Param] World world )
		{
			pm = world.LocalPlayer.PlayerActor.Trait<PowerManager>();
		}

		public override void Draw()
		{
			float powerScaleBy = 100;
			var maxPower = Math.Max(pm.PowerProvided, pm.PowerDrained);
			while (maxPower >= powerScaleBy) powerScaleBy *= 2;

			// Current power supply
			var providedFrac = pm.PowerProvided / powerScaleBy;
			lastProvidedFrac = providedFrac = float2.Lerp(lastProvidedFrac.GetValueOrDefault(providedFrac), providedFrac, .3f);

			var color = Color.LimeGreen;
			if (pm.PowerState == PowerState.Low)
				color = Color.Orange;
			if (pm.PowerState == PowerState.Critical)
				color = Color.Red;
			
			var b = RenderBounds;
			var rect = new RectangleF(Game.viewport.Location.X + b.X,
			                          Game.viewport.Location.Y + b.Y + (1-providedFrac)*b.Height,
			                          (float)b.Width,
			                          providedFrac*b.Height);
			Game.Renderer.LineRenderer.FillRect(rect, color);
			
			var indicator = ChromeProvider.GetImage("sidebar-bits", "left-indicator");
			
			var drainedFrac = pm.PowerDrained / powerScaleBy;
			lastDrainedFrac = drainedFrac = float2.Lerp(lastDrainedFrac.GetValueOrDefault(drainedFrac), drainedFrac, .3f);
			
			float2 pos = new float2(b.X + b.Width - indicator.size.X,
			                        b.Y + (1-drainedFrac)*b.Height - indicator.size.Y / 2);
			
			Game.Renderer.RgbaSpriteRenderer.DrawSprite(indicator, pos);
		}
	}
}
