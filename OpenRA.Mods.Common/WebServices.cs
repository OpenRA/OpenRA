#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Net;
using System.Text;

namespace OpenRA.Mods.Common
{
	public enum ModVersionStatus { NotChecked, Latest, Outdated, Unknown, PlaytestAvailable }

	public class WebServices : IGlobalModData
	{
		public readonly string ServerList = "https://master.openra.net/games";
		public readonly string ServerAdvertise = "https://master.openra.net/ping";
		public readonly string MapRepository = "https://resource.openra.net/map/";
		public readonly string GameNews = "https://master.openra.net/gamenews";
		public readonly string GameNewsFileName = "news.yaml";
		public readonly string VersionCheck = "https://master.openra.net/versioncheck";

		public ModVersionStatus ModVersionStatus { get; private set; }
		const int VersionCheckProtocol = 1;

		public void CheckModVersion()
		{
			Action<DownloadDataCompletedEventArgs> onComplete = i =>
			{
				if (i.Error != null)
					return;
				try
				{
					var data = Encoding.UTF8.GetString(i.Result);

					var status = ModVersionStatus.Latest;
					switch (data)
					{
						case "outdated": status = ModVersionStatus.Outdated; break;
						case "unknown": status = ModVersionStatus.Unknown; break;
						case "playtest": status = ModVersionStatus.PlaytestAvailable; break;
					}

					Game.RunAfterTick(() => ModVersionStatus = status);
				}
				catch { }
			};

			var queryURL = VersionCheck + "?protocol={0}&engine={1}&mod={2}&version={3}".F(
				VersionCheckProtocol,
				Uri.EscapeUriString(Game.EngineVersion),
				Uri.EscapeUriString(Game.ModData.Manifest.Id),
				Uri.EscapeUriString(Game.ModData.Manifest.Metadata.Version));

			new Download(queryURL, _ => { }, onComplete);
		}
	}
}
