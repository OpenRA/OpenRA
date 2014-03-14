#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Network;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class LobbyMapPreviewLogic
	{
		[ObjectCreator.UseCtor]
		internal LobbyMapPreviewLogic(Widget widget, OrderManager orderManager, LobbyLogic lobby)
		{
			var mapPreview = widget.Get<MapPreviewWidget>("MAP_PREVIEW");
			mapPreview.Preview = () => lobby.Map;
			mapPreview.OnMouseDown = mi => LobbyUtils.SelectSpawnPoint(orderManager, mapPreview, lobby.Map, mi);
			mapPreview.SpawnClients = () => LobbyUtils.GetSpawnClients(orderManager, lobby.Map);

			var mapTitle = widget.GetOrNull<LabelWidget>("MAP_TITLE");
			if (mapTitle != null)
				mapTitle.GetText = () => lobby.Map.Title;

			var mapType = widget.GetOrNull<LabelWidget>("MAP_TYPE");
			if (mapType != null)
				mapType.GetText = () => lobby.Map.Type;

			var mapAuthor = widget.GetOrNull<LabelWidget>("MAP_AUTHOR");
			if (mapAuthor != null)
				mapAuthor.GetText = () => "Created by {0}".F(lobby.Map.Author);
		}
	}
}
