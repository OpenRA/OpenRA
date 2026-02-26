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
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using OpenRA;
using OpenRA.Traits;
using CotSvc = OpenRA.Mods.Common.CotOutputService;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World | SystemActors.Player)]
	[Desc("Broadcasts Cursor-on-Target (CoT) messages over UDP when certain orders are issued (e.g., PlaceBeacon).")]
	public sealed class CoTBroadcasterInfo : TraitInfo
	{
		[Desc("UDP target host or IP.")]
		public readonly string UdpHost = "127.0.0.1";

		[Desc("UDP target port.")]
		public readonly int UdpPort = 4242;

		[Desc("List of order strings to trigger CoT messages (e.g., PlaceBeacon).")]
		public readonly string[] TargetOrders = ["PlaceBeacon"];

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

		public override object Create(ActorInitializer init) { return new CoTBroadcaster(this); }
	}

	public sealed class CoTBroadcaster : INotifyOrderIssued, IResolveOrder
	{
		readonly CoTBroadcasterInfo info;
		readonly HashSet<string> orderSet;
		readonly IPEndPoint endpoint;

		public CoTBroadcaster(CoTBroadcasterInfo info)
		{
			this.info = info;
			orderSet = (info.TargetOrders ?? []).ToHashSet(StringComparer.OrdinalIgnoreCase);
			endpoint = new IPEndPoint(ParseAddress(info.UdpHost), info.UdpPort);
			CotSvc.EnsureInitializedFrom(info.UdpHost, info.UdpPort);
			Log.Write("cot", string.Format(System.Globalization.CultureInfo.InvariantCulture,
				"init endpoint={0} orders={1} callsign={2} type={3}",
				endpoint, string.Join(",", orderSet), info.Callsign, info.CotType));
		}

		static IPAddress ParseAddress(string s)
		{
			if (IPAddress.TryParse(s, out var ip))
				return ip;
			var addresses = Dns.GetHostAddresses(s);
			return addresses.Length > 0 ? addresses[0] : IPAddress.Loopback;
		}

		bool INotifyOrderIssued.OrderIssued(World world, string orderString, Target target)
		{
			if (string.IsNullOrEmpty(orderString) || !orderSet.Contains(orderString))
				return false;

			// Defer CoT sending to ResolveOrder where we have an Actor context,
			// allowing a stable UID based on the issuing actor's ActorID.
			return false;
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order == null)
				return;

			var orderString = order.OrderString;
			if (string.IsNullOrEmpty(orderString) || !orderSet.Contains(orderString))
				return;

			var world = self.World;

			// Map target position to cell, then lat/lon using GeoTransform
			var cell = world.Map.CellContaining(order.Target.CenterPosition);
			if (!world.Map.TryCellToLatLon(cell, out var lat, out var lon))
			{
				Log.Write("cot", $"skip order={orderString} no lat/lon (map not georef?)");
				return;
			}

			var now = DateTime.UtcNow;
			var start = now;
			var stale = now.AddSeconds(Math.Max(1, info.StaleSeconds));

			var uid = $"OpenRA-AID-{self.ActorID}";
			var cot = BuildCotXml(uid, lat, lon, info.Hae, info.Ce, info.Le, info.CotType, info.Callsign, start, stale);

			// Enqueue for async send via CotOutputService
			try
			{
				var data = Encoding.UTF8.GetBytes(cot);
				CotSvc.EnsureInitializedFrom(info.UdpHost, info.UdpPort);
				CotSvc.Enqueue(data);
				Log.Write("cot", string.Format(System.Globalization.CultureInfo.InvariantCulture,
					"send order={0} lat={1} lon={2} target={3} bytes={4}",
					orderString,
					lat.ToString("0.########", System.Globalization.CultureInfo.InvariantCulture),
					lon.ToString("0.########", System.Globalization.CultureInfo.InvariantCulture),
					endpoint, data.Length));
				Log.Write("cot", "payload " + cot);
			}
			catch (Exception e)
			{
				// Intentionally avoid disrupting gameplay, but log the error for debugging
				Log.Write("cot", e);
			}
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
