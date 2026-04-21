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

using System.Threading.Tasks;
using OpenRA.Support;

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
			Task.Run(async () =>
			{
				var queryURL = new HttpQueryBuilder(VersionCheck)
				{
					{ "protocol", VersionCheckProtocol },
					{ "engine", Game.EngineVersion },
					{ "mod", Game.ModData.Manifest.Id },
					{ "version", Game.ModData.Manifest.Metadata.Version }
				}.ToString();

				try
				{
					var client = HttpClientFactory.Create();

					var httpResponseMessage = await client.GetAsync(queryURL);
					var result = await httpResponseMessage.Content.ReadAsStringAsync();

					var status = ModVersionStatus.Latest;
					switch (result)
					{
						case "outdated": status = ModVersionStatus.Outdated; break;
						case "unknown": status = ModVersionStatus.Unknown; break;
						case "playtest": status = ModVersionStatus.PlaytestAvailable; break;
					}

					Game.RunAfterTick(() => ModVersionStatus = status);
				}
				catch { }
			});
		}
	}
}
