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

namespace OpenRA.Mods.Common.Traits
{
	public abstract class CoTEmitterInfoBase : PausableConditionalTraitInfo
	{
		[Desc("UDP target host or IP.")]
		public readonly string UdpHost = "127.0.0.1";

		[Desc("UDP target port.")]
		public readonly int UdpPort = 4242;

		[Desc("Default device callsign to include in CoT detail. Can be overridden per-actor via ActorCallsigns.")]
		public string Callsign = "OpenRA-Unit";

		[Desc("Default CoT type (symbol id). Can be overridden per-actor via ActorSymbols.")]
		public readonly string CotType = "a-f-G-U-C";

		[Desc("Reported height above ellipsoid (meters).")]
		public readonly double Hae = 0.0;

		[Desc("Circular error (meters).")]
		public double Ce = 25.0;

		[Desc("Linear error (meters).")]
		public double Le = 25.0;

		[Desc("Seconds after event when the message should be considered stale.")]
		public readonly int StaleSeconds = 120;

		[Desc("Number of ticks between heartbeat checks and potential sends.")]
		public int UpdateIntervalTicks = 25;

		[Desc("Maximum ticks between heartbeat sends; ensures updates even when stationary.")]
		public int MaxIntervalTicks = 250;

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

		[Desc("Include <archive/> element in CoT detail.")]
		public readonly bool IncludeArchive = true;
	}

	public abstract class CoTEmitterBase<TInfo> : PausableConditionalTrait<TInfo>, INotifyAddedToWorld, INotifyDamageStateChanged, INotifyKilled, ITick
		where TInfo : CoTEmitterInfoBase
	{
		protected readonly IPEndPoint Endpoint;
		bool heartbeatsDisabled;
		bool initialized;
		int intervalCounter;
		int ticksSinceLastSend;
		bool haveLastCell;
		CPos lastCell;
		string uid;
		CoTVisibilityRouter router;
		bool wasEmitting;

		protected CoTEmitterBase(ActorInitializer init, TInfo info)
			: base(info)
		{
			Endpoint = new IPEndPoint(ParseAddress(info.UdpHost), info.UdpPort);
			CotOutputService.EnsureInitializedFrom(info.UdpHost, info.UdpPort);
			Log.Write("cot", string.Format(CultureInfo.InvariantCulture,
				"{0} init endpoint={1} callsign={2} type={3} updateTicks={4} maxTicks={5}",
				EmitterName, Endpoint, info.Callsign, info.CotType, info.UpdateIntervalTicks, info.MaxIntervalTicks));
		}

		protected abstract CoTDomain Domain { get; }

		protected virtual string EmitterName => GetType().Name;

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

			OnAddedToWorld(self);

			if (IsTraitDisabled || IsTraitPaused)
				return;

			if (!TryGetLatLon(self, out var lat, out var lon))
				return;

			EmitWithRouter(self, lat, lon, Info.StaleSeconds, "spawn");
		}

		protected virtual void OnAddedToWorld(Actor self) { }

		void INotifyDamageStateChanged.DamageStateChanged(Actor self, AttackInfo e)
		{
			if (!TryGetLatLon(self, out var lat, out var lon))
				return;
			EmitWithRouter(self, lat, lon, Info.StaleSeconds, "damage");
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			heartbeatsDisabled = true;
			if (!TryGetLatLon(self, out var lat, out var lon))
				return;

			if (router != null)
			{
				if (router.ShouldEmit(self, Domain, out var overrideType, out var overrideMilsym))
				{
					var type = string.IsNullOrEmpty(overrideType) ? ResolveType(self) : overrideType;
					SendEvent(self, lat, lon, type, ResolveCallsign(self), Info.StaleSeconds, "killed", "Dead", overrideMilsym);
				}
			}
			else
				SendEvent(self, lat, lon, ResolveType(self), ResolveCallsign(self), Info.StaleSeconds, "killed", "Dead");
		}

		void ITick.Tick(Actor self)
		{
			if (heartbeatsDisabled)
				return;

			var world = self.World;
			if (IsTraitDisabled || IsTraitPaused || world.Paused)
				return;

			intervalCounter++;
			if (intervalCounter < Math.Max(1, Info.UpdateIntervalTicks))
				return;

			intervalCounter = 0;
			ticksSinceLastSend += Info.UpdateIntervalTicks;

			var cell = world.Map.CellContaining(self.CenterPosition);
			var moved = !haveLastCell || cell != lastCell;
			var dueToTime = ticksSinceLastSend >= Math.Max(1, Info.MaxIntervalTicks);

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
				if (router.ShouldEmit(self, Domain, out var overrideType, out var overrideMilsym))
				{
					var type = string.IsNullOrEmpty(overrideType) ? ResolveType(self) : overrideType;
					SendEvent(self, lat, lon, type, ResolveCallsign(self), Info.StaleSeconds, moved ? "heartbeat+move" : "heartbeat", null, overrideMilsym);
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
				SendEvent(self, lat, lon, ResolveType(self), ResolveCallsign(self), Info.StaleSeconds, moved ? "heartbeat+move" : "heartbeat");
			}

			ticksSinceLastSend = 0;
			lastCell = cell;
			haveLastCell = true;
		}

		protected override void TraitDisabled(Actor self)
		{
			if (heartbeatsDisabled || !initialized)
				return;
			if (!TryGetLatLon(self, out var lat, out var lon))
				return;
			Log.Write("cot", $"{EmitterName} disabled; sending stale update");
			SendEvent(self, lat, lon, ResolveType(self), ResolveCallsign(self), Info.StaleSeconds, "disabled");
		}

		protected override void TraitEnabled(Actor self)
		{
			if (heartbeatsDisabled || !initialized)
				return;
			if (!TryGetLatLon(self, out var lat, out var lon))
				return;
			Log.Write("cot", $"{EmitterName} enabled; refreshing marker");
			SendEvent(self, lat, lon, ResolveType(self), ResolveCallsign(self), Info.StaleSeconds, "enabled");
			ticksSinceLastSend = 0;
		}

		protected override void TraitResumed(Actor self)
		{
			if (heartbeatsDisabled || !initialized)
				return;
			if (!TryGetLatLon(self, out var lat, out var lon))
				return;
			Log.Write("cot", $"{EmitterName} resumed; refreshing marker");
			SendEvent(self, lat, lon, ResolveType(self), ResolveCallsign(self), Info.StaleSeconds, "resumed");
			ticksSinceLastSend = 0;
		}

		protected override void TraitPaused(Actor self)
		{
			if (heartbeatsDisabled || !initialized)
				return;
			if (!TryGetLatLon(self, out var lat, out var lon))
				return;
			Log.Write("cot", $"{EmitterName} paused; sending stale update");
			SendEvent(self, lat, lon, ResolveType(self), ResolveCallsign(self), Info.StaleSeconds, "paused");
		}

		void EmitWithRouter(Actor self, double lat, double lon, int staleSeconds, string reason)
		{
			if (router != null)
			{
				if (router.ShouldEmit(self, Domain, out var overrideType, out var overrideMilsym))
				{
					var type = string.IsNullOrEmpty(overrideType) ? ResolveType(self) : overrideType;
					SendEvent(self, lat, lon, type, ResolveCallsign(self), staleSeconds, reason, null, overrideMilsym);
					wasEmitting = true;
				}
				else
					wasEmitting = false;
			}
			else
				SendEvent(self, lat, lon, ResolveType(self), ResolveCallsign(self), staleSeconds, reason);
		}

		protected virtual void SendEvent(
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
			var stale = now.AddSeconds(Math.Max(1, staleSeconds));
			var milsymId = !string.IsNullOrEmpty(milsymOverride) ? milsymOverride : ResolveMilsymId(self, stateOverride);
			var cot = BuildCotXml(uid, lat, lon, Info.Hae, Info.Ce, Info.Le, type, callsign, milsymId, now, stale);

			try
			{
				var data = Encoding.UTF8.GetBytes(cot);
				CotOutputService.EnsureInitializedFrom(Info.UdpHost, Info.UdpPort);
				CotOutputService.Enqueue(data);
				Log.Write("cot", string.Format(CultureInfo.InvariantCulture,
					"send {0} actor={1} lat={2} lon={3} target={4} bytes={5}",
					reason,
					self.Info.Name,
					lat.ToString("0.########", CultureInfo.InvariantCulture),
					lon.ToString("0.########", CultureInfo.InvariantCulture),
					Endpoint, data.Length));
			}
			catch (Exception e)
			{
				Log.Write("cot", e);
			}
		}

		protected virtual string BuildCotXml(
			string uid, double lat, double lon, double hae, double ce, double le,
			string type, string callsign, string milsymId, DateTime start, DateTime stale)
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
			AppendExtraDetailElements(sb, start);
			if (Info.IncludeArchive)
				sb.Append("<archive/>");
			sb.Append("</detail>");
			sb.Append("</event>");
			return sb.ToString();
		}

		protected virtual void AppendExtraDetailElements(StringBuilder sb, DateTime start) { }

		// Public static helpers for testability
		public static string SecurityElementEscape(string s)
		{
			if (string.IsNullOrEmpty(s)) return string.Empty;
			return s.Replace("&", "&amp;").Replace("\"", "&quot;").Replace("'", "&apos;").Replace("<", "&lt;").Replace(">", "&gt;");
		}

		public static string Clean(string s)
		{
			if (string.IsNullOrEmpty(s))
				return string.Empty;
			var t = s.Trim();

			if (t.Length >= 2 && ((t[0] == '"' && t[^1] == '"') || (t[0] == '\'' && t[^1] == '\'')))
				return t[1..^1];
			return t;
		}

		public static bool TryGetValueAnyCase<T>(Dictionary<string, T> dict, string key, out T value)
		{
			value = default;
			if (dict == null || key == null)
				return false;
			if (dict.TryGetValue(key, out value))
				return true;
			foreach (var kv in dict)
				if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
				{
					value = kv.Value;
					return true;
				}

			return false;
		}

		protected string ResolveCallsign(Actor self)
		{
			if (TryGetValueAnyCase(Info.ActorCallsigns, self.Info.Name, out var cs))
			{
				cs = Clean(cs);
				if (!string.IsNullOrEmpty(cs))
					return cs;
			}

			return Clean(Info.Callsign);
		}

		protected string ResolveType(Actor self)
		{
			if (TryGetValueAnyCase(Info.ActorDamageSymbols, self.Info.Name, out var stateMap) && stateMap != null)
			{
				var h = self.TraitOrDefault<Health>();
				var stateKey = h != null ? h.DamageState.ToString() : "Undamaged";
				if (TryGetValueAnyCase(stateMap, stateKey, out var st) && !string.IsNullOrEmpty(st))
					return Clean(st);

				if (TryGetValueAnyCase(stateMap, "Default", out st) && !string.IsNullOrEmpty(st))
					return Clean(st);
			}

			if (TryGetValueAnyCase(Info.ActorSymbols, self.Info.Name, out var t) && !string.IsNullOrEmpty(t))
				return Clean(t);

			return Clean(Info.CotType);
		}

		protected string ResolveMilsymId(Actor self, string stateOverride = null)
		{
			if (TryGetValueAnyCase(Info.ActorDamageMilsymIds, self.Info.Name, out var stateMap) && stateMap != null)
			{
				var h = self.TraitOrDefault<Health>();
				var stateKey = !string.IsNullOrEmpty(stateOverride) ? stateOverride : (h != null ? h.DamageState.ToString() : "Undamaged");
				if (TryGetValueAnyCase(stateMap, stateKey, out var st) && !string.IsNullOrEmpty(st))
					return Clean(st);

				if (TryGetValueAnyCase(stateMap, "Default", out st) && !string.IsNullOrEmpty(st))
					return Clean(st);
			}

			if (TryGetValueAnyCase(Info.ActorMilsymIds, self.Info.Name, out var t) && !string.IsNullOrEmpty(t))
				return Clean(t);

			return Clean(Info.MilsymId);
		}

		protected static bool TryGetLatLon(Actor self, out double lat, out double lon)
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

		protected static IPAddress ParseAddress(string s)
		{
			if (IPAddress.TryParse(s, out var ip))
				return ip;
			var addresses = Dns.GetHostAddresses(s);
			return addresses.Length > 0 ? addresses[0] : IPAddress.Loopback;
		}
	}
}
