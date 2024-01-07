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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class MultiplayerLogic : ServerListLogic
	{
		static readonly Action DoNothing = () => { };

		public class MultiplayerLogicDynamicWidgets : ServerListLogicDynamicWidgets
		{
			public override ISet<string> WindowWidgetIds { get; }

			public MultiplayerLogicDynamicWidgets()
			{
				WindowWidgetIds = new HashSet<string>(base.WindowWidgetIds)
				{
					"CONNECTING_PANEL",
					"DIRECTCONNECT_PANEL",
					"MULTIPLAYER_CREATESERVER_PANEL",
					"MULTIPLAYER_PANEL",
					"SERVER_LOBBY",
				};
			}
		}

		readonly MultiplayerLogicDynamicWidgets dynamicWidgets = new();

		[ObjectCreator.UseCtor]
		public MultiplayerLogic(Widget widget, ModData modData, Action onStart, Action onExit, ConnectionTarget directConnectEndPoint)
			: base(widget, modData, server => Join(server, onStart, onExit))
		{
			var directConnectButton = widget.Get<ButtonWidget>("DIRECTCONNECT_BUTTON");
			directConnectButton.OnClick = () =>
			{
				dynamicWidgets.OpenWindow("DIRECTCONNECT_PANEL", new WidgetArgs
				{
					{ "openLobby", () => OpenLobby(onStart, onExit) },
					{ "onExit", DoNothing },
					{ "directConnectEndPoint", null },
				});
			};

			var createServerButton = widget.Get<ButtonWidget>("CREATE_BUTTON");
			createServerButton.OnClick = () =>
			{
				dynamicWidgets.OpenWindow("MULTIPLAYER_CREATESERVER_PANEL", new WidgetArgs
				{
					{ "openLobby", () => OpenLobby(onStart, onExit) },
					{ "onExit", DoNothing }
				});
			};

			var hasMaps = modData.MapCache.Any(p => !p.Visibility.HasFlag(MapVisibility.Shellmap));
			createServerButton.Disabled = !hasMaps;

			widget.Get<ButtonWidget>("BACK_BUTTON").OnClick = () => { Ui.CloseWindow(); onExit(); };

			if (directConnectEndPoint != null)
			{
				// The connection window must be opened at the end of the tick for the widget hierarchy to
				// work out, but we also want to prevent the server browser from flashing visible for one tick.
				widget.Visible = false;
				Game.RunAfterTick(() =>
				{
					dynamicWidgets.OpenWindow("DIRECTCONNECT_PANEL", new WidgetArgs
					{
						{ "openLobby", () => OpenLobby(onStart, onExit) },
						{ "onExit", DoNothing },
						{ "directConnectEndPoint", directConnectEndPoint },
					});

					widget.Visible = true;
				});
			}
		}

		static void OpenLobby(Action onStart, Action onExit)
		{
			// Close the multiplayer browser
			Ui.CloseWindow();

			void OnLobbyExit()
			{
				// Open a fresh copy of the multiplayer browser
				new MultiplayerLogicDynamicWidgets().OpenWindow("MULTIPLAYER_PANEL", new WidgetArgs
				{
					{ "onStart", onStart },
					{ "onExit", onExit },
					{ "directConnectEndPoint", null },
				});

				Game.Disconnect();

				DiscordService.UpdateStatus(DiscordState.InMenu);
			}

			new MultiplayerLogicDynamicWidgets().OpenWindow("SERVER_LOBBY", new WidgetArgs
			{
				{ "onStart", onStart },
				{ "onExit", OnLobbyExit },
				{ "skirmishMode", false }
			});
		}

		static void Join(GameServer server, Action onStart, Action onExit)
		{
			if (server == null || !server.IsJoinable)
				return;

			var host = server.Address.Split(':')[0];
			var port = Exts.ParseInt32Invariant(server.Address.Split(':')[1]);

			ConnectionLogic.Connect(
				new MultiplayerLogicDynamicWidgets(), new ConnectionTarget(host, port), "", () => OpenLobby(onStart, onExit), DoNothing);
		}
	}
}
