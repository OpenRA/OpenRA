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
			return FindUnits(world, loc, loc).Where(a => !world.FogObscures(a));
		}

		public static IEnumerable<Actor> FindUnits(this World world, PPos a, PPos b)
		{
			var u = PPos.Min(a, b);
			var v = PPos.Max(a, b);
			return world.WorldActor.Trait<SpatialBins>().ActorsInBox(u,v);
		}

		public static Actor ClosestTo(this IEnumerable<Actor> actors, PPos px)
		{
			return actors.OrderBy( a => (a.CenterLocation - px).LengthSquared ).FirstOrDefault();
		}

		public static IEnumerable<Actor> FindUnitsInCircle(this World world, PPos a, int r)
		{
			using (new PerfSample("FindUnitsInCircle"))
			{
				var min = a - PVecInt.FromRadius(r);
				var max = a + PVecInt.FromRadius(r);

				var actors = world.FindUnits(min, max);

				var rect = new Rectangle(min.X, min.Y, max.X - min.X, max.Y - min.Y);

				var inBox = actors.Where(x => x.ExtendedBounds.Value.IntersectsWith(rect));

				return inBox.Where(x => (x.CenterLocation - a).LengthSquared < r * r);
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

		const int MaxRange = 50;
		static List<CVec>[] TilesByDistance = InitTilesByDistance(MaxRange);

		public static string GetTerrainType(this World world, CPos cell)
		{
			var custom = world.Map.CustomTerrain[cell.X, cell.Y];
			return custom != null ? custom : world.TileSet.GetTerrainType(world.Map.MapTiles.Value[cell.X, cell.Y]);
		}

		public static TerrainTypeInfo GetTerrainInfo(this World world, CPos cell)
		{
			return world.TileSet.Terrain[world.GetTerrainType(cell)];
		}

		public static CPos ClampToWorld(this World world, CPos xy)
		{
			var r = world.Map.Bounds;
			return xy.Clamp(new Rectangle(r.X,r.Y,r.Width-1, r.Height-1));
		}

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

		public static CPos ChooseRandomCell(this World w, Thirdparty.Random r)
		{
			return new CPos(
				r.Next(w.Map.Bounds.Left, w.Map.Bounds.Right),
				r.Next(w.Map.Bounds.Top, w.Map.Bounds.Bottom));
		}

		public static float Gauss1D(this Thirdparty.Random r, int samples)
		{
			return Exts.MakeArray(samples, _ => r.NextFloat() * 2 - 1f)
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

		public static SoundInfo GetVoice(this Actor a)
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
