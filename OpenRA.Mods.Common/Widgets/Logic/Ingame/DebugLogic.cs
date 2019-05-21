﻿#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic.Ingame
{
	public class DebugLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public DebugLogic(Widget widget, OrderManager orderManager, World world, WorldRenderer worldRenderer)
		{
			var geometryOverlay = world.WorldActor.TraitOrDefault<TerrainGeometryOverlay>();
			if (geometryOverlay != null)
			{
				var labelWidget = widget.Get<LabelWidget>("DEBUG_TEXT");

				var cellPosText = new CachedTransform<int2, string>(t =>
				{
					var cell = worldRenderer.Viewport.ViewToWorld(Viewport.LastMousePos);
					var map = worldRenderer.World.Map;
					var wpos = map.CenterOfCell(cell);
					return map.Height.Contains(cell) ? "({0},{1}) ({2})".F(cell, map.Height[cell], wpos) : "";
				});

				labelWidget.GetText = () => cellPosText.Update(Viewport.LastMousePos);

				labelWidget.IsVisible = () => geometryOverlay.Enabled;
			}
		}
	}
}
