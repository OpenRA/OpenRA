#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.IO;
using System.Linq;
using System.Net;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class MissionBrowserLogic
	{
		readonly Action onStart;
		readonly ScrollPanelWidget descriptionPanel;
		readonly LabelWidget description;
		readonly SpriteFont descriptionFont;

		MapPreview selectedMapPreview;

		[ObjectCreator.UseCtor]
		public MissionBrowserLogic(Widget widget, Action onStart, Action onExit)
		{
			this.onStart = onStart;

			var missionList = widget.Get<ScrollPanelWidget>("MISSION_LIST");
			var template = widget.Get<ScrollItemWidget>("MISSION_TEMPLATE");

			widget.Get("MISSION_INFO").IsVisible = () => selectedMapPreview != null;

			var previewWidget = widget.Get<MapPreviewWidget>("MISSION_PREVIEW");
			previewWidget.Preview = () => selectedMapPreview;

			descriptionPanel = widget.Get<ScrollPanelWidget>("MISSION_DESCRIPTION_PANEL");
			description = widget.Get<LabelWidget>("MISSION_DESCRIPTION");
			descriptionFont = Game.Renderer.Fonts[description.Font];

			var yaml = new MiniYaml(null, Game.modData.Manifest.Missions.Select(MiniYaml.FromFile).Aggregate(MiniYaml.MergeLiberal)).ToDictionary();

			var missionMapPaths = yaml["Missions"].Nodes.Select(n => Path.GetFullPath(n.Key));

			var maps = Game.modData.MapCache
				.Where(p => p.Status == MapStatus.Available && missionMapPaths.Contains(Path.GetFullPath(p.Map.Path)))
				.Select(p => p.Map);

			missionList.RemoveChildren();
			foreach (var m in maps)
			{
				var map = m;

				var item = ScrollItemWidget.Setup(template,
					() => selectedMapPreview != null && selectedMapPreview.Uid == map.Uid,
					() => SelectMap(map),
					StartMission);

				item.Get<LabelWidget>("TITLE").GetText = () => map.Title;
				missionList.AddChild(item);
			}

			if (maps.Any())
				SelectMap(maps.First());

			widget.Get<ButtonWidget>("STARTGAME_BUTTON").OnClick = StartMission;

			widget.Get<ButtonWidget>("BACK_BUTTON").OnClick = () =>
			{
				Game.Disconnect();
				Ui.CloseWindow();
				onExit();
			};
		}

		void SelectMap(Map map)
		{
			selectedMapPreview = Game.modData.MapCache[map.Uid];

			var text = map.Description != null ? map.Description.Replace("\\n", "\n") : "";
			text = WidgetUtils.WrapText(text, description.Bounds.Width, descriptionFont);
			description.Text = text;
			description.Bounds.Height = descriptionFont.Measure(text).Y;
			descriptionPanel.Layout.AdjustChildren();
		}

		void StartMission()
		{
			OrderManager om = null;

			Action lobbyReady = null;
			lobbyReady = () =>
			{
				Game.LobbyInfoChanged -= lobbyReady;
				onStart();
				om.IssueOrder(Order.Command("state {0}".F(Session.ClientState.Ready)));
			};
			Game.LobbyInfoChanged += lobbyReady;

			om = Game.JoinServer(IPAddress.Loopback.ToString(), Game.CreateLocalServer(selectedMapPreview.Uid), "");
		}
	}
}
