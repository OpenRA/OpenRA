#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Graphics;
using System;
using System.Drawing;
using System.Timers;

namespace OpenRA.Widgets
{


	public class ToolTipWidget : LabelWidget
	{
		public bool IsRendered = true;
		int2 lastMousePosition = Viewport.LastMousePos;

		public ToolTipWidget()
			: base()
		{
			this.Color = Color.Yellow;
			GetText = () => Text;
			GetColor = () => Color;
			GetContrastColor = () => ContrastColor;
			Timer t = new Timer(3000);
			t.Elapsed += (object s, ElapsedEventArgs arg) =>
			{
				lastMousePosition = Viewport.LastMousePos;
			};
			t.Start();
		}

		protected ToolTipWidget(ToolTipWidget other)
			: base(other)
		{
			Text = other.Text;
			Align = other.Align;
			Font = other.Font;
			Color = other.Color;
			Contrast = other.Contrast;
			ContrastColor = other.ContrastColor;
			WordWrap = other.WordWrap;
			GetText = other.GetText;
			GetColor = other.GetColor;
			GetContrastColor = other.GetContrastColor;
			Timer t = new Timer(3000);
			t.Elapsed += (object s, ElapsedEventArgs arg) =>
			{
				lastMousePosition = Viewport.LastMousePos;
			};
			t.Start();
		}




		public override void Draw()
		{
			if (Ui.MouseOverWidget == null)
				return;

			if (Viewport.LastMousePos.X != lastMousePosition.X || Viewport.LastMousePos.Y != lastMousePosition.Y)
				return;

			if (Ui.MouseOverWidget != null && Ui.MouseOverWidget.Get<LabelWidget>("TITLE") != null && this.Tag != null && Ui.MouseOverWidget.Get<LabelWidget>("TITLE").Tag.ToString() != this.Tag.ToString())
				return;

			this.IsRendered = true;
			SpriteFont font = Game.Renderer.Fonts[Font];
			var text = GetText();
			if (text == null)
				return;

			int2 textSize = font.Measure(text);
			int2 position = new int2(Viewport.LastMousePos.X - 5, Viewport.LastMousePos.Y + 15);

			if (VAlign == TextVAlign.Middle)
				position += new int2(0, (Bounds.Height - textSize.Y) / 2);

			if (VAlign == TextVAlign.Bottom)
				position += new int2(0, Bounds.Height - textSize.Y);

			if (Align == TextAlign.Center)
				position += new int2((Bounds.Width - textSize.X) / 2, 0);

			if (Align == TextAlign.Right)
				position += new int2(Bounds.Width - textSize.X, 0);

			if (WordWrap)
				text = WidgetUtils.WrapText(text, Bounds.Width, font);

			var color = GetColor();
			var contrast = GetContrastColor();
			if (Contrast)
				font.DrawTextWithContrast(text, position, color, contrast, 2);
			else
				font.DrawText(text, position, color);


		}

		public override Widget Clone() { return new ToolTipWidget(this); }
	}
}
