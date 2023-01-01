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
using System.Linq;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Traits
{
	public readonly struct ActorBoundsPair
	{
		public readonly Actor Actor;
		public readonly Polygon Bounds;

		public ActorBoundsPair(Actor actor, Polygon bounds) { Actor = actor; Bounds = bounds; }

		public override int GetHashCode() { return Actor.GetHashCode() ^ Bounds.GetHashCode(); }

		public override string ToString() { return $"{Actor.Info.Name}->{Bounds.GetType().Name}"; }
	}

	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	public class ScreenMapInfo : TraitInfo
	{
		[Desc("Size of partition bins (world pixels)")]
		public readonly int BinSize = 250;

		public override object Create(ActorInitializer init) { return new ScreenMap(init.World, this); }
	}

	public class ScreenMap : IWorldLoaded
	{
		static readonly IEnumerable<FrozenActor> NoFrozenActors = Array.Empty<FrozenActor>();
		readonly Func<FrozenActor, bool> frozenActorIsValid = fa => fa.IsValid;
		readonly Func<Actor, bool> actorIsInWorld = a => a.IsInWorld;
		readonly Func<Actor, ActorBoundsPair> selectActorAndBounds;
		readonly Cache<Player, SpatiallyPartitioned<FrozenActor>> partitionedMouseFrozenActors;
		readonly SpatiallyPartitioned<Actor> partitionedMouseActors;
		readonly Dictionary<Actor, ActorBoundsPair> partitionedMouseActorBounds = new Dictionary<Actor, ActorBoundsPair>();

		readonly Cache<Player, SpatiallyPartitioned<FrozenActor>> partitionedRenderableFrozenActors;
		readonly SpatiallyPartitioned<Actor> partitionedRenderableActors;
		readonly SpatiallyPartitioned<IEffect> partitionedRenderableEffects;

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

			partitionedMouseFrozenActors = new Cache<Player, SpatiallyPartitioned<FrozenActor>>(
				_ => new SpatiallyPartitioned<FrozenActor>(width, height, info.BinSize));
			partitionedMouseActors = new SpatiallyPartitioned<Actor>(width, height, info.BinSize);
			selectActorAndBounds = a => partitionedMouseActorBounds[a];

			partitionedRenderableFrozenActors = new Cache<Player, SpatiallyPartitioned<FrozenActor>>(
				_ => new SpatiallyPartitioned<FrozenActor>(width, height, info.BinSize));
			partitionedRenderableActors = new SpatiallyPartitioned<Actor>(width, height, info.BinSize);
			partitionedRenderableEffects = new SpatiallyPartitioned<IEffect>(width, height, info.BinSize);

			addOrUpdateFrozenActors = new Cache<Player, HashSet<FrozenActor>>(_ => new HashSet<FrozenActor>());
			removeFrozenActors = new Cache<Player, HashSet<FrozenActor>>(_ => new HashSet<FrozenActor>());
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
				partitionedRenderableEffects.Add(effect, screenBounds);
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
			partitionedRenderableEffects.Remove(effect);
		}

		static bool ValidBounds(Rectangle bounds)
		{
			return bounds.Width > 0 && bounds.Height > 0;
		}

		public IEnumerable<FrozenActor> FrozenActorsAtMouse(Player viewer, int2 worldPx)
		{
			if (viewer == null)
				return NoFrozenActors;

			return partitionedMouseFrozenActors[viewer]
				.At(worldPx)
				.Where(frozenActorIsValid)
				.Where(x => x.MouseBounds.Contains(worldPx));
		}

		public IEnumerable<FrozenActor> FrozenActorsAtMouse(Player viewer, MouseInput mi)
		{
			return FrozenActorsAtMouse(viewer, worldRenderer.Viewport.ViewToWorldPx(mi.Location));
		}

		public IEnumerable<ActorBoundsPair> ActorsAtMouse(int2 worldPx)
		{
			return partitionedMouseActors.At(worldPx)
				.Where(actorIsInWorld)
				.Select(selectActorAndBounds)
				.Where(x => x.Bounds.Contains(worldPx));
		}

		public IEnumerable<ActorBoundsPair> ActorsAtMouse(MouseInput mi)
		{
			return ActorsAtMouse(worldRenderer.Viewport.ViewToWorldPx(mi.Location));
		}

		static Rectangle RectWithCorners(int2 a, int2 b)
		{
			return Rectangle.FromLTRB(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));
		}

		public IEnumerable<ActorBoundsPair> ActorsInMouseBox(int2 a, int2 b)
		{
			return ActorsInMouseBox(RectWithCorners(a, b));
		}

		public IEnumerable<ActorBoundsPair> ActorsInMouseBox(Rectangle r)
		{
			return partitionedMouseActors.InBox(r)
				.Where(actorIsInWorld)
				.Select(selectActorAndBounds)
				.Where(x => x.Bounds.IntersectsWith(r));
		}

		public IEnumerable<Actor> RenderableActorsInBox(int2 a, int2 b)
		{
			return partitionedRenderableActors.InBox(RectWithCorners(a, b)).Where(actorIsInWorld);
		}

		public IEnumerable<IEffect> RenderableEffectsInBox(int2 a, int2 b)
		{
			return partitionedRenderableEffects.InBox(RectWithCorners(a, b));
		}

		public IEnumerable<FrozenActor> RenderableFrozenActorsInBox(Player p, int2 a, int2 b)
		{
			if (p == null)
				return NoFrozenActors;

			return partitionedRenderableFrozenActors[p].InBox(RectWithCorners(a, b)).Where(frozenActorIsValid);
		}

		public void TickRender()
		{
			foreach (var a in addOrUpdateActors)
			{
				var mouseBounds = a.MouseBounds(worldRenderer);
				if (!mouseBounds.IsEmpty)
				{
					if (partitionedMouseActors.Contains(a))
						partitionedMouseActors.Update(a, mouseBounds.BoundingRect);
					else
						partitionedMouseActors.Add(a, mouseBounds.BoundingRect);

					partitionedMouseActorBounds[a] = new ActorBoundsPair(a, mouseBounds);
				}
				else
					partitionedMouseActors.Remove(a);

				var screenBounds = a.ScreenBounds(worldRenderer).Union();
				if (!screenBounds.Size.IsEmpty)
				{
					if (partitionedRenderableActors.Contains(a))
						partitionedRenderableActors.Update(a, screenBounds);
					else
						partitionedRenderableActors.Add(a, screenBounds);
				}
				else
					partitionedRenderableActors.Remove(a);
			}

			foreach (var a in removeActors)
			{
				partitionedMouseActors.Remove(a);
				partitionedMouseActorBounds.Remove(a);
				partitionedRenderableActors.Remove(a);
			}

			addOrUpdateActors.Clear();
			removeActors.Clear();

			foreach (var kv in addOrUpdateFrozenActors)
			{
				foreach (var fa in kv.Value)
				{
					var mouseBounds = fa.MouseBounds;
					if (!mouseBounds.IsEmpty)
					{
						if (partitionedMouseFrozenActors[kv.Key].Contains(fa))
							partitionedMouseFrozenActors[kv.Key].Update(fa, mouseBounds.BoundingRect);
						else
							partitionedMouseFrozenActors[kv.Key].Add(fa, mouseBounds.BoundingRect);
					}
					else
						partitionedMouseFrozenActors[kv.Key].Remove(fa);

					var screenBounds = fa.ScreenBounds.Union();
					if (!screenBounds.Size.IsEmpty)
					{
						if (partitionedRenderableFrozenActors[kv.Key].Contains(fa))
							partitionedRenderableFrozenActors[kv.Key].Update(fa, screenBounds);
						else
							partitionedRenderableFrozenActors[kv.Key].Add(fa, screenBounds);
					}
					else
						partitionedRenderableFrozenActors[kv.Key].Remove(fa);
				}

				kv.Value.Clear();
			}

			foreach (var kv in removeFrozenActors)
			{
				foreach (var fa in kv.Value)
				{
					partitionedMouseFrozenActors[kv.Key].Remove(fa);
					partitionedRenderableFrozenActors[kv.Key].Remove(fa);
				}

				kv.Value.Clear();
			}
		}

		public IEnumerable<Rectangle> RenderBounds(Player viewer)
		{
			var bounds = partitionedRenderableActors.ItemBounds
				.Concat(partitionedRenderableEffects.ItemBounds);

			return viewer != null ? bounds.Concat(partitionedRenderableFrozenActors[viewer].ItemBounds) : bounds;
		}

		public IEnumerable<Polygon> MouseBounds(Player viewer)
		{
			var bounds = partitionedMouseActorBounds.Values.Select(a => a.Bounds);
			return viewer != null ? bounds.Concat(partitionedMouseFrozenActors[viewer].Items.Select(fa => fa.MouseBounds)) : bounds;
		}
	}
}
