#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class ServerBrowserLogic
	{
		GameServer currentServer = null;
		ScrollItemWidget ServerTemplate;

		[ObjectCreator.UseCtor]
		public ServerBrowserLogic(Widget widget)
		{
			var bg = widget.GetWidget("JOINSERVER_BG");

			bg.GetWidget("JOINSERVER_PROGRESS_TITLE").Visible = true;
			bg.GetWidget<LabelWidget>("JOINSERVER_PROGRESS_TITLE").Text = "Fetching game list...";

			ServerList.Query(RefreshServerList);

			bg.GetWidget("SERVER_INFO").IsVisible = () => currentServer != null;
			var preview = bg.GetWidget<MapPreviewWidget>("MAP_PREVIEW");
			preview.Map = () => CurrentMap();
			preview.IsVisible = () => CurrentMap() != null;

			bg.GetWidget<LabelWidget>("SERVER_IP").GetText = () => currentServer.Address;
			bg.GetWidget<LabelWidget>("SERVER_MODS").GetText = () => GenerateModsLabel(currentServer);
			bg.GetWidget<LabelWidget>("MAP_TITLE").GetText = () => (CurrentMap() != null) ? CurrentMap().Title : "Unknown";
			bg.GetWidget<LabelWidget>("MAP_PLAYERS").GetText = () =>
			{
				if (currentServer == null)
					return "";
				string ret = currentServer.Players.ToString();
				if (CurrentMap() != null)
					ret += "/" + CurrentMap().PlayerCount.ToString();
				return ret;
			};

			var sl = bg.GetWidget<ScrollPanelWidget>("SERVER_LIST");
			ServerTemplate = sl.GetWidget<ScrollItemWidget>("SERVER_TEMPLATE");

			bg.GetWidget<ButtonWidget>("REFRESH_BUTTON").OnClick = () =>
			{
				bg.GetWidget("JOINSERVER_PROGRESS_TITLE").Visible = true;
				bg.GetWidget<LabelWidget>("JOINSERVER_PROGRESS_TITLE").Text = "Fetching game list...";
				sl.RemoveChildren();
				currentServer = null;

				ServerList.Query(RefreshServerList);
			};

			bg.GetWidget<ButtonWidget>("CANCEL_BUTTON").OnClick = () => Widget.CloseWindow();
			bg.GetWidget<ButtonWidget>("DIRECTCONNECT_BUTTON").OnClick = () =>
			{
				Widget.CloseWindow();
				Widget.OpenWindow("DIRECTCONNECT_BG");
			};

			bg.GetWidget<ButtonWidget>("JOIN_BUTTON").OnClick = () =>
			{
				if (currentServer == null)
					return;

				Widget.CloseWindow();
				Game.JoinServer(currentServer.Address.Split(':')[0], int.Parse(currentServer.Address.Split(':')[1]));
			};
		}

		Map CurrentMap()
		{
			return (currentServer == null) ? null : Game.modData.FindMapByUid(currentServer.Map);
		}

		static string GenerateModLabel(KeyValuePair<string,string> mod)
		{
			if (Mod.AllMods.ContainsKey(mod.Key))
				return "{0} ({1})".F(Mod.AllMods[mod.Key].Title, mod.Value);

			return "Unknown Mod: {0}".F(mod.Key);
		}

		public static string GenerateModsLabel(GameServer s)
		{
			return s.UsefulMods.Select(m => GenerateModLabel(m)).JoinWith("\n");
		}

		void RefreshServerList(IEnumerable<GameServer> games)
		{
			var r = Widget.RootWidget;
			var bg = r.GetWidget("JOINSERVER_BG");

			if (bg == null) // We got a MasterServer reply AFTER the browser is gone, just return to prevent crash - Gecko
				return;

			var sl = bg.GetWidget<ScrollPanelWidget>("SERVER_LIST");

			sl.RemoveChildren();
			currentServer = null;

			if (games == null)
			{
				r.GetWidget("JOINSERVER_PROGRESS_TITLE").Visible = true;
				r.GetWidget<LabelWidget>("JOINSERVER_PROGRESS_TITLE").Text = "Failed to contact master server.";
				return;
			}

			var gamesWaiting = games.Where(g => g.CanJoin());

			if (gamesWaiting.Count() == 0)
			{
				r.GetWidget("JOINSERVER_PROGRESS_TITLE").Visible = true;
				r.GetWidget<LabelWidget>("JOINSERVER_PROGRESS_TITLE").Text = "No games found.";
				return;
			}

			r.GetWidget("JOINSERVER_PROGRESS_TITLE").Visible = false;

			currentServer = gamesWaiting.FirstOrDefault();

			foreach (var loop in gamesWaiting)
			{
				var game = loop;
				var item = ScrollItemWidget.Setup(ServerTemplate,
					() => currentServer == game,
					() => currentServer = game);
				item.GetWidget<LabelWidget>("TITLE").GetText = () => "{0} ({1})".F(game.Name, game.Address);
				sl.AddChild(item);
			}
		}
	}
}
