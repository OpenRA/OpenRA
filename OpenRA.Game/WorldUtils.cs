#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
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
		public static IEnumerable<Actor> FindUnitsAtMouse(this World world, int2 mouseLocation)
		{
			var loc = Game.viewport.ViewToWorldPx(mouseLocation);
			return FindUnits(world, loc, loc).Where(a => world.LocalShroud.IsVisible(a));
		}

		public static IEnumerable<Actor> FindUnits(this World world, int2 a, int2 b)
		{
			var u = float2.Min(a, b).ToInt2();
			var v = float2.Max(a, b).ToInt2();
			return world.WorldActor.Trait<SpatialBins>().ActorsInBox(u,v);
		}

		public static Actor ClosestTo( this IEnumerable<Actor> actors, int2 px )
		{
			return actors.OrderBy( a => (a.CenterLocation - px).LengthSquared ).FirstOrDefault();
		}

		public static IEnumerable<Actor> FindUnitsInCircle(this World world, int2 a, int r)
		{
			using (new PerfSample("FindUnitsInCircle"))
			{
				var min = a - new int2(r, r);
				var max = a + new int2(r, r);

				var actors = world.FindUnits(min, max);

				var rect = new Rectangle(min.X, min.Y, max.X - min.X, max.Y - min.Y);

				var inBox = actors.Where(x => x.ExtendedBounds.Value.IntersectsWith(rect));

				return inBox.Where(x => (x.CenterLocation - a).LengthSquared < r * r);
			}
		}

		public static IEnumerable<int2> FindTilesInCircle(this World world, int2 a, int r)
		{
			var min = world.ClampToWorld(a - new int2(r, r));
			var max = world.ClampToWorld(a + new int2(r, r));
			for (var j = min.Y; j <= max.Y; j++)
				for (var i = min.X; i <= max.X; i++)
					if (r * r >= (new int2(i, j) - a).LengthSquared)
						yield return new int2(i, j);
		}

		public static string GetTerrainType(this World world, int2 cell)
		{
			var custom = world.Map.CustomTerrain[cell.X, cell.Y];
			return custom != null ? custom : world.TileSet.GetTerrainType(world.Map.MapTiles.Value[cell.X, cell.Y]);
		}

		public static TerrainTypeInfo GetTerrainInfo(this World world, int2 cell)
		{
			return world.TileSet.Terrain[world.GetTerrainType(cell)];
		}

		public static int2 ClampToWorld( this World world, int2 xy )
		{
			var r = world.Map.Bounds;
			return xy.Clamp(new Rectangle(r.X,r.Y,r.Width-1, r.Height-1));
		}

		public static int2 ChooseRandomEdgeCell(this World w)
		{
			var isX = w.SharedRandom.Next(2) == 0;
			var edge = w.SharedRandom.Next(2) == 0;

			return new int2(
				isX ? w.SharedRandom.Next(w.Map.Bounds.Left, w.Map.Bounds.Right)
					: (edge ? w.Map.Bounds.Left : w.Map.Bounds.Right),
				!isX ? w.SharedRandom.Next(w.Map.Bounds.Top, w.Map.Bounds.Bottom)
					: (edge ? w.Map.Bounds.Top : w.Map.Bounds.Bottom));
		}

		public static int2 ChooseRandomCell(this World w, Thirdparty.Random r)
		{
			return new int2(
				r.Next(w.Map.Bounds.Left, w.Map.Bounds.Right),
				r.Next(w.Map.Bounds.Top, w.Map.Bounds.Bottom));
		}

		public static float Gauss1D(this Thirdparty.Random r, int samples)
		{
			return Exts.MakeArray(samples, _ => (float)r.NextDouble() * 2 - 1f)
				.Sum() / samples;
		}

		// Returns a random offset in the range [-1..1,-1..1] with a separable
		// Gauss distribution with 'samples' values taken for each axis
		public static float2 Gauss2D(this Thirdparty.Random r, int samples)
		{
			return new float2(Gauss1D(r, samples), Gauss1D(r, samples));
		}

		public static bool HasVoice(this Actor a)
		{
			var selectable = a.Info.Traits.GetOrDefault<SelectableInfo>();
			return selectable != null && selectable.Voice != null;
		}

		public static VoiceInfo GetVoice(this Actor a)
		{
			var selectable = a.Info.Traits.GetOrDefault<SelectableInfo>();
			if (selectable == null) return null;
			var v = selectable.Voice;
			return (v == null) ? null : Rules.Voices[v];
		}

		public static void PlayVoiceForOrders(this World w, Order[] orders)
		{
			// Find an actor with a phrase to say
			foreach (var o in orders)
			{
				if (o.Subject.Destroyed) continue;
				foreach (var v in o.Subject.TraitsImplementing<IOrderVoice>())
					if (Sound.PlayVoice(v.VoicePhraseForOrder(o.Subject, o),
						o.Subject, o.Subject.Owner.Country.Race))
						return;
			}
		}

		public static void DoTimed<T>(this IEnumerable<T> e, Action<T> a, string text, double time)
		{
			var sw = new Stopwatch();

			e.Do(x =>
			{
				var t = sw.ElapsedTime();
				a(x);
				var dt = sw.ElapsedTime() - t;
				if (dt > time)
					Log.Write("perf", text, x, dt * 1000, Game.LocalTick);
			});
		}

		public static bool AreMutualAllies( Player a, Player b )
		{
			return a.Stances[b] == Stance.Ally &&
				b.Stances[a] == Stance.Ally;
		}
	}
}
