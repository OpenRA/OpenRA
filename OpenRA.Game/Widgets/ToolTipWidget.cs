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
		public int TooltipDelay = 20;

		public ToolTipWidget()
			: base()
		{
			this.Color = Color.Yellow;
			GetText = () => Text;
			GetColor = () => Color;
			GetContrastColor = () => ContrastColor;
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
		}

		public override void Draw()
		{
			if (Ui.MouseOverWidget == null)
				return;

			if (Ui.MouseOverWidget != null && Ui.MouseOverWidget != this.Tag)
			return;

			if (Viewport.TicksSinceLastMove < TooltipDelay)
				return;

			this.IsRendered = true;
			base.Position = new int2(Viewport.LastMousePos.X, Viewport.LastMousePos.Y + 15);
			base.Draw();
		}

		public override Widget Clone() { return new ToolTipWidget(this); }
	}
}
