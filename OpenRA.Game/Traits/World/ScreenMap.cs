#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Traits
{
	public class ScreenMapInfo : ITraitInfo
	{
		[Desc("Size of partition bins (world pixels)")]
		public readonly int BinSize = 250;

		public object Create(ActorInitializer init) { return new ScreenMap(init.world, this); }
	}

	public class ScreenMap : IWorldLoaded
	{
		ScreenMapInfo info;
		WorldRenderer worldRenderer;
		Cache<Player, Dictionary<FrozenActor, Rectangle>[]> frozen;
		Dictionary<Actor, Rectangle>[] actors;
		int rows, cols;

		public ScreenMap(World world, ScreenMapInfo info)
		{
			this.info = info;
			var ts = Game.modData.Manifest.TileSize;
			cols = world.Map.MapSize.X * ts.Width / info.BinSize + 1;
			rows = world.Map.MapSize.Y * ts.Height / info.BinSize + 1;

			frozen = new Cache<Player, Dictionary<FrozenActor, Rectangle>[]>(InitializeFrozenActors);
			actors = new Dictionary<Actor, Rectangle>[rows * cols];
			for (var j = 0; j < rows; j++)
				for (var i = 0; i < cols; i++)
					actors[j * cols + i] = new Dictionary<Actor, Rectangle>();
		}

		public void WorldLoaded(World w, WorldRenderer wr) { worldRenderer = wr; }

		Dictionary<FrozenActor, Rectangle>[] InitializeFrozenActors(Player p)
		{
			var f = new Dictionary<FrozenActor, Rectangle>[rows * cols];
			for (var j = 0; j < rows; j++)
				for (var i = 0; i < cols; i++)
					f[j * cols + i] = new Dictionary<FrozenActor, Rectangle>();

			return f;
		}

		public void Add(Player viewer, FrozenActor fa)
		{
			var pos = worldRenderer.ScreenPxPosition(fa.CenterPosition);
			var bounds = fa.Bounds;
			bounds.Offset(pos.X, pos.Y);

			var top = Math.Max(0, bounds.Top / info.BinSize);
			var left = Math.Max(0, bounds.Left / info.BinSize);
			var bottom = Math.Min(rows - 1, bounds.Bottom / info.BinSize);
			var right = Math.Min(cols - 1, bounds.Right / info.BinSize);

			for (var j = top; j <= bottom; j++)
				for (var i = left; i <= right; i++)
					frozen[viewer][j*cols + i].Add(fa, bounds);
		}

		public void Remove(Player viewer, FrozenActor fa)
		{
			foreach (var bin in frozen[viewer])
				bin.Remove(fa);
		}

		public void Add(Actor a)
		{
			var pos = worldRenderer.ScreenPxPosition(a.CenterPosition);
			var bounds = a.Bounds.Value;
			bounds.Offset(pos.X, pos.Y);

			var top = Math.Max(0, bounds.Top / info.BinSize);
			var left = Math.Max(0, bounds.Left / info.BinSize);
			var bottom = Math.Min(rows - 1, bounds.Bottom / info.BinSize);
			var right = Math.Min(cols - 1, bounds.Right / info.BinSize);

			for (var j = top; j <= bottom; j++)
				for (var i = left; i <= right; i++)
					actors[j * cols + i].Add(a, bounds);
		}

		public void Remove(Actor a)
		{
			foreach (var bin in actors)
				bin.Remove(a);
		}

		public void Update(Actor a)
		{
			Remove(a);
			Add(a);
		}

		public static readonly IEnumerable<FrozenActor> NoFrozenActors = new FrozenActor[0].AsEnumerable();
		public IEnumerable<FrozenActor> FrozenActorsAt(Player viewer, int2 worldPx)
		{
			if (viewer == null)
				return NoFrozenActors;

			var i = (worldPx.X / info.BinSize).Clamp(0, cols - 1);
			var j = (worldPx.Y / info.BinSize).Clamp(0, rows - 1);
			return frozen[viewer][j*cols + i]
				.Where(kv => kv.Key.IsValid && kv.Value.Contains(worldPx))
				.Select(kv => kv.Key);
		}

		public IEnumerable<FrozenActor> FrozenActorsAt(Player viewer, MouseInput mi)
		{
			return FrozenActorsAt(viewer, worldRenderer.Viewport.ViewToWorldPx(mi.Location));
		}

		public IEnumerable<Actor> ActorsAt(int2 worldPx)
		{
			var i = (worldPx.X / info.BinSize).Clamp(0, cols - 1);
			var j = (worldPx.Y / info.BinSize).Clamp(0, rows - 1);
			return actors[j * cols + i]
				.Where(kv => kv.Key.IsInWorld && kv.Value.Contains(worldPx))
				.Select(kv => kv.Key);
		}

		public IEnumerable<Actor> ActorsAt(MouseInput mi)
		{
			return ActorsAt(worldRenderer.Viewport.ViewToWorldPx(mi.Location));
		}

		public IEnumerable<Actor> ActorsInBox(int2 a, int2 b)
		{
			return ActorsInBox(Rectangle.FromLTRB(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)));
		}

		public IEnumerable<Actor> ActorsInBox(Rectangle r)
		{
			var left = (r.Left / info.BinSize).Clamp(0, cols - 1);
			var right = (r.Right / info.BinSize).Clamp(0, cols - 1);
			var top = (r.Top / info.BinSize).Clamp(0, rows - 1);
			var bottom = (r.Bottom / info.BinSize).Clamp(0, rows - 1);

			var actorsInBox = new List<Actor>();
			for (var j = top; j <= bottom; j++)
				for (var i = left; i <= right; i++)
					actorsInBox.AddRange(actors[j * cols + i]
						.Where(kv => kv.Key.IsInWorld && kv.Value.IntersectsWith(r))
						.Select(kv => kv.Key));

			return actorsInBox.Distinct();
		}

		public IEnumerable<FrozenActor> FrozenActorsInBox(Player p, int2 a, int2 b)
		{
			return FrozenActorsInBox(p, Rectangle.FromLTRB(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)));
		}

		public IEnumerable<FrozenActor> FrozenActorsInBox(Player p, Rectangle r)
		{
			var left = (r.Left / info.BinSize).Clamp(0, cols - 1);
			var right = (r.Right / info.BinSize).Clamp(0, cols - 1);
			var top = (r.Top / info.BinSize).Clamp(0, rows - 1);
			var bottom = (r.Bottom / info.BinSize).Clamp(0, rows - 1);

			var frozenInBox = new List<FrozenActor>();
			for (var j = top; j <= bottom; j++)
				for (var i = left; i <= right; i++)
					frozenInBox.AddRange(frozen[p][j * cols + i]
					                     .Where(kv => kv.Key.IsValid && kv.Value.IntersectsWith(r))
					                     .Select(kv => kv.Key));

			return frozenInBox.Distinct();
		}
	}
}
