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
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Traits
{
	public class ScreenMapInfo : ITraitInfo
	{
		[Desc("Size of partition bins (world pixels)")]
		public readonly int BinSize = 250;

		public object Create(ActorInitializer init) { return new ScreenMap(init.world, this); }
	}

	public class ScreenMap
	{
		ScreenMapInfo info;
		Cache<Player, List<FrozenActor>[]> frozen;
		List<Actor>[] actors;
		int rows, cols;

		public ScreenMap(World world, ScreenMapInfo info)
		{
			this.info = info;
			cols = world.Map.MapSize.X * Game.CellSize / info.BinSize + 1;
			rows = world.Map.MapSize.Y * Game.CellSize / info.BinSize + 1;

			frozen = new Cache<Player, List<FrozenActor>[]>(InitializeFrozenActors);
			actors = new List<Actor>[rows * cols];
			for (var j = 0; j < rows; j++)
				for (var i = 0; i < cols; i++)
					actors[j * cols + i] = new List<Actor>();
		}

		List<FrozenActor>[] InitializeFrozenActors(Player p)
		{
			var f = new List<FrozenActor>[rows * cols];
			for (var j = 0; j < rows; j++)
				for (var i = 0; i < cols; i++)
					f[j * cols + i] = new List<FrozenActor>();

			return f;
		}

		public void Add(Player viewer, FrozenActor fa)
		{
			var top = Math.Max(0, fa.Bounds.Top / info.BinSize);
			var left = Math.Max(0, fa.Bounds.Left / info.BinSize);
			var bottom = Math.Min(rows - 1, fa.Bounds.Bottom / info.BinSize);
			var right = Math.Min(cols - 1, fa.Bounds.Right / info.BinSize);

			for (var j = top; j <= bottom; j++)
				for (var i = left; i <= right; i++)
					frozen[viewer][j*cols + i].Add(fa);
		}

		public void Remove(Player viewer, FrozenActor fa)
		{
			foreach (var bin in frozen[viewer])
				bin.Remove(fa);
		}

		public void Add(Actor a)
		{
			var b = a.Bounds.Value;
			var top = Math.Max(0, b.Top / info.BinSize);
			var left = Math.Max(0, b.Left / info.BinSize);
			var bottom = Math.Min(rows - 1, b.Bottom / info.BinSize);
			var right = Math.Min(cols - 1, b.Right / info.BinSize);

			for (var j = top; j <= bottom; j++)
				for (var i = left; i <= right; i++)
					actors[j * cols + i].Add(a);
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

		public IEnumerable<FrozenActor> FrozenActorsAt(Player viewer, int2 pxPos)
		{
			var i = (pxPos.X / info.BinSize).Clamp(0, cols - 1);
			var j = (pxPos.Y / info.BinSize).Clamp(0, rows - 1);
			return frozen[viewer][j*cols + i].Where(fa => fa.Bounds.Contains(pxPos) && fa.IsValid);
		}

		public IEnumerable<Actor> ActorsAt(int2 pxPos)
		{
			var i = (pxPos.X / info.BinSize).Clamp(0, cols - 1);
			var j = (pxPos.Y / info.BinSize).Clamp(0, rows - 1);
			return actors[j*cols + i].Where(a => a.Bounds.Value.Contains(pxPos) && a.IsInWorld);
		}

		// Legacy fallback
		public IEnumerable<Actor> ActorsAt(PPos pxPos) { return ActorsAt(pxPos.ToInt2()); }

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

			for (var j = top; j <= bottom; j++)
				for (var i = left; i <= right; i++)
					foreach (var a in actors[j*cols + i].Where(b => b.Bounds.Value.IntersectsWith(r) && b.IsInWorld))
						yield return a;
		}
	}
}
