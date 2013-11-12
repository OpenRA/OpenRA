#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
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

namespace OpenRA.Widgets
{
	public class TimerWidget : Widget
	{
		public string Font = "Title";
		public Color Color = Color.White;
		public bool Contrast = false;
		public Color ContrastColor = Color.Black;
		public Func<Color> GetColor;
		public Func<Color> GetContrastColor;
		
		public TimerWidget()
			: base()
		{
			GetColor = () => Color;
			GetContrastColor = () => ContrastColor;
		}
		
		protected TimerWidget(TimerWidget other)
			: base(other)
		{
			Font = other.Font;
			Color = other.Color;
			Contrast = other.Contrast;
			ContrastColor = other.ContrastColor;
			GetColor = other.GetColor;
			GetContrastColor = other.GetContrastColor;
		}
		
		public override void Draw()
		{
			SpriteFont font = Game.Renderer.Fonts[Font];
			var rb = RenderBounds;
			var color = GetColor();
			var contrast = GetContrastColor();
			
			var s = WidgetUtils.FormatTime(Game.LocalTick) + (Game.orderManager.world.Paused?" (paused)":"");
			var pos = new float2(rb.Left - font.Measure(s).X / 2, rb.Top);
			if (Contrast)
				font.DrawTextWithContrast(s, pos, color, contrast, 1);
			else
				font.DrawText(s, pos, color);
		}
	}
}

