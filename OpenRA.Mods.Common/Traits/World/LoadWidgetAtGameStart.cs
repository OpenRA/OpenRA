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

using OpenRA.Graphics;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Traits
{
	public class LoadWidgetAtGameStartInfo : ITraitInfo
	{
		[Desc("The widget tree to open when a shellmap is loaded (i.e. the main menu).")]
		public readonly string ShellmapRoot = "MAINMENU";

		[Desc("The widget tree to open when a regular map is loaded (i.e. the ingame UI).")]
		public readonly string IngameRoot = "INGAME_ROOT";

		[Desc("The widget tree to open when the map editor is loaded.")]
		public readonly string EditorRoot = "EDITOR_ROOT";

		[Desc("Remove any existing UI when a map is loaded.")]
		public readonly bool ClearRoot = true;

		public object Create(ActorInitializer init) { return new LoadWidgetAtGameStart(this); }
	}

	public class LoadWidgetAtGameStart : IWorldLoaded
	{
		readonly LoadWidgetAtGameStartInfo info;

		public LoadWidgetAtGameStart(LoadWidgetAtGameStartInfo info)
		{
			this.info = info;
		}

		public void WorldLoaded(World world, WorldRenderer wr)
		{
			// Clear any existing widget state
			if (info.ClearRoot)
				Ui.ResetAll();

			var widget = world.Type == WorldType.Shellmap ? info.ShellmapRoot :
				world.Type == WorldType.Editor ? info.EditorRoot : info.IngameRoot;

			Game.LoadWidget(world, widget, Ui.Root, new WidgetArgs());
		}
	}
}