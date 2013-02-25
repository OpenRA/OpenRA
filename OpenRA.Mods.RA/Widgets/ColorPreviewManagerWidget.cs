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
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets
{
	public class ColorPreviewManagerWidget : Widget
	{
		public readonly string Palette = "colorpicker";
		public readonly int[] RemapIndices = {};
		public ColorRamp Ramp;

		ColorRamp cachedRamp;
		WorldRenderer worldRenderer;
		Palette preview;

		[ObjectCreator.UseCtor]
		public ColorPreviewManagerWidget(WorldRenderer worldRenderer)
			: base()
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
			if (cachedRamp == Ramp)
				return;

			preview.ApplyRemap(new PlayerColorRemap(RemapIndices, Ramp));
			cachedRamp = Ramp;
		}
	}
}

