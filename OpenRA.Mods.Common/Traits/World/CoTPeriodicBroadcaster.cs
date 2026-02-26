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
using System.Globalization;
using System.Net;
using System.Text;
using OpenRA.Traits;
using CotSvc = OpenRA.Mods.Common.CotOutputService;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Broadcasts periodic Cursor-on-Target (CoT) messages for the actor's current position.")]
	public sealed class CoTPeriodicBroadcasterInfo : TraitInfo
	{
		[Desc("UDP target host or IP.")]
		public readonly string UdpHost = "127.0.0.1";

		[Desc("UDP target port.")]
		public readonly int UdpPort = 4242;

		[Desc("Device callsign to include in CoT detail.")]
		public readonly string Callsign = "OpenRA";

		[Desc("CoT type (default generic user).")]
		public readonly string CotType = "a-f-G-U-C";

		[Desc("Reported height above ellipsoid (meters).")]
		public readonly double Hae = 0.0;

		[Desc("Circular error (meters).")]
		public readonly double Ce = 50.0;

		[Desc("Linear error (meters).")]
		public readonly double Le = 50.0;

		[Desc("Seconds after event when the message should be considered stale.")]
		public readonly int StaleSeconds = 120;

		[Desc("Number of ticks between position checks and potential sends.")]
		public readonly int UpdateIntervalTicks = 25;

		[Desc("Maximum ticks between sends; ensures updates even when stationary.")]
		public readonly int MaxIntervalTicks = 250;

		public override object Create(ActorInitializer init) { return new CoTPeriodicBroadcaster(this); }
	}

	public sealed class CoTPeriodicBroadcaster : ITick, INotifyAddedToWorld
	{
		readonly CoTPeriodicBroadcasterInfo info;
		readonly IPEndPoint endpoint;

		int intervalCounter;
		int ticksSinceLastSend;
		bool haveLastCell;
		CPos lastCell;
		string uid;

		public CoTPeriodicBroadcaster(CoTPeriodicBroadcasterInfo info)
		{
			this.info = info;
			endpoint = new IPEndPoint(ParseAddress(info.UdpHost), info.UdpPort);
			CotSvc.EnsureInitializedFrom(info.UdpHost, info.UdpPort);
			Log.Write("cot", string.Format(CultureInfo.InvariantCulture,
				"periodic init endpoint={0} callsign={1} type={2} updateTicks={3} maxTicks={4}",
				endpoint, info.Callsign, info.CotType, info.UpdateIntervalTicks, info.MaxIntervalTicks));
		}

		static IPAddress ParseAddress(string s)
		{
			if (IPAddress.TryParse(s, out var ip))
				return ip;
			var addresses = Dns.GetHostAddresses(s);
			return addresses.Length > 0 ? addresses[0] : IPAddress.Loopback;
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			// Initialize last cell to current to avoid duplicate immediate sends;
			// initial spawn message is handled by CoTOnSpawnBroadcaster if present.
			var world = self.World;
			lastCell = world.Map.CellContaining(self.CenterPosition);
			haveLastCell = true;
			intervalCounter = 0;
			ticksSinceLastSend = 0;
			uid = $"OpenRA-AID-{self.ActorID}";
		}

		void ITick.Tick(Actor self)
		{
			intervalCounter++;
			if (intervalCounter < Math.Max(1, info.UpdateIntervalTicks))
				return;

			intervalCounter = 0;
			ticksSinceLastSend += info.UpdateIntervalTicks;

			var world = self.World;
			var cell = world.Map.CellContaining(self.CenterPosition);
			var moved = !haveLastCell || cell != lastCell;
			var dueToTime = ticksSinceLastSend >= Math.Max(1, info.MaxIntervalTicks);

			if (!moved && !dueToTime)
			{
				// Nothing to send this cycle; update last cell snapshot and return
				lastCell = cell;
				haveLastCell = true;
				return;
			}

			if (!world.Map.TryCellToLatLon(cell, out var lat, out var lon))
			{
				Log.Write("cot", "skip periodic no lat/lon (map not georef?)");
				lastCell = cell;
				haveLastCell = true;
				return;
			}

			var now = DateTime.UtcNow;
			var start = now;
			var stale = now.AddSeconds(Math.Max(1, info.StaleSeconds));
			var cot = BuildCotXml(uid, lat, lon, info.Hae, info.Ce, info.Le, info.CotType, info.Callsign, start, stale);

			try
			{
				var data = Encoding.UTF8.GetBytes(cot);
				CotSvc.EnsureInitializedFrom(info.UdpHost, info.UdpPort);
				CotSvc.Enqueue(data);
				Log.Write("cot", string.Format(CultureInfo.InvariantCulture,
					"send periodic actor={0} moved={1} lat={2} lon={3} target={4} bytes={5}",
					self.Info.Name,
					moved,
					lat.ToString("0.########", CultureInfo.InvariantCulture),
					lon.ToString("0.########", CultureInfo.InvariantCulture),
					endpoint, data.Length));
				Log.Write("cot", "payload " + cot);
			}
			catch (Exception e)
			{
				// Intentionally avoid disrupting gameplay, but log the error for debugging
				Log.Write("cot", e);
			}

			// Reset schedule
			ticksSinceLastSend = 0;
			lastCell = cell;
			haveLastCell = true;
		}

		static string BuildCotXml(string uid, double lat, double lon, double hae, double ce, double le, string type, string callsign, DateTime start, DateTime stale)
		{
			var nowStr = start.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
			var startStr = nowStr;
			var staleStr = stale.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
			var latStr = lat.ToString("0.########", CultureInfo.InvariantCulture);
			var lonStr = lon.ToString("0.########", CultureInfo.InvariantCulture);
			var haeStr = hae.ToString("0.###", CultureInfo.InvariantCulture);
			var ceStr = ce.ToString("0.###", CultureInfo.InvariantCulture);
			var leStr = le.ToString("0.###", CultureInfo.InvariantCulture);

			// Minimal CoT 2.0 message
			var sb = new StringBuilder();
			sb.Append("<event version=\"2.0\" ");
			sb.Append(CultureInfo.InvariantCulture, $"uid=\"{uid}\" ");
			sb.Append(CultureInfo.InvariantCulture, $"type=\"{type}\" ");
			sb.Append(CultureInfo.InvariantCulture, $"time=\"{nowStr}\" start=\"{startStr}\" stale=\"{staleStr}\" how=\"m-g\">");
			sb.Append(CultureInfo.InvariantCulture, $"<point lat=\"{latStr}\" lon=\"{lonStr}\" hae=\"{haeStr}\" ce=\"{ceStr}\" le=\"{leStr}\"/>");
			sb.Append("<detail>");
			sb.Append(CultureInfo.InvariantCulture, $"<contact callsign=\"{SecurityElementEscape(callsign)}\"/>");
			sb.Append("</detail>");
			sb.Append("</event>");
			return sb.ToString();
		}

		static string SecurityElementEscape(string s)
		{
			if (string.IsNullOrEmpty(s)) return string.Empty;
			return s.Replace("&", "&amp;").Replace("\"", "&quot;").Replace("'", "&apos;").Replace("<", "&lt;").Replace(">", "&gt;");
		}
	}
}
