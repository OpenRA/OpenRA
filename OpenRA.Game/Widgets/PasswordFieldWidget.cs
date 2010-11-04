#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Drawing;
using OpenRA.Graphics;

namespace OpenRA.Widgets
{
	public class PasswordFieldWidget : TextFieldWidget
	{
		public PasswordFieldWidget()
			: base()
		{
		}

		protected PasswordFieldWidget(PasswordFieldWidget widget)
			: base(widget)
		{

		}

		public override void DrawInner( WorldRenderer wr )
		{
			int margin = 5;
			var font = (Bold) ? Game.Renderer.BoldFont : Game.Renderer.RegularFont;
			var cursor = (showCursor && Focused) ? "|" : "";
			var textSize = font.Measure(new string('*', Text.Length) + "|");
			var pos = RenderOrigin;

			WidgetUtils.DrawPanel("dialog3",
				new Rectangle(pos.X, pos.Y, Bounds.Width, Bounds.Height));

			// Inset text by the margin and center vertically
			var textPos = pos + new int2(margin, (Bounds.Height - textSize.Y) / 2 - VisualHeight);

			// Right align when editing and scissor when the text overflows
			if (textSize.X > Bounds.Width - 2 * margin)
			{
				if (Focused)
					textPos += new int2(Bounds.Width - 2 * margin - textSize.X, 0);

				Game.Renderer.EnableScissor(pos.X + margin, pos.Y, Bounds.Width - 2 * margin, Bounds.Bottom);
			}

			font.DrawText(new string('*', Text.Length) + cursor, textPos, Color.White);

			if (textSize.X > Bounds.Width - 2 * margin)
				Game.Renderer.DisableScissor();
		}

		public override Widget Clone() { return new PasswordFieldWidget(this); }
	}
}