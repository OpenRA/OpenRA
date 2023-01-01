#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Graphics;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	public class LoadWidgetAtGameStartInfo : TraitInfo
	{
		[Desc("The widget tree to open when a shellmap is loaded (i.e. the main menu).")]
		public readonly string ShellmapRoot = "MAINMENU";

		[Desc("The widget tree to open when a regular map is loaded (i.e. the ingame UI).")]
		public readonly string IngameRoot = "INGAME_ROOT";

		[Desc("The widget tree to open when the map editor is loaded.")]
		public readonly string EditorRoot = "EDITOR_ROOT";

		[Desc("The widget tree to open (in addition to INGAME_ROOT) while loading a saved game.")]
		public readonly string GameSaveLoadingRoot = "GAMESAVE_LOADING_SCREEN";

		[Desc("Remove any existing UI when a map is loaded.")]
		public readonly bool ClearRoot = true;

		public override object Create(ActorInitializer init) { return new LoadWidgetAtGameStart(this); }
	}

	public class LoadWidgetAtGameStart : IWorldLoaded, INotifyGameLoading, INotifyGameLoaded
	{
		readonly LoadWidgetAtGameStartInfo info;
		Widget root;

		public LoadWidgetAtGameStart(LoadWidgetAtGameStartInfo info)
		{
			this.info = info;
		}

		void INotifyGameLoading.GameLoading(World world)
		{
			// Clear any existing widget state
			if (info.ClearRoot)
				Ui.ResetAll();

			Ui.OpenWindow(info.GameSaveLoadingRoot, new WidgetArgs()
			{
				{ "world", world }
			});
		}

		void IWorldLoaded.WorldLoaded(World world, WorldRenderer wr)
		{
			if (!world.IsLoadingGameSave && info.ClearRoot)
				Ui.ResetAll();

			var widget = world.Type == WorldType.Shellmap ? info.ShellmapRoot :
				world.Type == WorldType.Editor ? info.EditorRoot : info.IngameRoot;

			root = Game.LoadWidget(world, widget, Ui.Root, new WidgetArgs());

			// The Lua API requires the UI to available, so hide it instead
			if (world.IsLoadingGameSave)
				root.IsVisible = () => false;
		}

		void INotifyGameLoaded.GameLoaded(World world)
		{
			Ui.CloseWindow();
			root.IsVisible = () => true;

			// Open the options menu
			if (!world.IsReplay)
			{
				var optionsButton = root.GetOrNull<MenuButtonWidget>("OPTIONS_BUTTON");
				world.SetPauseState(false);
				if (optionsButton != null)
					Sync.RunUnsynced(world, optionsButton.OnClick);
			}
		}
	}
}
