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
		WorldRenderer worldRenderer;

		public ScreenMap(World world, ScreenMapInfo info)
		{
			var size = world.Map.Grid.TileSize;
			var width = world.Map.MapSize.X * size.Width;
			var height = world.Map.MapSize.Y * size.Height;
			partitionedFrozenActors = new Cache<Player, SpatiallyPartitioned<FrozenActor>>(
				_ => new SpatiallyPartitioned<FrozenActor>(width, height, info.BinSize));
			partitionedActors = new SpatiallyPartitioned<Actor>(width, height, info.BinSize);
			partitionedEffects = new SpatiallyPartitioned<IEffect>(width, height, info.BinSize);
		}

		public void WorldLoaded(World w, WorldRenderer wr) { worldRenderer = wr; }

		Rectangle FrozenActorBounds(FrozenActor fa)
		{
			var pos = worldRenderer.ScreenPxPosition(fa.CenterPosition);
			var bounds = fa.RenderBounds;
			bounds.Offset(pos.X, pos.Y);
			return bounds;
		}

		Rectangle ActorBounds(Actor a)
		{
			var pos = worldRenderer.ScreenPxPosition(a.CenterPosition);
			var bounds = a.RenderBounds;
			bounds.Offset(pos.X, pos.Y);
			return bounds;
		}

		public void Add(Player viewer, FrozenActor fa)
		{
			partitionedFrozenActors[viewer].Add(fa, FrozenActorBounds(fa));
		}

		public void Remove(Player viewer, FrozenActor fa)
		{
			partitionedFrozenActors[viewer].Remove(fa);
		}

		public void Add(Actor a)
		{
			partitionedActors.Add(a, ActorBounds(a));
		}

		public void Update(Actor a)
		{
			partitionedActors.Update(a, ActorBounds(a));
		}

		public void Remove(Actor a)
		{
			partitionedActors.Remove(a);
		}

		public void Add(IEffect effect, WPos position, Size size)
		{
			var screenPos = worldRenderer.ScreenPxPosition(position);
			var screenBounds = new Rectangle(screenPos.X - size.Width / 2, screenPos.Y - size.Height / 2, size.Width, size.Height);
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
			var screenPos = worldRenderer.ScreenPxPosition(position);
			var screenBounds = new Rectangle(screenPos.X - size.Width / 2, screenPos.Y - size.Height / 2, size.Width, size.Height);
			partitionedEffects.Remove(effect);
			if (ValidBounds(screenBounds))
				partitionedEffects.Add(effect, screenBounds);
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
	}
}
