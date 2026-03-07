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
using System.Linq;
using OpenRA.Mods.Common.Lint;
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic.Ingame
{
	[ChromeLogicArgsHotkeys("IngameGameSpeedDecreaseKey", "IngameGameSpeedIncreaseKey")]
	public class IngameGameSpeedHotkeyLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public IngameGameSpeedHotkeyLogic(Widget widget, ModData modData, World world, OrderManager orderManager, Dictionary<string, MiniYaml> logicArgs)
		{
			var decreaseKey = new HotkeyReference();
			if (logicArgs.TryGetValue("IngameGameSpeedDecreaseKey", out var yaml))
				decreaseKey = modData.Hotkeys[yaml.Value];

			var increaseKey = new HotkeyReference();
			if (logicArgs.TryGetValue("IngameGameSpeedIncreaseKey", out yaml))
				increaseKey = modData.Hotkeys[yaml.Value];

			var keyhandler = widget.Get<LogicKeyListenerWidget>("WORLD_KEYHANDLER");
			keyhandler.AddHandler(e =>
			{
				if (e.Event == KeyInputEvent.Down &&
					(decreaseKey.IsActivatedBy(e) || increaseKey.IsActivatedBy(e)) &&
					!world.IsReplay &&
					orderManager.LobbyInfo.NonBotClients.Count() == 1 &&
					orderManager.LobbyInfo.NonBotClients.First().IsAdmin)
				{
					var gameSpeeds = Game.ModData.GetOrCreate<GameSpeeds>();
					var speedKeys = gameSpeeds.Speeds.Keys.ToList();
					var currentSpeed = orderManager.LobbyInfo.GlobalSettings.OptionOrDefault("gamespeed", gameSpeeds.DefaultSpeed);
					var currentIndex = speedKeys.IndexOf(currentSpeed);
					if (currentIndex >= 0)
					{
						var newIndex = decreaseKey.IsActivatedBy(e) ? currentIndex - 1 : currentIndex + 1;
						if (newIndex >= 0 && newIndex < speedKeys.Count)
							orderManager.IssueOrder(Order.Command($"ingame_gamespeed {speedKeys[newIndex]}"));
					}
				}

				return false;
			});
		}
	}
}
