#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Traits
{
	public class ScreenMapInfo : ITraitInfo
	{
		[Desc("Size of partition bins (world pixels)")]
		public readonly int BinSize = 250;

		public object Create(ActorInitializer init) { return new ScreenMap(init.World, this); }
	}

	public class ScreenMap : IWorldLoaded
	{
		static readonly IEnumerable<FrozenActor> NoFrozenActors = new FrozenActor[0];
		readonly Func<FrozenActor, bool> frozenActorIsValid = fa => fa.IsValid;
		readonly Func<Actor, bool> actorIsInWorld = a => a.IsInWorld;
		readonly Cache<Player, SpatiallyPartitioned<FrozenActor>> partitionedFrozenActors;
		readonly SpatiallyPartitioned<Actor> partitionedActors;
		readonly SpatiallyPartitioned<IEffect> partitionedEffects;

		// Updates are done in one pass to ensure all bound changes have been applied
		readonly HashSet<Actor> addOrUpdateActors = new HashSet<Actor>();
		readonly HashSet<Actor> removeActors = new HashSet<Actor>();
		readonly Cache<Player, HashSet<FrozenActor>> addOrUpdateFrozenActors;
		readonly Cache<Player, HashSet<FrozenActor>> removeFrozenActors;

		WorldRenderer worldRenderer;

		public ScreenMap(World world, ScreenMapInfo info)
		{
			var size = world.Map.Grid.TileSize;
			var width = world.Map.MapSize.X * size.Width;
			var height = world.Map.MapSize.Y * size.Height;
			partitionedFrozenActors = new Cache<Player, SpatiallyPartitioned<FrozenActor>>(
				_ => new SpatiallyPartitioned<FrozenActor>(width, height, info.BinSize));

			addOrUpdateFrozenActors = new Cache<Player, HashSet<FrozenActor>>(_ => new HashSet<FrozenActor>());
			removeFrozenActors = new Cache<Player, HashSet<FrozenActor>>(_ => new HashSet<FrozenActor>());

			partitionedActors = new SpatiallyPartitioned<Actor>(width, height, info.BinSize);
			partitionedEffects = new SpatiallyPartitioned<IEffect>(width, height, info.BinSize);
		}

		public void WorldLoaded(World w, WorldRenderer wr) { worldRenderer = wr; }

		public void AddOrUpdate(Player viewer, FrozenActor fa)
		{
			if (removeFrozenActors[viewer].Contains(fa))
				removeFrozenActors[viewer].Remove(fa);

			addOrUpdateFrozenActors[viewer].Add(fa);
		}

		public void Remove(Player viewer, FrozenActor fa)
		{
			removeFrozenActors[viewer].Add(fa);
		}

		public void AddOrUpdate(Actor a)
		{
			if (removeActors.Contains(a))
				removeActors.Remove(a);

			addOrUpdateActors.Add(a);
		}

		public void Remove(Actor a)
		{
			removeActors.Add(a);
		}

		public void Add(IEffect effect, WPos position, Size size)
		{
			var screenPos = worldRenderer.ScreenPxPosition(position);
			var screenWidth = Math.Abs(size.Width);
			var screenHeight = Math.Abs(size.Height);
			var screenBounds = new Rectangle(screenPos.X - screenWidth / 2, screenPos.Y - screenHeight / 2, screenWidth, screenHeight);
			if (ValidBounds(screenBounds))
				partitionedEffects.Add(effect, screenBounds);
		}

		public void Add(IEffect effect, WPos position, Sprite sprite)
		{
			var size = new Size((int)sprite.Size.X, (int)sprite.Size.Y);
			Add(effect, position, size);
		}

		public void Update(IEffect effect, WPos position, Size size)
		{
			Remove(effect);
			Add(effect, position, size);
		}

		public void Update(IEffect effect, WPos position, Sprite sprite)
		{
			var size = new Size((int)sprite.Size.X, (int)sprite.Size.Y);
			Update(effect, position, size);
		}

		public void Remove(IEffect effect)
		{
			partitionedEffects.Remove(effect);
		}

		bool ValidBounds(Rectangle bounds)
		{
			return bounds.Width > 0 && bounds.Height > 0;
		}

		public IEnumerable<FrozenActor> FrozenActorsAt(Player viewer, int2 worldPx)
		{
			if (viewer == null)
				return NoFrozenActors;
			return partitionedFrozenActors[viewer].At(worldPx).Where(frozenActorIsValid);
		}

		public IEnumerable<FrozenActor> FrozenActorsAt(Player viewer, MouseInput mi)
		{
			return FrozenActorsAt(viewer, worldRenderer.Viewport.ViewToWorldPx(mi.Location));
		}

		public IEnumerable<Actor> ActorsAt(int2 worldPx)
		{
			return partitionedActors.At(worldPx).Where(actorIsInWorld);
		}

		public IEnumerable<Actor> ActorsAt(MouseInput mi)
		{
			return ActorsAt(worldRenderer.Viewport.ViewToWorldPx(mi.Location));
		}

		static Rectangle RectWithCorners(int2 a, int2 b)
		{
			return Rectangle.FromLTRB(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));
		}

		public IEnumerable<Actor> ActorsInBox(int2 a, int2 b)
		{
			return ActorsInBox(RectWithCorners(a, b));
		}

		public IEnumerable<IEffect> EffectsInBox(int2 a, int2 b)
		{
			return partitionedEffects.InBox(RectWithCorners(a, b));
		}

		public IEnumerable<Actor> ActorsInBox(Rectangle r)
		{
			return partitionedActors.InBox(r).Where(actorIsInWorld);
		}

		public IEnumerable<FrozenActor> FrozenActorsInBox(Player p, int2 a, int2 b)
		{
			return FrozenActorsInBox(p, RectWithCorners(a, b));
		}

		public IEnumerable<IEffect> EffectsInBox(Rectangle r)
		{
			return partitionedEffects.InBox(r);
		}

		public IEnumerable<FrozenActor> FrozenActorsInBox(Player p, Rectangle r)
		{
			if (p == null)
				return NoFrozenActors;
			return partitionedFrozenActors[p].InBox(r).Where(frozenActorIsValid);
		}

		Rectangle AggregateBounds(IEnumerable<Rectangle> screenBounds)
		{
			if (!screenBounds.Any())
				return Rectangle.Empty;

			var bounds = screenBounds.First();
			foreach (var b in screenBounds.Skip(1))
				bounds = Rectangle.Union(bounds, b);

			return bounds;
		}

		public void Tick()
		{
			foreach (var a in addOrUpdateActors)
			{
				var bounds = AggregateBounds(a.ScreenBounds(worldRenderer));
				if (!bounds.Size.IsEmpty)
				{
					if (partitionedActors.Contains(a))
						partitionedActors.Update(a, bounds);
					else
						partitionedActors.Add(a, bounds);
				}
				else
					partitionedActors.Remove(a);
			}

			foreach (var a in removeActors)
				partitionedActors.Remove(a);

			addOrUpdateActors.Clear();
			removeActors.Clear();

			foreach (var kv in addOrUpdateFrozenActors)
			{
				foreach (var fa in kv.Value)
				{
					var bounds = AggregateBounds(fa.ScreenBounds);
					if (!bounds.Size.IsEmpty)
					{
						if (partitionedFrozenActors[kv.Key].Contains(fa))
							partitionedFrozenActors[kv.Key].Update(fa, bounds);
						else
							partitionedFrozenActors[kv.Key].Add(fa, bounds);
					}
					else
						partitionedFrozenActors[kv.Key].Remove(fa);
				}

				kv.Value.Clear();
			}

			foreach (var kv in removeFrozenActors)
			{
				foreach (var fa in kv.Value)
					partitionedFrozenActors[kv.Key].Remove(fa);

				kv.Value.Clear();
			}
		}

		public IEnumerable<Rectangle> ItemBounds(Player viewer)
		{
			var bounds = partitionedActors.ItemBounds
				.Concat(partitionedEffects.ItemBounds);

			return viewer != null ? bounds.Concat(partitionedFrozenActors[viewer].ItemBounds) : bounds;
		}
	}
}
