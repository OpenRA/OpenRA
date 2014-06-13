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
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.GameRules;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA
{
	public static class WorldUtils
	{
		public static IEnumerable<Actor> FindActorsInBox(this World world, CPos tl, CPos br)
		{
			// TODO: Support diamond boxes for isometric maps?
			return world.FindActorsInBox(tl.TopLeft, br.BottomRight);
		}

		public static IEnumerable<Actor> FindActorsInBox(this World world, WPos tl, WPos br)
		{
			return world.ActorMap.ActorsInBox(tl, br);
		}

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
				return world.FindActorsInBox(origin - vec, origin + vec).Where(
					a => (a.CenterPosition - origin).HorizontalLengthSquared <= rSq);
			}
		}

		public static IEnumerable<CPos> FindTilesInCircle(this World world, CPos a, int r)
		{
			if (r >= TilesByDistance.Length)
				throw new InvalidOperationException("FindTilesInCircle supports queries for only <= {0}".F(MaxRange));

			for(var i = 0; i <= r; i++)
			{
				foreach(var offset in TilesByDistance[i])
				{
					var t = offset + a;
					if (world.Map.Bounds.Contains(t.X, t.Y))
						yield return t;
				}
			}
		}

		static List<CVec>[] InitTilesByDistance(int max)
		{
			var ts = new List<CVec>[max+1];
			for (var i = 0; i < max+1; i++)
				ts[i] = new List<CVec>();

			for (var j = -max; j <= max; j++)
				for (var i = -max; i <= max; i++)
					if (max * max >= i * i + j * j)
						ts[(int)Math.Ceiling(Math.Sqrt(i*i + j*j))].Add(new CVec(i,j));

			return ts;
		}

		public const int MaxRange = 50;
		static List<CVec>[] TilesByDistance = InitTilesByDistance(MaxRange);

		public static CPos ChooseRandomEdgeCell(this World w)
		{
			var isX = w.SharedRandom.Next(2) == 0;
			var edge = w.SharedRandom.Next(2) == 0;

			return new CPos(
				isX ? w.SharedRandom.Next(w.Map.Bounds.Left, w.Map.Bounds.Right)
					: (edge ? w.Map.Bounds.Left : w.Map.Bounds.Right),
				!isX ? w.SharedRandom.Next(w.Map.Bounds.Top, w.Map.Bounds.Bottom)
					: (edge ? w.Map.Bounds.Top : w.Map.Bounds.Bottom));
		}

		public static WRange DistanceToMapEdge(this World w, WPos pos, WVec dir)
		{
			var tl = w.Map.Bounds.TopLeftAsCPos().TopLeft;
			var br = w.Map.Bounds.BottomRightAsCPos().BottomRight;
			var x = dir.X == 0 ? int.MaxValue : ((dir.X < 0 ? tl.X : br.X) - pos.X) / dir.X;
			var y = dir.Y == 0 ? int.MaxValue : ((dir.Y < 0 ? tl.Y : br.Y) - pos.Y) / dir.Y;
			return new WRange(Math.Min(x, y) * dir.Length);
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
