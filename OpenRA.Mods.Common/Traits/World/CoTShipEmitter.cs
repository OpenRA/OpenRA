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
using System.Net;
using System.Text;
using OpenRA.Traits;
using CotSvc = OpenRA.Mods.Common.CotOutputService;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Emits Cursor-on-Target (CoT) messages for ship lifecycle events (spawn, damage, killed) and periodic heartbeats while afloat/alive.")]
	public sealed class CoTShipEmitterInfo : PausableConditionalTraitInfo
	{
		[Desc("UDP target host or IP.")]
		public readonly string UdpHost = "127.0.0.1";

		[Desc("UDP target port.")]
		public readonly int UdpPort = 4242;

		[Desc("Default device callsign to include in CoT detail. Can be overridden per-actor via ActorCallsigns.")]
		public readonly string Callsign = "OpenRA-Ship";

		[Desc("Default CoT type (symbol id). Can be overridden per-actor via ActorSymbols.")]
		public readonly string CotType = "a-f-G-U-C";

		[Desc("Reported height above ellipsoid (meters).")]
		public readonly double Hae = 0.0;

		[Desc("Circular error (meters).")]
		public readonly double Ce = 25.0;

		[Desc("Linear error (meters).")]
		public readonly double Le = 25.0;

		[Desc("Seconds after event when the message should be considered stale.")]
		public readonly int StaleSeconds = 120;

		[Desc("Number of ticks between heartbeat checks and potential sends.")]
		public readonly int UpdateIntervalTicks = 25;

		[Desc("Maximum ticks between heartbeat sends; ensures updates even when stationary.")]
		public readonly int MaxIntervalTicks = 250;

		[Desc("Optional per-actor callsign overrides. Key: actor type name, Value: callsign.")]
		public readonly Dictionary<string, string> ActorCallsigns = [];

		[Desc("Optional per-actor symbol (type) overrides. Key: actor type name, Value: CoT type id.")]
		public readonly Dictionary<string, string> ActorSymbols = [];

		[Desc("Optional per-actor damage-state symbol overrides. Key: actor type name. Nested keys: Undamaged, Light, Medium, Heavy, Critical, Dead.")]
		public readonly Dictionary<string, Dictionary<string, string>> ActorDamageSymbols = [];

		[Desc("Default MIL-STD-2525 symbol ID for __milsym detail. Can be overridden per-actor via ActorMilsymIds.")]
		public readonly string MilsymId = string.Empty;

		[Desc("Optional per-actor 2525 symbol overrides. Key: actor type name, Value: 2525 symbol id.")]
		public readonly Dictionary<string, string> ActorMilsymIds = [];

		[Desc("Optional per-actor damage-state 2525 symbol overrides. Key: actor type name. Nested keys: Undamaged, Light, Medium, Heavy, Critical, Dead.")]
		public readonly Dictionary<string, Dictionary<string, string>> ActorDamageMilsymIds = [];

		[Desc("Include <__milsym> element in CoT detail when MilsymId is not empty.")]
		public readonly bool IncludeMilsymDetail = true;

		[Desc("Include <color> element in CoT detail.")]
		public readonly bool IncludeColor = true;

		[Desc("Color argb attribute for <color> detail.")]
		public readonly int ColorArgb = -1;

		[Desc("Color value attribute for <color> detail.")]
		public readonly int ColorValue = -1;

		[Desc("Include <link> element in CoT detail.")]
		public readonly bool IncludeLink = true;

		[Desc("Parent callsign to include in <link> element.")]
		public readonly string LinkParentCallsign = string.Empty;

		[Desc("Relation attribute for <link> element.")]
		public readonly string LinkRelation = "p-p";

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

		[Desc("Include <archive/> element in CoT detail.")]
		public readonly bool IncludeArchive = true;

		public override object Create(ActorInitializer init) { return new CoTShipEmitter(init, this); }
	}

	public sealed class CoTShipEmitter : PausableConditionalTrait<CoTShipEmitterInfo>, INotifyAddedToWorld, INotifyDamageStateChanged, INotifyKilled, ITick
	{
		readonly CoTShipEmitterInfo info;
		readonly IPEndPoint endpoint;
		bool heartbeatsDisabled;
		bool initialized;
		int intervalCounter;
		int ticksSinceLastSend;
		bool haveLastCell;
		CPos lastCell;
		string uid;
		CoTVisibilityRouter router;
		bool wasEmitting;

		// Telemetry state
		double? lastLat;
		double? lastLon;
		DateTime? lastSendUtc;

		public CoTShipEmitter(ActorInitializer init, CoTShipEmitterInfo info)
			: base(info)
		{
			this.info = info;
			endpoint = new IPEndPoint(ParseAddress(info.UdpHost), info.UdpPort);
			CotSvc.EnsureInitializedFrom(info.UdpHost, info.UdpPort);
			Log.Write("cot", string.Format(CultureInfo.InvariantCulture,
				"ship-emitter init endpoint={0} callsign={1} type={2} updateTicks={3} maxTicks={4}",
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
			uid = $"OpenRA-AID-{self.ActorID}";
			var world = self.World;
			router = world.WorldActor.TraitOrDefault<CoTVisibilityRouter>();
			lastCell = world.Map.CellContaining(self.CenterPosition);
			haveLastCell = true;
			intervalCounter = 0;
			ticksSinceLastSend = 0;
			initialized = true;
			wasEmitting = false;

			if (IsTraitDisabled || IsTraitPaused)
				return;

			if (!TryGetLatLon(self, out var lat, out var lon))
				return;

			if (router != null)
			{
				if (router.ShouldEmit(self, CoTDomain.Vessel, out var overrideType, out var overrideMilsym))
				{
					var type = string.IsNullOrEmpty(overrideType) ? ResolveType(self) : overrideType;
					SendEvent(self, lat, lon, type, ResolveCallsign(self), info.StaleSeconds, "spawn", null, overrideMilsym);
					wasEmitting = true;
				}
				else
					wasEmitting = false;
			}
			else
				SendEvent(self, lat, lon, ResolveType(self), ResolveCallsign(self), info.StaleSeconds, "spawn");
		}

		void INotifyDamageStateChanged.DamageStateChanged(Actor self, AttackInfo e)
		{
			if (!TryGetLatLon(self, out var lat, out var lon))
				return;
			if (router != null)
			{
				if (router.ShouldEmit(self, CoTDomain.Vessel, out var overrideType, out var overrideMilsym))
				{
					var type = string.IsNullOrEmpty(overrideType) ? ResolveType(self) : overrideType;
					SendEvent(self, lat, lon, type, ResolveCallsign(self), info.StaleSeconds, "damage", null, overrideMilsym);
					wasEmitting = true;
				}
			}
			else
				SendEvent(self, lat, lon, ResolveType(self), ResolveCallsign(self), info.StaleSeconds, "damage");
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			// Stop future heartbeats after sending final event
			heartbeatsDisabled = true;
			if (!TryGetLatLon(self, out var lat, out var lon))
				return;

			// Use explicit 'Dead' mapping for killed events, but only if currently allowed to emit
			if (router != null)
			{
				if (router.ShouldEmit(self, CoTDomain.Vessel, out var overrideType, out var overrideMilsym))
				{
					var type = string.IsNullOrEmpty(overrideType) ? ResolveType(self) : overrideType;
					SendEvent(self, lat, lon, type, ResolveCallsign(self), info.StaleSeconds, "killed", "Dead", overrideMilsym);
				}
			}
			else
				SendEvent(self, lat, lon, ResolveType(self), ResolveCallsign(self), info.StaleSeconds, "killed", "Dead");
		}

		void ITick.Tick(Actor self)
		{
			if (heartbeatsDisabled)
				return;

			var world = self.World;
			if (IsTraitDisabled || IsTraitPaused || world.Paused)
				return;

			intervalCounter++;
			if (intervalCounter < Math.Max(1, info.UpdateIntervalTicks))
				return;

			intervalCounter = 0;
			ticksSinceLastSend += info.UpdateIntervalTicks;

			var cell = world.Map.CellContaining(self.CenterPosition);
			var moved = !haveLastCell || cell != lastCell;
			var dueToTime = ticksSinceLastSend >= Math.Max(1, info.MaxIntervalTicks);

			if (!moved && !dueToTime)
			{
				lastCell = cell;
				haveLastCell = true;
				return;
			}

			if (!TryGetLatLon(self, out var lat, out var lon))
			{
				lastCell = cell;
				haveLastCell = true;
				return;
			}

			if (router != null)
			{
				if (router.ShouldEmit(self, CoTDomain.Vessel, out var overrideType, out var overrideMilsym))
				{
					var type = string.IsNullOrEmpty(overrideType) ? ResolveType(self) : overrideType;
					SendEvent(self, lat, lon, type, ResolveCallsign(self), info.StaleSeconds, moved ? "heartbeat+move" : "heartbeat", null, overrideMilsym);
					wasEmitting = true;
				}
				else
				{
					if (wasEmitting)
						SendEvent(self, lat, lon, ResolveType(self), ResolveCallsign(self), router.StaleSecondsOnLoss, "stale");
					wasEmitting = false;
				}
			}
			else
			{
				SendEvent(self, lat, lon, ResolveType(self), ResolveCallsign(self), info.StaleSeconds, moved ? "heartbeat+move" : "heartbeat");
			}

			// Reset schedule
			ticksSinceLastSend = 0;
			lastCell = cell;
			haveLastCell = true;
		}

		protected override void TraitDisabled(Actor self)
		{
			// Send final update so TAKX can mark this stale after StaleSeconds
			if (heartbeatsDisabled || !initialized)
				return;
			if (!TryGetLatLon(self, out var lat, out var lon))
				return;
			Log.Write("cot", "ship-emitter disabled; sending stale update");
			SendEvent(self, lat, lon, ResolveType(self), ResolveCallsign(self), info.StaleSeconds, "disabled");
		}

		protected override void TraitEnabled(Actor self)
		{
			// Immediately refresh on re-enable
			if (heartbeatsDisabled || !initialized)
				return;
			if (!TryGetLatLon(self, out var lat, out var lon))
				return;
			Log.Write("cot", "ship-emitter enabled; refreshing marker");
			SendEvent(self, lat, lon, ResolveType(self), ResolveCallsign(self), info.StaleSeconds, "enabled");
			ticksSinceLastSend = 0;
		}

		protected override void TraitResumed(Actor self)
		{
			// Pause condition cleared; refresh marker
			if (heartbeatsDisabled || !initialized)
				return;
			if (!TryGetLatLon(self, out var lat, out var lon))
				return;
			Log.Write("cot", "ship-emitter resumed; refreshing marker");
			SendEvent(self, lat, lon, ResolveType(self), ResolveCallsign(self), info.StaleSeconds, "resumed");
			ticksSinceLastSend = 0;
		}

		protected override void TraitPaused(Actor self)
		{
			// Suppress heartbeats while paused; optionally send final stale event so it expires if pause is long.
			if (heartbeatsDisabled || !initialized)
				return;
			if (!TryGetLatLon(self, out var lat, out var lon))
				return;
			Log.Write("cot", "ship-emitter paused; sending stale update");
			SendEvent(self, lat, lon, ResolveType(self), ResolveCallsign(self), info.StaleSeconds, "paused");
		}

		string ResolveCallsign(Actor self)
		{
			if (TryGetValueAnyCase(info.ActorCallsigns, self.Info.Name, out var cs))
			{
				cs = Clean(cs);
				if (!string.IsNullOrEmpty(cs))
					return cs;
			}

			return Clean(info.Callsign);
		}

		string ResolveType(Actor self)
		{
			// Prefer damage-state specific mapping when configured
			if (TryGetValueAnyCase(info.ActorDamageSymbols, self.Info.Name, out var stateMap) && stateMap != null)
			{
				var h = self.TraitOrDefault<Health>();
				var stateKey = h != null ? h.DamageState.ToString() : "Undamaged";
				if (TryGetValueAnyCase(stateMap, stateKey, out var st) && !string.IsNullOrEmpty(st))
					return Clean(st);

				// Optional default inside the state map
				if (TryGetValueAnyCase(stateMap, "Default", out st) && !string.IsNullOrEmpty(st))
					return Clean(st);
			}

			// Fall back to static per-actor symbol
			if (TryGetValueAnyCase(info.ActorSymbols, self.Info.Name, out var t) && !string.IsNullOrEmpty(t))
				return Clean(t);

			// Global default
			return Clean(info.CotType);
		}

		string ResolveMilsymId(Actor self, string stateOverride = null)
		{
			if (TryGetValueAnyCase(info.ActorDamageMilsymIds, self.Info.Name, out var stateMap) && stateMap != null)
			{
				var h = self.TraitOrDefault<Health>();
				var stateKey = !string.IsNullOrEmpty(stateOverride) ? stateOverride : (h != null ? h.DamageState.ToString() : "Undamaged");
				if (TryGetValueAnyCase(stateMap, stateKey, out var st) && !string.IsNullOrEmpty(st))
					return Clean(st);

				// Optional default inside the state map
				if (TryGetValueAnyCase(stateMap, "Default", out st) && !string.IsNullOrEmpty(st))
					return Clean(st);
			}

			if (TryGetValueAnyCase(info.ActorMilsymIds, self.Info.Name, out var t) && !string.IsNullOrEmpty(t))
				return Clean(t);

			return Clean(info.MilsymId);
		}

		static bool TryGetLatLon(Actor self, out double lat, out double lon)
		{
			var world = self.World;
			var cell = world.Map.CellContaining(self.CenterPosition);
			if (!world.Map.TryCellToLatLon(cell, out lat, out lon))
			{
				Log.Write("cot", "skip cot no lat/lon (map not georef?)");
				return false;
			}

			return true;
		}

		void SendEvent(
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
			uid ??= $"OpenRA-AID-{self.ActorID}";

			var now = DateTime.UtcNow;
			var start = now;
			var stale = now.AddSeconds(Math.Max(1, staleSeconds));
			var milsymId = !string.IsNullOrEmpty(milsymOverride) ? milsymOverride : ResolveMilsymId(self, stateOverride);

			// Derive telemetry from last sent position/time
			var speedMps = 0.0;
			var courseDeg = 0.0;
			if (lastLat.HasValue && lastLon.HasValue && lastSendUtc.HasValue)
			{
				var dt = Math.Max(0.001, (now - lastSendUtc.Value).TotalSeconds);
				var dist = HaversineMeters(lastLat.Value, lastLon.Value, lat, lon);
				speedMps = dist / dt;
				courseDeg = InitialBearingDeg(lastLat.Value, lastLon.Value, lat, lon);
			}

			// Determine submarine depth and HAE output
			var isSub = IsSubmarine(self);
			var depthMeters = (int?)null;
			var haeOut = info.Hae;
			if (isSub && info.IncludeDepth && string.Equals(info.DepthMode, "Fixed", StringComparison.OrdinalIgnoreCase))
			{
				var d = ResolveDepthMeters(self);
				if (d > 0)
				{
					depthMeters = d;
					if (info.UseDepthAsNegativeHae)
						haeOut = -d;
				}
			}

			double? speedOpt = info.IncludeTrack ? speedMps : null;
			double? courseOpt = info.IncludeTrack ? courseDeg : null;

			var cot = BuildCotXml(uid, lat, lon, haeOut, info.Ce, info.Le, type, callsign, milsymId, start, stale, speedOpt, courseOpt, depthMeters);

			try
			{
				var data = Encoding.UTF8.GetBytes(cot);
				CotSvc.EnsureInitializedFrom(info.UdpHost, info.UdpPort);
				CotSvc.Enqueue(data);
				Log.Write("cot", string.Format(CultureInfo.InvariantCulture,
					"send {0} actor={1} lat={2} lon={3} speed={4:0.#}mps course={5:000} depth={6}m target={7} bytes={8}",
					reason,
					self.Info.Name,
					lat.ToString("0.########", CultureInfo.InvariantCulture),
					lon.ToString("0.########", CultureInfo.InvariantCulture),
					speedMps,
					(int)Math.Round(courseDeg) % 360,
					depthMeters.HasValue ? depthMeters.Value.ToString(CultureInfo.InvariantCulture) : "-",
					endpoint, data.Length));
				Log.Write("cot", "payload " + cot);
			}
			catch (Exception e)
			{
				// Do not disrupt gameplay
				Log.Write("cot", e);
			}

			// Update telemetry state after send attempt
			lastLat = lat;
			lastLon = lon;
			lastSendUtc = now;
		}

		string BuildCotXml(
			string uid, double lat, double lon, double hae, double ce, double le,
			string type, string callsign, string milsymId, DateTime start, DateTime stale,
			double? speedMps, double? courseDeg, int? depthMeters)
		{
			var nowStr = start.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
			var startStr = nowStr;
			var staleStr = stale.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
			var latStr = lat.ToString("0.########", CultureInfo.InvariantCulture);
			var lonStr = lon.ToString("0.########", CultureInfo.InvariantCulture);
			var haeStr = hae.ToString("0.###", CultureInfo.InvariantCulture);
			var ceStr = ce.ToString("0.###", CultureInfo.InvariantCulture);
			var leStr = le.ToString("0.###", CultureInfo.InvariantCulture);

			// Sanitize values coming from YAML to avoid embedded quotes like "\"\""
			var typeStr = Clean(type);
			var callsignStr = Clean(callsign);
			var milsymStr = Clean(milsymId);

			var sb = new StringBuilder();
			sb.Append("<event version=\"2.0\" ");
			sb.Append(CultureInfo.InvariantCulture, $"uid=\"{uid}\" ");
			sb.Append(CultureInfo.InvariantCulture, $"type=\"{typeStr}\" ");
			sb.Append(CultureInfo.InvariantCulture, $"time=\"{nowStr}\" start=\"{startStr}\" stale=\"{staleStr}\" how=\"m-g\">");
			sb.Append(CultureInfo.InvariantCulture, $"<point lat=\"{latStr}\" lon=\"{lonStr}\" hae=\"{haeStr}\" ce=\"{ceStr}\" le=\"{leStr}\"/>");
			sb.Append("<detail>");
			if (info.IncludeMilsymDetail && !string.IsNullOrEmpty(milsymStr))
				sb.Append(CultureInfo.InvariantCulture, $"<__milsym id=\"{SecurityElementEscape(milsymStr)}\"/>");
			if (info.IncludeColor)
				sb.Append(CultureInfo.InvariantCulture, $"<color argb=\"{info.ColorArgb}\" value=\"{info.ColorValue}\"/>");
			if (info.IncludeLink)
			{
				var parentCs = Clean(info.LinkParentCallsign ?? string.Empty);
				var relation = Clean(string.IsNullOrEmpty(info.LinkRelation) ? "p-p" : info.LinkRelation);
				sb.Append("<link ");
				sb.Append(CultureInfo.InvariantCulture, $"parent_callsign=\"{SecurityElementEscape(parentCs)}\" ");
				sb.Append(CultureInfo.InvariantCulture, $"production_time=\"{startStr}\" ");
				sb.Append(CultureInfo.InvariantCulture, $"relation=\"{SecurityElementEscape(relation)}\" ");
				sb.Append(CultureInfo.InvariantCulture, $"type=\"{SecurityElementEscape(typeStr)}\" ");
				sb.Append(CultureInfo.InvariantCulture, $"uid=\"{SecurityElementEscape(uid)}\"/>");
			}

			sb.Append(CultureInfo.InvariantCulture, $"<contact callsign=\"{SecurityElementEscape(callsignStr)}\"/>");
			if (info.IncludeTrack && speedMps.HasValue && courseDeg.HasValue)
				sb.Append(CultureInfo.InvariantCulture, $"<track speed=\"{Math.Round(speedMps.Value)}\" course=\"{(int)Math.Round(courseDeg.Value) % 360:000}\"/>");
			if (info.IncludeDepth && depthMeters.HasValue)
				sb.Append(CultureInfo.InvariantCulture, $"<depth value=\"{depthMeters.Value}\" unit=\"m\"/>");
			if (info.IncludeArchive)
				sb.Append("<archive/>");
			sb.Append("</detail>");
			sb.Append("</event>");
			return sb.ToString();
		}

		static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
		{
			const double R = 6371000.0; // meters
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
			if (TryGetValueAnyCase(info.ActorIsSubmarine, self.Info.Name, out var isSub) && isSub)
				return true;
			return false;
		}

		int ResolveDepthMeters(Actor self)
		{
			if (TryGetValueAnyCase(info.ActorDepthMeters, self.Info.Name, out var d) && d > 0)
				return d;
			return (int)Math.Round(info.DepthFixedMeters);
		}

		static string SecurityElementEscape(string s)
		{
			if (string.IsNullOrEmpty(s)) return string.Empty;
			return s.Replace("&", "&amp;").Replace("\"", "&quot;").Replace("'", "&apos;").Replace("<", "&lt;").Replace(">", "&gt;");
		}

		static string Clean(string s)
		{
			if (string.IsNullOrEmpty(s))
				return string.Empty;
			var t = s.Trim();

			// Trim a single leading/trailing quote pair if present
			if (t.Length >= 2 && ((t[0] == '"' && t[^1] == '"') || (t[0] == '\'' && t[^1] == '\'')))
				return t[1..^1];
			return t;
		}

		static bool TryGetValueAnyCase<T>(Dictionary<string, T> dict, string key, out T value)
		{
			value = default;
			if (dict == null || key == null)
				return false;
			if (dict.TryGetValue(key, out value))
				return true;
			var up = key.ToUpperInvariant();
			if (!ReferenceEquals(up, key) && dict.TryGetValue(up, out value))
				return true;
			var lo = key.ToLowerInvariant();
			if (!ReferenceEquals(lo, key) && dict.TryGetValue(lo, out value))
				return true;
			foreach (var kv in dict)
				if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
				{
					value = kv.Value;
					return true;
				}

			return false;
		}
	}
}
