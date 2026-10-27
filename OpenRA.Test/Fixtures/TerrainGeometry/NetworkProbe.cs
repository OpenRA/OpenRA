// SPDX-License-Identifier: GPL-3.0-or-later
using OpenRA;
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.TerrainProbe;

// Test-only lobby automation; both clients use the normal network order manager.
public static class NetworkProbe
{
	public static bool TryStart(Arguments args)
	{
		if (!args.Contains("TerrainProbe.NetworkPort"))
			return false;

		Ui.ResetAll();
		Game.Settings.Save();
		var port = int.Parse(args.GetValue("TerrainProbe.NetworkPort", "0"), System.Globalization.CultureInfo.InvariantCulture);
		if (args.GetValue("TerrainProbe.NetworkHost", "False") == "True")
		{
			var map = Game.ModData.MapCache.Single(m => Path.GetFileName(m.Path) == "terrain-probe");
			Game.CreateServer(new ServerSettings
			{
				Name = "Local terrain regression",
				ListenPort = port,
				Map = map.Uid,
				AdvertiseOnline = false,
				AdvertiseOnLocalNetwork = false,
				DiscoverNatDevices = false,
				QueryMapRepository = false,
				EnableGeoIP = false
			});
		}

		var om = Game.JoinServer(new ConnectionTarget("127.0.0.1", port), "");
		var ready = false;
		Game.LobbyInfoChanged += () =>
		{
			if (!ready && om.World == null && om.LocalClient != null && om.LobbyInfo.NonBotClients.Count() == 2)
			{
				ready = true;
				om.IssueOrder(Order.Command("state Ready"));
			}
		};
		return true;
	}
}
