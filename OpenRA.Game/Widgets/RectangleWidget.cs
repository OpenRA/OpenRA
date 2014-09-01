#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Drawing;

namespace OpenRA.Widgets
{
	public class RectangleWidget : Widget
	{
		public Func<Color> GetColor;
		public Func<int> Thickness;

		public RectangleWidget()
		{
			GetColor = () => Color.White;
			Thickness = () => 1;
		}

		protected RectangleWidget(RectangleWidget widget)
			: base(widget)
		{
			GetColor = widget.GetColor;
		}

		public override Widget Clone()
		{
			return new RectangleWidget(this);
		}

		public override void Draw()
		{
			for (var i = 0; i < Thickness(); i++)
			{
				Game.Renderer.LineRenderer.DrawRect(
					new float2(RenderBounds.X - i, RenderBounds.Y - i),
					new float2(RenderBounds.Right + i, RenderBounds.Bottom + i),
					GetColor());
			}
		}
	}
}
