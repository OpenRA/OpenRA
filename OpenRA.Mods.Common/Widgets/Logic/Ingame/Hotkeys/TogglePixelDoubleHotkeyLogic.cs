#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Lint;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic.Ingame
{
	[ChromeLogicArgsHotkeys("TogglePixelDoubleKey")]
	public class TogglePixelDoubleHotkeyLogic : SingleHotkeyBaseLogic
	{
		readonly Viewport viewport;

		[ObjectCreator.UseCtor]
		public TogglePixelDoubleHotkeyLogic(Widget widget, ModData modData, WorldRenderer worldRenderer, Dictionary<string, MiniYaml> logicArgs)
			: base(widget, modData, "TogglePixelDoubleKey", "WORLD_KEYHANDLER", logicArgs)
		{
			viewport = worldRenderer.Viewport;
		}

		protected override bool OnHotkeyActivated(KeyInput e)
		{
			// Zoom is currently always set directly, so we don't need to worry about floating point imprecision
			if (viewport.Zoom == 1f)
				viewport.Zoom = 2f;
			else
			{
				// Reset zoom to regular view if it was anything else before
				// (like a zoom level only reachable by using the scroll wheel).
				viewport.Zoom = 1f;
			}

			Game.Settings.Graphics.PixelDouble = viewport.Zoom == 2f;

			return true;
		}
	}
}
