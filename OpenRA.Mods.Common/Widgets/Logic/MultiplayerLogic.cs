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
using System.Linq;
using OpenRA.Network;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class MultiplayerLogic : ChromeLogic
	{
		// Caption is dynamic
		[FluentReference("seconds")]
		const string QuickMatchSearching = "button-multiplayer-panel-quickmatch-searching";

		static readonly Action DoNothing = () => { };

		readonly Action onStart;
		readonly Action onExit;
		readonly ServerListLogic serverListLogic;

		[ObjectCreator.UseCtor]
		public MultiplayerLogic(Widget widget, ModData modData, Action onStart, Action onExit, ConnectionTarget directConnectEndPoint)
		{
			// MultiplayerLogic is a superset of the ServerListLogic
			// but cannot be a direct subclass because it needs to pass object-level state to the constructor
			serverListLogic = new ServerListLogic(widget, modData, Join);

			this.onStart = onStart;
			this.onExit = onExit;

			var directConnectButton = widget.Get<ButtonWidget>("DIRECTCONNECT_BUTTON");
			directConnectButton.OnClick = () =>
			{
				Ui.OpenWindow("DIRECTCONNECT_PANEL", new WidgetArgs
				{
					{ "openLobby", OpenLobby },
					{ "onExit", DoNothing },
					{ "directConnectEndPoint", null },
				});
			};

			var quickMatchButton = widget.GetOrNull<ButtonWidget>("QUICKMATCH_BUTTON");
			if (quickMatchButton != null)
			{
				var idle = FluentProvider.GetMessage(quickMatchButton.Text);
				var searchStarted = DateTime.Now;
				var searching = new CachedTransform<int, string>(
					seconds => FluentProvider.GetMessage(QuickMatchSearching, "seconds", seconds));
				quickMatchButton.GetText = () => QuickMatch.State == QuickMatch.Status.Searching
					? searching.Update((int)(DateTime.Now - searchStarted).TotalSeconds)
					: idle;
				quickMatchButton.OnClick = () =>
				{
					if (QuickMatch.State != QuickMatch.Status.Idle && QuickMatch.State != QuickMatch.Status.NoServers)
					{
						QuickMatch.Cancel();
						return;
					}

					QuickMatch.LobbyJoined = () => OpenLobby(QuickMatch.Cancel);
					QuickMatch.LobbyLeaving = () =>
					{
						// This can happen routinely during Recheck
						if (Ui.CurrentWindow()?.Id == "SERVER_LOBBY")
							Ui.CloseWindow();
					};

					QuickMatch.CountdownStep = step =>
					{
						if (step > 0)
							// Sound volume is correctly guided by UI sound parameters
							Game.Sound.Play(SoundType.UI, $"common|quickmatch/tminus{step}.aud");
					};

					QuickMatch.MatchStarting = () => Game.Sound.Play(SoundType.UI, "common|quickmatch/startnow.aud");
					searchStarted = DateTime.Now;
					QuickMatch.Start();
				};
			}

			var createServerButton = widget.Get<ButtonWidget>("CREATE_BUTTON");
			createServerButton.OnClick = () =>
			{
				Ui.OpenWindow("MULTIPLAYER_CREATESERVER_PANEL", new WidgetArgs
				{
					{ "openLobby", OpenLobby },
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
					Ui.OpenWindow("DIRECTCONNECT_PANEL", new WidgetArgs
					{
						{ "openLobby", OpenLobby },
						{ "onExit", DoNothing },
						{ "directConnectEndPoint", directConnectEndPoint },
					});

					widget.Visible = true;
				});
			}
		}

		void OpenLobby() { OpenLobby(null); }

		void OpenLobby(Action onLeave)
		{
			// Close the multiplayer browser
			Ui.CloseWindow();

			void OnLobbyExit()
			{
				// Open a fresh copy of the multiplayer browser
				Ui.OpenWindow("MULTIPLAYER_PANEL", new WidgetArgs
				{
					{ "onStart", onStart },
					{ "onExit", onExit },
					{ "directConnectEndPoint", null },
				});

				Game.Disconnect();

				DiscordService.UpdateStatus(DiscordState.InMenu);
			}

			Game.OpenWindow("SERVER_LOBBY", new WidgetArgs
			{
				{ "onStart", onStart },
				{
					"onExit", (Action)(() =>
					{
						// Since we piggy back on Lobby, we need to hook our callback somehow
						onLeave?.Invoke();
						OnLobbyExit();
					})
				},
				{ "skirmishMode", false }
			});
		}

		void Join(GameServer server)
		{
			if (server == null || !server.IsJoinable)
				return;

			var host = server.Address.Split(':')[0];
			var port = Exts.ParseInt32Invariant(server.Address.Split(':')[1]);

			ConnectionLogic.Connect(new ConnectionTarget(host, port), "", OpenLobby, DoNothing);
		}

		bool disposed;
		protected override void Dispose(bool disposing)
		{
			if (disposing && !disposed)
			{
				disposed = true;
				serverListLogic.Dispose();
			}

			base.Dispose(disposing);
		}
	}
}
