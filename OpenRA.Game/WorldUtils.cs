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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA
{
	public static class WorldUtils
	{
		public static Actor ClosestTo(this IEnumerable<Actor> actors, Actor a)
		{
			return actors.ClosestTo(a.CenterPosition);
		}

		public static Actor ClosestTo(this IEnumerable<Actor> actors, WPos pos)
		{
			return actors.MinByOrDefault(a => (a.CenterPosition - pos).LengthSquared);
		}

		public static IEnumerable<Actor> FindActorsInCircle(this World world, WPos origin, WRange r)
		{
			using (new PerfSample("FindUnitsInCircle"))
			{
				// Target ranges are calculated in 2D, so ignore height differences
				var vec = new WVec(r, r, WRange.Zero);
				var rSq = r.Range*r.Range;
				return world.ActorMap.ActorsInBox(origin - vec, origin + vec).Where(
					a => (a.CenterPosition - origin).HorizontalLengthSquared <= rSq);
			}
		}

		public static bool HasVoices(this Actor a)
		{
			var selectable = a.Info.Traits.GetOrDefault<SelectableInfo>();
			return selectable != null && selectable.Voice != null;
		}

		public static bool HasVoice(this Actor a, string voice)
		{
			var v = GetVoices(a);
			return v != null && v.Voices.ContainsKey(voice);
		}

		public static SoundInfo GetVoices(this Actor a)
		{
			var selectable = a.Info.Traits.GetOrDefault<SelectableInfo>();
			if (selectable == null) return null;
			var v = selectable.Voice;
			return (v == null) ? null : a.World.Map.Rules.Voices[v.ToLowerInvariant()];
		}

		public static void PlayVoiceForOrders(this World w, Order[] orders)
		{
			// Find an actor with a phrase to say
			foreach (var o in orders)
			{
				if (o == null)
					continue;

				if (o.Subject.Destroyed)
					continue;

				foreach (var v in o.Subject.TraitsImplementing<IOrderVoice>())
					if (Sound.PlayVoice(v.VoicePhraseForOrder(o.Subject, o),
						o.Subject, o.Subject.Owner.Country.Race))
						return;
			}
		}

		public static void DoTimed<T>(this IEnumerable<T> e, Action<T> a, string text)
		{
			// Note - manual enumeration here for performance due to high call volume.
			var longTickThresholdInStopwatchTicks = PerfTimer.LongTickThresholdInStopwatchTicks;
			using (var enumerator = e.GetEnumerator())
			{
				var start = Stopwatch.GetTimestamp();
				while (enumerator.MoveNext())
				{
					a(enumerator.Current);
					var current = Stopwatch.GetTimestamp();
					if (current - start > longTickThresholdInStopwatchTicks)
					{
						PerfTimer.LogLongTick(start, current, text, enumerator.Current);
						start = Stopwatch.GetTimestamp();
					}
					else
					{
						start = current;
					}
				}
			}
		}

		public static bool AreMutualAllies( Player a, Player b )
		{
			return a.Stances[b] == Stance.Ally &&
				b.Stances[a] == Stance.Ally;
		}
	}
}
