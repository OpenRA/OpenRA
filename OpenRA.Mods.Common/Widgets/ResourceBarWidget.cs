#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public enum ResourceBarOrientation { Vertical, Horizontal }
	public enum ResourceBarStyle { Flat, Bevelled }
	public class ResourceBarWidget : Widget
	{
		public readonly string TooltipTemplate;
		public readonly string TooltipContainer;
		Lazy<TooltipContainerWidget> tooltipContainer;

		public string TooltipFormat = "";
		public ResourceBarOrientation Orientation = ResourceBarOrientation.Vertical;
		public ResourceBarStyle Style = ResourceBarStyle.Flat;
		public string IndicatorCollection = "sidebar-bits";
		public string IndicatorImage = "indicator";

		public Func<float> GetProvided = () => 0;
		public Func<float> GetUsed = () => 0;
		public Func<Color> GetBarColor = () => Color.White;
		EWMA providedLerp = new EWMA(0.3f);
		EWMA usedLerp = new EWMA(0.3f);

		[ObjectCreator.UseCtor]
		public ResourceBarWidget(World world)
		{
			tooltipContainer = Exts.Lazy(() =>
				Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));
		}

		public override void MouseEntered()
		{
			if (TooltipContainer == null)
				return;

			Func<string> getText = () => TooltipFormat.F(GetUsed(), GetProvided());
			tooltipContainer.Value.SetTooltip(TooltipTemplate, new WidgetArgs() { { "getText", getText } });
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
			var indicator = ChromeProvider.GetImage(IndicatorCollection, IndicatorImage);

			var color = GetBarColor();
			if (Orientation == ResourceBarOrientation.Vertical)
			{
				if (Style == ResourceBarStyle.Bevelled)
				{
					var colorDark = Exts.ColorLerp(0.25f, color, Color.Black);
					for (var i = 0; i < b.Height; i++)
					{
						color = (i - 1 < b.Height / 2) ? color : colorDark;
						var bottom = new float2(b.Left + i, b.Bottom);
						var top = new float2(b.Left + i, b.Bottom + providedFrac * b.Height);

						// Indent corners
						if ((i == 0 || i == b.Width - 1) && providedFrac * b.Height > 1)
						{
							bottom.Y += 1;
							top.Y -= 1;
						}

						Game.Renderer.RgbaColorRenderer.DrawLine(bottom, top, 1, color);
					}
				}
				else
				{
					var tl = new float2(b.X, (int)float2.Lerp(b.Bottom, b.Top, providedFrac));
					var br = tl + new float2(b.Width, (int)(providedFrac * b.Height));
					Game.Renderer.RgbaColorRenderer.FillRect(tl, br, color);
				}

				var x = (b.Left + b.Right - indicator.Size.X) / 2;
				var y = float2.Lerp(b.Bottom, b.Top, usedFrac) - indicator.Size.Y / 2;
				Game.Renderer.RgbaSpriteRenderer.DrawSprite(indicator, new float2(x, y));
			}
			else
			{
				if (Style == ResourceBarStyle.Bevelled)
				{
					var colorDark = Exts.ColorLerp(0.25f, color, Color.Black);
					for (var i = 0; i < b.Height; i++)
					{
						color = (i - 1 < b.Height / 2) ? color : colorDark;
						var left = new float2(b.Left, b.Top + i);
						var right = new float2(b.Left + providedFrac * b.Width, b.Top + i);

						// Indent corners
						if ((i == 0 || i == b.Height - 1) && providedFrac * b.Width > 1)
						{
							left.X += 1;
							right.X -= 1;
						}

						Game.Renderer.RgbaColorRenderer.DrawLine(left, right, 1, color);
					}
				}
				else
				{
					var tl = new float2(b.X, b.Y);
					var br = tl + new float2((int)(providedFrac * b.Width), b.Height);
					Game.Renderer.RgbaColorRenderer.FillRect(tl, br, color);
				}

				var x = float2.Lerp(b.Left, b.Right, usedFrac) - indicator.Size.X / 2;
				var y = (b.Bottom + b.Top - indicator.Size.Y) / 2;
				Game.Renderer.RgbaSpriteRenderer.DrawSprite(indicator, new float2(x, y));
			}
		}
	}
}
