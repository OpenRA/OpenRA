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
	public interface IInfinityCashDisplay
	{
		Func<bool> ShowInfinitySymbol { get; set; }
	}

	static class IngameCashLabelDrawing
	{
		const string SidewaysEightText = "8";
		const float SidewaysEightAngle = -(float)Math.PI / 2;

		public static void DrawSidewaysEight(LabelWidget widget)
		{
			if (!Game.Renderer.Fonts.TryGetValue(widget.Font, out var font))
				throw new ArgumentException($"Requested font '{widget.Font}' was not found.");

			var textSize = font.Measure(SidewaysEightText);
			var position = widget.RenderOrigin;
			var topOffset = font.TopOffset;

			if (widget.VAlign == TextVAlign.Top)
				position += new int2(0, -topOffset);
			else if (widget.VAlign == TextVAlign.Middle)
				position += new int2(0, (widget.Bounds.Height - textSize.Y - topOffset) / 2);
			else if (widget.VAlign == TextVAlign.Bottom)
				position += new int2(0, widget.Bounds.Height - textSize.Y);

			// Rotated 90° clockwise: visual width and height swap.
			var visualWidth = textSize.Y;
			if (widget.Align == TextAlign.Center)
				position += new int2((widget.Bounds.Width - visualWidth) / 2, 0);
			else if (widget.Align == TextAlign.Right)
				position += new int2(widget.Bounds.Width - visualWidth, 0);

			position += new int2(4, 5);

			var drawLocation = new float2(position.X, position.Y);
			var color = widget.GetColor();
			var bgDark = widget.GetContrastColorDark();
			var bgLight = widget.GetContrastColorLight();

			if (widget.Contrast)
				font.DrawTextWithShadow(SidewaysEightText, drawLocation, color, bgDark, bgLight, widget.ContrastRadius, SidewaysEightAngle);
			else if (widget.Shadow)
				font.DrawTextWithShadow(SidewaysEightText, drawLocation, color, bgDark, bgLight, 1, SidewaysEightAngle);
			else
				font.DrawText(SidewaysEightText, drawLocation, color, SidewaysEightAngle);
		}
	}

	public class IngameCashLabelWidget : LabelWithTooltipWidget, IInfinityCashDisplay
	{
		public Func<bool> ShowInfinitySymbol { get; set; }

		[ObjectCreator.UseCtor]
		public IngameCashLabelWidget(ModData modData)
			: base(modData) { }

		protected IngameCashLabelWidget(IngameCashLabelWidget other)
			: base(other)
		{
			ShowInfinitySymbol = other.ShowInfinitySymbol;
		}

		public override IngameCashLabelWidget Clone() { return new IngameCashLabelWidget(this); }

		public override void Draw()
		{
			if (ShowInfinitySymbol != null && ShowInfinitySymbol())
				IngameCashLabelDrawing.DrawSidewaysEight(this);
			else
				base.Draw();
		}
	}

	public class WorldIngameCashLabelWidget : WorldLabelWithTooltipWidget, IInfinityCashDisplay
	{
		public Func<bool> ShowInfinitySymbol { get; set; }

		[ObjectCreator.UseCtor]
		public WorldIngameCashLabelWidget(ModData modData, World world)
			: base(modData, world) { }

		protected WorldIngameCashLabelWidget(WorldIngameCashLabelWidget other)
			: base(other)
		{
			ShowInfinitySymbol = other.ShowInfinitySymbol;
		}

		public override WorldIngameCashLabelWidget Clone() { return new WorldIngameCashLabelWidget(this); }

		public override void Draw()
		{
			if (ShowInfinitySymbol != null && ShowInfinitySymbol())
				IngameCashLabelDrawing.DrawSidewaysEight(this);
			else
				base.Draw();
		}
	}
}
