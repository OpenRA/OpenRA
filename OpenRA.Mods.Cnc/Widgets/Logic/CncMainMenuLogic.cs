#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Widgets;
using OpenRA.Mods.Common.Widgets.Logic;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets.Logic
{
	public class CncMainMenuLogic : MainMenuLogic
	{
		[ObjectCreator.UseCtor]
		public CncMainMenuLogic(Widget widget, World world, ModData modData)
			: base(widget, world, modData)
		{
			var shellmapDecorations = widget.Get("SHELLMAP_DECORATIONS");
			shellmapDecorations.IsVisible = () => menuType != MenuType.None && Game.Settings.Game.ShowShellmap;
			shellmapDecorations.Get<ImageWidget>("RECBLOCK").IsVisible = () => world.WorldTick / 25 % 2 == 0;

			var shellmapDisabledDecorations = widget.Get("SHELLMAP_DISABLED_DECORATIONS");
			shellmapDisabledDecorations.IsVisible = () => !Game.Settings.Game.ShowShellmap;
		}
	}
}
