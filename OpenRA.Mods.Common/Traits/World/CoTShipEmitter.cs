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
using System.Text;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Emits Cursor-on-Target (CoT) messages for ship lifecycle events (spawn, damage, killed) and periodic heartbeats while afloat/alive.")]
	public sealed class CoTShipEmitterInfo : CoTEmitterInfoBase
	{
		[Desc("Include <track> element with speed/course in CoT detail.")]
		public readonly bool IncludeTrack = true;

		[Desc("Include <depth> element for submarines in CoT detail.")]
		public readonly bool IncludeDepth = true;

		[Desc("Depth mode for submarines. 'Fixed' uses DepthFixedMeters; other modes may be added later.")]
		public readonly string DepthMode = "Fixed";

		[Desc("Default fixed depth (meters, positive down) for submarines when DepthMode='Fixed'.")]
		public readonly double DepthFixedMeters = 50.0;

		[Desc("When emitting depth for submarines, also set point.hae to -depth.")]
		public readonly bool UseDepthAsNegativeHae = true;

		[Desc("Optional per-actor submarine flag overrides. Key: actor type name, Value: true for submarine.")]
		public readonly Dictionary<string, bool> ActorIsSubmarine = [];

		[Desc("Optional per-actor fixed depth overrides (meters, positive down). Key: actor type name, Value: depth.")]
		public readonly Dictionary<string, int> ActorDepthMeters = [];

		public CoTShipEmitterInfo()
		{
			Callsign = "OpenRA-Ship";
		}

		public override object Create(ActorInitializer init) { return new CoTShipEmitter(init, this); }
	}

	public sealed class CoTShipEmitter : CoTEmitterBase<CoTShipEmitterInfo>
	{
		const double MinTimeDeltaSeconds = 0.001;

		double? lastLat;
		double? lastLon;
		DateTime? lastSendUtc;

		protected override CoTDomain Domain => CoTDomain.Vessel;

		public CoTShipEmitter(ActorInitializer init, CoTShipEmitterInfo info)
			: base(init, info) { }

		protected override void SendEvent(
			Actor self,
			double lat,
			double lon,
			string type,
			string callsign,
			int staleSeconds,
			string reason,
			string stateOverride = null,
			string milsymOverride = null)
		{
			var uid = $"OpenRA-AID-{self.ActorID}";
			var now = DateTime.UtcNow;
			var stale = now.AddSeconds(Math.Max(1, staleSeconds));
			var milsymId = !string.IsNullOrEmpty(milsymOverride) ? milsymOverride : ResolveMilsymId(self, stateOverride);

			// Derive telemetry
			var speedMps = 0.0;
			var courseDeg = 0.0;
			if (lastLat.HasValue && lastLon.HasValue && lastSendUtc.HasValue)
			{
				var dt = Math.Max(MinTimeDeltaSeconds, (now - lastSendUtc.Value).TotalSeconds);
				var dist = HaversineMeters(lastLat.Value, lastLon.Value, lat, lon);
				speedMps = dist / dt;
				courseDeg = InitialBearingDeg(lastLat.Value, lastLon.Value, lat, lon);
			}

			// Determine submarine depth and HAE output
			var isSub = IsSubmarine(self);
			var depthMeters = (int?)null;
			var haeOut = Info.Hae;
			if (isSub && Info.IncludeDepth && string.Equals(Info.DepthMode, "Fixed", StringComparison.OrdinalIgnoreCase))
			{
				var d = ResolveDepthMeters(self);
				if (d > 0)
				{
					depthMeters = d;
					if (Info.UseDepthAsNegativeHae)
						haeOut = -d;
				}
			}

			double? speedOpt = Info.IncludeTrack ? speedMps : null;
			double? courseOpt = Info.IncludeTrack ? courseDeg : null;

			var cot = BuildCotXmlWithTelemetry(uid, lat, lon, haeOut, Info.Ce, Info.Le, type, callsign, milsymId, now, stale, speedOpt, courseOpt, depthMeters);

			try
			{
				var data = Encoding.UTF8.GetBytes(cot);
				CotOutputService.EnsureInitializedFrom(Info.UdpHost, Info.UdpPort);
				CotOutputService.Enqueue(data);
				Log.Write("cot", string.Format(CultureInfo.InvariantCulture,
					"send {0} actor={1} lat={2} lon={3} speed={4:0.#}mps course={5:000} depth={6}m target={7} bytes={8}",
					reason,
					self.Info.Name,
					lat.ToString("0.########", CultureInfo.InvariantCulture),
					lon.ToString("0.########", CultureInfo.InvariantCulture),
					speedMps,
					(int)Math.Round(courseDeg) % 360,
					depthMeters.HasValue ? depthMeters.Value.ToString(CultureInfo.InvariantCulture) : "-",
					Endpoint, data.Length));
			}
			catch (Exception e)
			{
				Log.Write("cot", e);
			}

			lastLat = lat;
			lastLon = lon;
			lastSendUtc = now;
		}

		string BuildCotXmlWithTelemetry(
			string uid, double lat, double lon, double hae, double ce, double le,
			string type, string callsign, string milsymId, DateTime start, DateTime stale,
			double? speedMps, double? courseDeg, int? depthMeters)
		{
			var nowStr = start.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
			var staleStr = stale.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
			var latStr = lat.ToString("0.########", CultureInfo.InvariantCulture);
			var lonStr = lon.ToString("0.########", CultureInfo.InvariantCulture);
			var haeStr = hae.ToString("0.###", CultureInfo.InvariantCulture);
			var ceStr = ce.ToString("0.###", CultureInfo.InvariantCulture);
			var leStr = le.ToString("0.###", CultureInfo.InvariantCulture);

			var typeStr = Clean(type);
			var callsignStr = Clean(callsign);
			var milsymStr = Clean(milsymId);

			var sb = new StringBuilder();
			sb.Append("<event version=\"2.0\" ");
			sb.Append(CultureInfo.InvariantCulture, $"uid=\"{uid}\" ");
			sb.Append(CultureInfo.InvariantCulture, $"type=\"{typeStr}\" ");
			sb.Append(CultureInfo.InvariantCulture, $"time=\"{nowStr}\" start=\"{nowStr}\" stale=\"{staleStr}\" how=\"m-g\">");
			sb.Append(CultureInfo.InvariantCulture, $"<point lat=\"{latStr}\" lon=\"{lonStr}\" hae=\"{haeStr}\" ce=\"{ceStr}\" le=\"{leStr}\"/>");
			sb.Append("<detail>");
			if (Info.IncludeMilsymDetail && !string.IsNullOrEmpty(milsymStr))
				sb.Append(CultureInfo.InvariantCulture, $"<__milsym id=\"{SecurityElementEscape(milsymStr)}\"/>");
			if (Info.IncludeColor)
				sb.Append(CultureInfo.InvariantCulture, $"<color argb=\"{Info.ColorArgb}\" value=\"{Info.ColorValue}\"/>");
			if (Info.IncludeLink)
			{
				var parentCs = Clean(Info.LinkParentCallsign ?? string.Empty);
				var relation = Clean(string.IsNullOrEmpty(Info.LinkRelation) ? "p-p" : Info.LinkRelation);
				sb.Append("<link ");
				sb.Append(CultureInfo.InvariantCulture, $"parent_callsign=\"{SecurityElementEscape(parentCs)}\" ");
				sb.Append(CultureInfo.InvariantCulture, $"production_time=\"{nowStr}\" ");
				sb.Append(CultureInfo.InvariantCulture, $"relation=\"{SecurityElementEscape(relation)}\" ");
				sb.Append(CultureInfo.InvariantCulture, $"type=\"{SecurityElementEscape(typeStr)}\" ");
				sb.Append(CultureInfo.InvariantCulture, $"uid=\"{SecurityElementEscape(uid)}\"/>");
			}

			sb.Append(CultureInfo.InvariantCulture, $"<contact callsign=\"{SecurityElementEscape(callsignStr)}\"/>");
			if (Info.IncludeTrack && speedMps.HasValue && courseDeg.HasValue)
				sb.Append(CultureInfo.InvariantCulture, $"<track speed=\"{Math.Round(speedMps.Value)}\" course=\"{(int)Math.Round(courseDeg.Value) % 360:000}\"/>");
			if (Info.IncludeDepth && depthMeters.HasValue)
				sb.Append(CultureInfo.InvariantCulture, $"<depth value=\"{depthMeters.Value}\" unit=\"m\"/>");
			if (Info.IncludeArchive)
				sb.Append("<archive/>");
			sb.Append("</detail>");
			sb.Append("</event>");
			return sb.ToString();
		}

		static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
		{
			const double R = 6371000.0;
			static double ToRad(double d) => d * Math.PI / 180.0;
			var dLat = ToRad(lat2 - lat1);
			var dLon = ToRad(lon2 - lon1);
			var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
			var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
			return R * c;
		}

		static double InitialBearingDeg(double lat1, double lon1, double lat2, double lon2)
		{
			static double ToRad(double d) => d * Math.PI / 180.0;
			static double ToDeg(double r) => r * 180.0 / Math.PI;
			var phi1 = ToRad(lat1);
			var phi2 = ToRad(lat2);
			var dLon = ToRad(lon2 - lon1);
			var y = Math.Sin(dLon) * Math.Cos(phi2);
			var x = Math.Cos(phi1) * Math.Sin(phi2) - Math.Sin(phi1) * Math.Cos(phi2) * Math.Cos(dLon);
			var brng = Math.Atan2(y, x);
			var deg = (ToDeg(brng) + 360.0) % 360.0;
			return deg;
		}

		bool IsSubmarine(Actor self)
		{
			if (TryGetValueAnyCase(Info.ActorIsSubmarine, self.Info.Name, out var isSub) && isSub)
				return true;
			return false;
		}

		int ResolveDepthMeters(Actor self)
		{
			if (TryGetValueAnyCase(Info.ActorDepthMeters, self.Info.Name, out var d) && d > 0)
				return d;
			return (int)Math.Round(Info.DepthFixedMeters);
		}
	}
}
