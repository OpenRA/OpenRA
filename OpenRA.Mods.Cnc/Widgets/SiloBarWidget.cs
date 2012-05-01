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
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets
{
	public class SiloBarWidget : Widget
	{
		public readonly string TooltipTemplate = "SIMPLE_TOOLTIP";
		public readonly string TooltipContainer;
		Lazy<TooltipContainerWidget> tooltipContainer;

		public float LowStorageThreshold = 0.8f;
		float? lastCapacityFrac;
		float? lastStoredFrac;

		readonly PlayerResources pr;

		[ObjectCreator.UseCtor]
		public SiloBarWidget(World world)
		{
			pr = world.LocalPlayer.PlayerActor.Trait<PlayerResources>();
			tooltipContainer = Lazy.New(() =>
				Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));
		}

		public override void MouseEntered()
		{
			if (TooltipContainer == null) return;
			Func<string> getText = () => "Silo Usage: {0}/{1}".F(pr.Ore, pr.OreCapacity);
			tooltipContainer.Value.SetTooltip(TooltipTemplate, new WidgetArgs() {{ "getText", getText }});
		}

		public override void MouseExited()
		{
			if (TooltipContainer == null) return;
			tooltipContainer.Value.RemoveTooltip();
		}

		public override void Draw()
		{
			float scaleBy = 100;
			var max = Math.Max(pr.OreCapacity, pr.Ore);
			while (max >= scaleBy) scaleBy *= 2;

			// Current capacity
			var capacityFrac = pr.OreCapacity / scaleBy;
			lastCapacityFrac = capacityFrac = float2.Lerp(lastCapacityFrac.GetValueOrDefault(capacityFrac), capacityFrac, .3f);

			var color = GetBarColor();

			var b = RenderBounds;
			var rect = new RectangleF(b.X, float2.Lerp( b.Bottom, b.Top, capacityFrac ),
				(float)b.Width, capacityFrac*b.Height);
			Game.Renderer.LineRenderer.FillRect(rect, color);

			var indicator = ChromeProvider.GetImage("sidebar-bits", "right-indicator");

			var storedFrac = pr.Ore / scaleBy;
			lastStoredFrac = storedFrac = float2.Lerp(lastStoredFrac.GetValueOrDefault(storedFrac), storedFrac, .3f);

			float2 pos = new float2(b.X, float2.Lerp( b.Bottom, b.Top, storedFrac ) - indicator.size.Y / 2);

			Game.Renderer.RgbaSpriteRenderer.DrawSprite(indicator, pos);
		}

		Color GetBarColor()
		{
			if (pr.Ore == pr.OreCapacity) return Color.Red;
			if (pr.Ore >= LowStorageThreshold * pr.OreCapacity) return Color.Orange;
			return Color.LimeGreen;
		}
	}
}
