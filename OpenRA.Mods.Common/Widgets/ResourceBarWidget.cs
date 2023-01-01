#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public enum ResourceBarOrientation { Vertical, Horizontal }
	public class ResourceBarWidget : Widget
	{
		public readonly string TooltipTemplate;
		public readonly string TooltipContainer;
		readonly Lazy<TooltipContainerWidget> tooltipContainer;

		public CachedTransform<(float, float), string> TooltipTextCached;
		public ResourceBarOrientation Orientation = ResourceBarOrientation.Vertical;
		public string IndicatorCollection = "sidebar-bits";
		public string IndicatorImage = "indicator";

		public Func<float> GetProvided = () => 0;
		public Func<float> GetUsed = () => 0;
		public Func<Color> GetBarColor = () => Color.White;
		readonly EWMA providedLerp = new EWMA(0.3f);
		readonly EWMA usedLerp = new EWMA(0.3f);
		readonly World world;
		Sprite indicator;

		[ObjectCreator.UseCtor]
		public ResourceBarWidget(World world)
		{
			this.world = world;
			tooltipContainer = Exts.Lazy(() =>
				Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));
		}

		public override void Initialize(WidgetArgs args)
		{
			base.Initialize(args);

			indicator = ChromeProvider.GetImage(IndicatorCollection, IndicatorImage);
		}

		public override void MouseEntered()
		{
			if (TooltipContainer == null)
				return;

			Func<string> getText = () => TooltipTextCached.Update((GetUsed(), GetProvided()));
			tooltipContainer.Value.SetTooltip(TooltipTemplate, new WidgetArgs() { { "getText", getText }, { "world", world } });
		}

		public override void MouseExited()
		{
			if (TooltipContainer == null)
				return;
			tooltipContainer.Value.RemoveTooltip();
		}

		public override void Draw()
		{
			var scaleBy = 100.0f;
			var provided = GetProvided();
			var used = GetUsed();
			var max = Math.Max(provided, used);
			while (max >= scaleBy)
				scaleBy *= 2;

			var providedFrac = providedLerp.Update(provided / scaleBy);
			var usedFrac = usedLerp.Update(used / scaleBy);

			var b = RenderBounds;

			var color = GetBarColor();
			if (Orientation == ResourceBarOrientation.Vertical)
			{
				var tl = new float2(b.X, (int)float2.Lerp(b.Bottom, b.Top, providedFrac));
				var br = tl + new float2(b.Width, (int)(providedFrac * b.Height));
				Game.Renderer.RgbaColorRenderer.FillRect(tl, br, color);

				var x = (b.Left + b.Right - indicator.Size.X) / 2;
				var y = float2.Lerp(b.Bottom, b.Top, usedFrac) - indicator.Size.Y / 2;
				WidgetUtils.DrawSprite(indicator, new float2(x, y));
			}
			else
			{
				var tl = new float2(b.X, b.Y);
				var br = tl + new float2((int)(providedFrac * b.Width), b.Height);
				Game.Renderer.RgbaColorRenderer.FillRect(tl, br, color);

				var x = float2.Lerp(b.Left, b.Right, usedFrac) - indicator.Size.X / 2;
				var y = (b.Bottom + b.Top - indicator.Size.Y) / 2;
				WidgetUtils.DrawSprite(indicator, new float2(x, y));
			}
		}
	}
}
