#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.GameRules;
using OpenRA.Support;
using OpenRA.Traits;
using System;

namespace OpenRA
{
	public static class WorldUtils
	{
		public static IEnumerable<Actor> FindUnitsAtMouse(this World world, int2 mouseLocation)
		{
			var loc = mouseLocation + Game.viewport.Location;
			return FindUnits(world, loc, loc).Where(a => a.IsVisible(world.LocalPlayer));
		}

		public static IEnumerable<Actor> FindUnits(this World world, float2 a, float2 b)
		{
			var u = float2.Min(a, b).ToInt2();
			var v = float2.Max(a, b).ToInt2();
			return world.WorldActor.Trait<SpatialBins>().ActorsInBox(u,v);
		}

		public static IEnumerable<Actor> FindUnitsInCircle(this World world, float2 a, float r)
		{
			using (new PerfSample("FindUnitsInCircle"))
			{
				var min = a - new float2(r, r);
				var max = a + new float2(r, r);

				var actors = world.FindUnits(min, max);

				var rect = new RectangleF(min.X, min.Y, max.X - min.X, max.Y - min.Y);

				var inBox = actors.Where(x => x.GetBounds(false).IntersectsWith(rect));

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
			return custom != null ? custom : world.TileSet.GetTerrainType(world.Map.MapTiles[cell.X, cell.Y]);
		}
		
		public static TerrainTypeInfo GetTerrainInfo(this World world, int2 cell)
		{
			return world.TileSet.Terrain[world.GetTerrainType(cell)];
		}
		
		public static bool IsVisible(this Actor a, Player byPlayer)			/* must never be relied on in synced code! */
		{
			if (byPlayer == null) return true; // Observer
			if (a.World.LocalPlayer != null && a.World.LocalPlayer.Shroud.Disabled)
				return true;

			var shroud = a.World.WorldActor.Trait<Shroud>();
			if (!Shroud.GetVisOrigins(a).Any(o => a.World.Map.IsInMap(o) && shroud.exploredCells[o.X, o.Y]))		// covered by shroud
				return false;

			if (a.TraitsImplementing<IVisibilityModifier>().Any(t => !t.IsVisible(a, byPlayer)))
				return false;

			return true;
		}

		public static int2 ClampToWorld( this World world, int2 xy )
		{
			return xy.Clamp(world.Map.Bounds);			
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

		public static IEnumerable<CountryInfo> GetCountries(this World w)
		{
			return w.WorldActor.Info.Traits.WithInterface<CountryInfo>();
		}

		public static float Gauss1D(this Thirdparty.Random r, int samples)
		{
			return Graphics.Util.MakeArray(samples, _ => (float)r.NextDouble() * 2 - 1f)
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
			return a.Info.Traits.Contains<SelectableInfo>() && a.Info.Traits.Get<SelectableInfo>().Voice != null;
		}
		
		public static VoiceInfo GetVoice(this Actor a)
		{
			if (!a.Info.Traits.Contains<SelectableInfo>()) return null;
			var v = a.Info.Traits.Get<SelectableInfo>().Voice;
			return (v == null) ? null : Rules.Voices[v];
		}
	}
}
