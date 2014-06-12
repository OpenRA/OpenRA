#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */

#endregion

using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class ColorPreviewManagerWidget : Widget
	{
		public readonly string Palette = "colorpicker";
		public readonly int[] RemapIndices = ChromeMetrics.Get<int[]>("ColorPickerRemapIndices");
		public readonly float Ramp = 0.05f;
		public HSLColor Color;

		HSLColor cachedColor;
		WorldRenderer worldRenderer;
		Palette preview;

		[ObjectCreator.UseCtor]
		public ColorPreviewManagerWidget(WorldRenderer worldRenderer)
		{
			this.worldRenderer = worldRenderer;
		}

		public override void Initialize(WidgetArgs args)
		{
			base.Initialize(args);
			preview = worldRenderer.Palette(Palette).Palette;
		}

		public override void Tick()
		{
			if (cachedColor == Color)
				return;

			preview.ApplyRemap(new PlayerColorRemap(RemapIndices, Color, Ramp));
			cachedColor = Color;
		}
	}
}

