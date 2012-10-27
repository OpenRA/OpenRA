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
	public class ResourceBarWidget : Widget
	{
		public readonly string TooltipTemplate = "SIMPLE_TOOLTIP";
		public readonly string TooltipContainer;
		Lazy<TooltipContainerWidget> tooltipContainer;

		public float LowStorageThreshold = 0.8f;
		EWMA providedLerp = new EWMA(0.3f);
		EWMA usedLerp = new EWMA(0.3f);

		public Func<float> GetProvided = () => 0;
		public Func<float> GetUsed = () => 0;
		public string TooltipFormat = "";
		public bool RightIndicator = false;
		public Func<Color> GetBarColor = () => Color.White;

		[ObjectCreator.UseCtor]
		public ResourceBarWidget(World world)
		{
			tooltipContainer = Lazy.New(() =>
				Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));
		}

		public override void MouseEntered()
		{
			if (TooltipContainer == null) return;
			Func<string> getText = () => TooltipFormat.F(GetUsed(), GetProvided());
			tooltipContainer.Value.SetTooltip(TooltipTemplate, new WidgetArgs() {{ "getText", getText }});
		}

		public override void MouseExited()
		{
			if (TooltipContainer == null) return;
			tooltipContainer.Value.RemoveTooltip();
		}

		public override void Draw()
		{
			var scaleBy = 100.0f;
			var provided = GetProvided();
			var used = GetUsed();
			var max = Math.Max(provided, used);
			while (max >= scaleBy) scaleBy *= 2;

			var providedFrac = providedLerp.Update(provided/scaleBy);
			var usedFrac = usedLerp.Update(used/scaleBy);

			var color = GetBarColor();

			var b = RenderBounds;
			var rect = new RectangleF(b.X, float2.Lerp( b.Bottom, b.Top, providedFrac ),
				b.Width, providedFrac*b.Height);
			Game.Renderer.LineRenderer.FillRect(rect, color);

			var indicator = ChromeProvider.GetImage("sidebar-bits",
				RightIndicator ? "right-indicator" : "left-indicator");

			var indicatorX = RightIndicator ? (b.Right - indicator.size.X) : b.Left;

			var pos = new float2(indicatorX, float2.Lerp( b.Bottom, b.Top, usedFrac ) - indicator.size.Y / 2);

			Game.Renderer.RgbaSpriteRenderer.DrawSprite(indicator, pos);
		}
	}
}
