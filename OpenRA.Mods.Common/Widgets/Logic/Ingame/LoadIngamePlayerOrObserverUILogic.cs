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

using System.Collections.Generic;
using OpenRA.Mods.Common.Scripting;
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class LoadIngamePlayerOrObserverUILogic : ChromeLogic
	{
		public class LoadIngamePlayerOrObserverUILogicDynamicWidgets : DynamicWidgets
		{
			public override ISet<string> WindowWidgetIds { get; } =
				new HashSet<string>
				{
					"FMVPLAYER",
				};
			public override IReadOnlyDictionary<string, string> ParentWidgetIdForChildWidgetId { get; } =
				new Dictionary<string, string>
				{
					{ "OBSERVER_WIDGETS", "PLAYER_ROOT" },
					{ "PLAYER_WIDGETS", "PLAYER_ROOT" },
					{ "DEBUG_WIDGETS", "WORLD_ROOT" },
					{ "TRANSIENTS_PANEL", "WORLD_ROOT" },
				};
		}

		readonly LoadIngamePlayerOrObserverUILogicDynamicWidgets dynamicWidgets = new();

		bool loadingObserverWidgets = false;

		[ObjectCreator.UseCtor]
		public LoadIngamePlayerOrObserverUILogic(Widget widget, World world)
		{
			var ingameRoot = widget.Get("INGAME_ROOT");
			var worldRoot = ingameRoot.Get("WORLD_ROOT");
			var menuRoot = ingameRoot.Get("MENU_ROOT");
			var playerRoot = worldRoot.Get("PLAYER_ROOT");

			if (world.LocalPlayer == null)
				dynamicWidgets.LoadWidget(worldRoot, "OBSERVER_WIDGETS", new WidgetArgs());
			else
			{
				var playerWidgets = dynamicWidgets.LoadWidget(worldRoot, "PLAYER_WIDGETS", new WidgetArgs());
				var sidebarTicker = playerWidgets.Get<LogicTickerWidget>("SIDEBAR_TICKER");
				var objectives = world.LocalPlayer.PlayerActor.Info.TraitInfoOrDefault<MissionObjectivesInfo>();

				sidebarTicker.OnTick = () =>
				{
					// Switch to observer mode after win/loss
					if (world.LocalPlayer.WinState != WinState.Undefined && !loadingObserverWidgets)
					{
						loadingObserverWidgets = true;
						Game.RunAfterDelay(objectives?.GameOverDelay ?? 0, () =>
						{
							if (!Game.IsCurrentWorld(world))
								return;

							playerRoot.RemoveChildren();
							dynamicWidgets.LoadWidget(worldRoot, "PLAYER_WIDGETS", new WidgetArgs());
						});
					}
				};
			}

			dynamicWidgets.LoadWidget(ingameRoot, "DEBUG_WIDGETS", new WidgetArgs());
			dynamicWidgets.LoadWidget(ingameRoot, "TRANSIENTS_PANEL", new WidgetArgs());

			world.GameOver += () =>
			{
				Ui.CloseWindow();
				menuRoot.RemoveChildren();

				if (world.LocalPlayer != null)
				{
					var scriptContext = world.WorldActor.TraitOrDefault<LuaScript>();
					var missionData = world.WorldActor.Info.TraitInfoOrDefault<MissionDataInfo>();
					if (missionData != null && !(scriptContext != null && scriptContext.FatalErrorOccurred))
					{
						var video = world.LocalPlayer.WinState == WinState.Won ? missionData.WinVideo : missionData.LossVideo;
						if (!string.IsNullOrEmpty(video))
							Media.PlayFMVFullscreen(dynamicWidgets, world, video, () => { });
					}
				}

				var optionsButton = playerRoot.GetOrNull<MenuButtonWidget>("OPTIONS_BUTTON");
				if (optionsButton != null)
					Sync.RunUnsynced(world, optionsButton.OnClick);
			};
		}
	}
}
