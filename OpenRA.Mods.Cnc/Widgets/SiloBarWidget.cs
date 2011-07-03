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
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets
{
	public class SiloBarWidget : Widget
	{
		public float LowStorageThreshold = 0.8f;
		float? lastCapacityFrac;
		float? lastStoredFrac;

		readonly PlayerResources pr;
		[ObjectCreator.UseCtor]
		public SiloBarWidget( [ObjectCreator.Param] World world )
		{
			pr = world.LocalPlayer.PlayerActor.Trait<PlayerResources>();
		}

		public override void Draw()
		{
			float scaleBy = 100;
			var max = Math.Max(pr.OreCapacity, pr.Ore);
			while (max >= scaleBy) scaleBy *= 2;

			// Current capacity
			var capacityFrac = pr.OreCapacity / scaleBy;
			lastCapacityFrac = capacityFrac = float2.Lerp(lastCapacityFrac.GetValueOrDefault(capacityFrac), capacityFrac, .3f);

			var color = Color.LimeGreen;
			if (pr.Ore >= LowStorageThreshold*pr.OreCapacity)
				color = Color.Orange;
			if (pr.Ore == pr.OreCapacity)
				color = Color.Red;
			
			var b = RenderBounds;
			var rect = new RectangleF(Game.viewport.Location.X + b.X,
			                          Game.viewport.Location.Y + b.Y + (1-capacityFrac)*b.Height,
			                          (float)b.Width,
			                          capacityFrac*b.Height);
			Game.Renderer.LineRenderer.FillRect(rect, color);
			
			var indicator = ChromeProvider.GetImage("sidebar-bits", "right-indicator");
			
			var storedFrac = pr.Ore / scaleBy;
			lastStoredFrac = storedFrac = float2.Lerp(lastStoredFrac.GetValueOrDefault(storedFrac), storedFrac, .3f);
			
			float2 pos = new float2(b.X, b.Y + (1-storedFrac)*b.Height - indicator.size.Y / 2);
			
			Game.Renderer.RgbaSpriteRenderer.DrawSprite(indicator, pos);
		}
	}
}
