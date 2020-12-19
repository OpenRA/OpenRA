#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Traits
{
	public interface ICreatesFrozenActors
	{
		void OnVisibilityChanged(FrozenActor frozen);
	}

	[Desc("Required for FrozenUnderFog to work. Attach this to the player actor.")]
	public class FrozenActorLayerInfo : TraitInfo, Requires<ShroudInfo>
	{
		[Desc("Size of partition bins (cells)")]
		public readonly int BinSize = 10;

		public override object Create(ActorInitializer init) { return new FrozenActorLayer(init.Self, this); }
	}

	public class FrozenActor
	{
		public readonly PPos[] Footprint;
		public readonly WPos CenterPosition;
		readonly Actor actor;
		readonly ICreatesFrozenActors frozenTrait;
		readonly Player viewer;
		readonly Shroud shroud;
		readonly List<WPos> targetablePositions = new List<WPos>();

		public Player Owner { get; private set; }
		public BitSet<TargetableType> TargetTypes { get; private set; }
		public IEnumerable<WPos> TargetablePositions { get { return targetablePositions; } }

		public ITooltipInfo TooltipInfo { get; private set; }
		public Player TooltipOwner { get; private set; }
		readonly ITooltip[] tooltips;

		public int HP { get; private set; }
		public DamageState DamageState { get; private set; }
		readonly IHealth health;

		// The Visible flag is tied directly to the actor visibility under the fog.
		// If Visible is true, the actor is made invisible (via FrozenUnderFog/IDefaultVisibility)
		// and this FrozenActor is rendered instead.
		// The Hidden flag covers the edge case that occurs when the backing actor was last "seen"
		// to be cloaked or otherwise not CanBeViewedByPlayer()ed. Setting Visible to true when
		// the actor is hidden under the fog would leak the actors position via the tooltips and
		// AutoTargetability, and keeping Visible as false would cause the actor to be rendered
		// under the fog.
		public bool Visible = true;
		public bool Hidden = false;

		public bool Shrouded { get; private set; }
		public bool NeedRenderables { get; set; }
		public IRenderable[] Renderables = NoRenderables;
		public Rectangle[] ScreenBounds = NoBounds;

		public Polygon MouseBounds = Polygon.Empty;

		static readonly IRenderable[] NoRenderables = new IRenderable[0];
		static readonly Rectangle[] NoBounds = new Rectangle[0];

		int flashTicks;

		public FrozenActor(Actor actor, ICreatesFrozenActors frozenTrait, PPos[] footprint, Player viewer, bool startsRevealed)
		{
			this.actor = actor;
			this.frozenTrait = frozenTrait;
			this.viewer = viewer;
			shroud = viewer.Shroud;
			NeedRenderables = startsRevealed;

			// Consider all cells inside the map area (ignoring the current map bounds)
			Footprint = footprint
				.Where(m => shroud.Contains(m))
				.ToArray();

			if (Footprint.Length == 0)
				throw new ArgumentException(("This frozen actor has no footprint.\n" +
					"Actor Name: {0}\n" +
					"Actor Location: {1}\n" +
					"Input footprint: [{2}]\n" +
					"Input footprint (after shroud.Contains): [{3}]")
					.F(actor.Info.Name,
					actor.Location.ToString(),
					footprint.Select(p => p.ToString()).JoinWith("|"),
					footprint.Select(p => shroud.Contains(p).ToString()).JoinWith("|")));

			CenterPosition = actor.CenterPosition;

			tooltips = actor.TraitsImplementing<ITooltip>().ToArray();
			health = actor.TraitOrDefault<IHealth>();

			UpdateVisibility();
		}

		public uint ID { get { return actor.ActorID; } }
		public bool IsValid { get { return Owner != null; } }
		public ActorInfo Info { get { return actor.Info; } }
		public Actor Actor { get { return !actor.IsDead ? actor : null; } }
		public Player Viewer { get { return viewer; } }

		public void RefreshState()
		{
			Owner = actor.Owner;
			TargetTypes = actor.GetEnabledTargetTypes();
			targetablePositions.Clear();
			targetablePositions.AddRange(actor.GetTargetablePositions());
			Hidden = !actor.CanBeViewedByPlayer(viewer);

			if (health != null)
			{
				HP = health.HP;
				DamageState = health.DamageState;
			}

			var tooltip = tooltips.FirstEnabledTraitOrDefault();
			if (tooltip != null)
			{
				TooltipInfo = tooltip.TooltipInfo;
				TooltipOwner = tooltip.Owner;
			}
		}

		public void Tick()
		{
			if (flashTicks > 0)
				flashTicks--;
		}

		public void UpdateVisibility()
		{
			var wasVisible = Visible;
			Shrouded = true;
			Visible = true;

			// PERF: Avoid LINQ.
			foreach (var puv in Footprint)
			{
				if (shroud.IsVisible(puv))
				{
					Visible = false;
					Shrouded = false;
					break;
				}

				if (Shrouded && shroud.IsExplored(puv))
					Shrouded = false;
			}

			// Force the backing trait to update so other actors can't
			// query inconsistent state (both hidden or both visible)
			if (Visible != wasVisible)
				frozenTrait.OnVisibilityChanged(this);

			NeedRenderables |= Visible && !wasVisible;
		}

		public void Invalidate()
		{
			Owner = null;
		}

		public void Flash()
		{
			flashTicks = 5;
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (Shrouded)
				return NoRenderables;

			if (flashTicks > 0 && flashTicks % 2 == 0)
			{
				return Renderables.Concat(Renderables.Where(r => !r.IsDecoration && r is IModifyableRenderable)
					.Select(r =>
					{
						var mr = (IModifyableRenderable)r;
						return mr.WithTint(float3.Ones, mr.TintModifiers | TintModifiers.ReplaceColor).WithAlpha(0.5f);
					}));
			}

			return Renderables;
		}

		public bool HasRenderables { get { return !Shrouded && Renderables.Any(); } }

		public override string ToString()
		{
			return "{0} {1}{2}".F(Info.Name, ID, IsValid ? "" : " (invalid)");
		}
	}

	public class FrozenActorLayer : IRender, ITick, ISync
	{
		[Sync]
		public int VisibilityHash;

		[Sync]
		public int FrozenHash;

		readonly int binSize;
		readonly World world;
		readonly Player owner;
		readonly Dictionary<uint, FrozenActor> frozenActorsById;
		readonly SpatiallyPartitioned<uint> partitionedFrozenActorIds;
		readonly HashSet<uint> dirtyFrozenActorIds = new HashSet<uint>();

		public FrozenActorLayer(Actor self, FrozenActorLayerInfo info)
		{
			binSize = info.BinSize;
			world = self.World;
			owner = self.Owner;
			frozenActorsById = new Dictionary<uint, FrozenActor>();

			partitionedFrozenActorIds = new SpatiallyPartitioned<uint>(
				world.Map.MapSize.X, world.Map.MapSize.Y, binSize);

			self.Trait<Shroud>().OnShroudChanged += uv => dirtyFrozenActorIds.UnionWith(partitionedFrozenActorIds.At(new int2(uv.U, uv.V)));
		}

		public void Add(FrozenActor fa)
		{
			frozenActorsById.Add(fa.ID, fa);
			world.ScreenMap.AddOrUpdate(owner, fa);
			partitionedFrozenActorIds.Add(fa.ID, FootprintBounds(fa));
		}

		public void Remove(FrozenActor fa)
		{
			partitionedFrozenActorIds.Remove(fa.ID);
			world.ScreenMap.Remove(owner, fa);
			frozenActorsById.Remove(fa.ID);
		}

		Rectangle FootprintBounds(FrozenActor fa)
		{
			var p1 = fa.Footprint[0];
			var minU = p1.U;
			var maxU = p1.U;
			var minV = p1.V;
			var maxV = p1.V;
			foreach (var p in fa.Footprint)
			{
				if (minU > p.U)
					minU = p.U;
				else if (maxU < p.U)
					maxU = p.U;

				if (minV > p.V)
					minV = p.V;
				else if (maxV < p.V)
					maxV = p.V;
			}

			return Rectangle.FromLTRB(minU, minV, maxU + 1, maxV + 1);
		}

		void ITick.Tick(Actor self)
		{
			var frozenActorsToRemove = new List<FrozenActor>();
			VisibilityHash = 0;
			FrozenHash = 0;

			foreach (var kvp in frozenActorsById)
			{
				var id = kvp.Key;
				var hash = (int)id;
				FrozenHash += hash;

				var frozenActor = kvp.Value;
				frozenActor.Tick();
				if (dirtyFrozenActorIds.Contains(id))
					frozenActor.UpdateVisibility();

				if (frozenActor.Visible)
					VisibilityHash += hash;
				else if (frozenActor.Actor == null)
					frozenActorsToRemove.Add(frozenActor);
			}

			dirtyFrozenActorIds.Clear();

			foreach (var fa in frozenActorsToRemove)
				Remove(fa);
		}

		public virtual IEnumerable<IRenderable> Render(Actor self, WorldRenderer wr)
		{
			return world.ScreenMap.RenderableFrozenActorsInBox(owner, wr.Viewport.TopLeft, wr.Viewport.BottomRight)
				.Where(f => f.Visible)
				.SelectMany(ff => ff.Render(wr));
		}

		public IEnumerable<Rectangle> ScreenBounds(Actor self, WorldRenderer wr)
		{
			// Player-actor render traits don't require screen bounds
			yield break;
		}

		public FrozenActor FromID(uint id)
		{
			if (!frozenActorsById.TryGetValue(id, out var fa))
				return null;

			return fa;
		}

		public IEnumerable<FrozenActor> FrozenActorsInRegion(CellRegion region, bool onlyVisible = true)
		{
			var tl = region.TopLeft;
			var br = region.BottomRight;
			return partitionedFrozenActorIds.InBox(Rectangle.FromLTRB(tl.X, tl.Y, br.X, br.Y))
				.Select(FromID)
				.Where(fa => fa.IsValid && (!onlyVisible || fa.Visible));
		}

		public IEnumerable<FrozenActor> FrozenActorsInCircle(World world, WPos origin, WDist r, bool onlyVisible = true)
		{
			var centerCell = world.Map.CellContaining(origin);
			var cellRange = (r.Length + 1023) / 1024;
			var tl = centerCell - new CVec(cellRange, cellRange);
			var br = centerCell + new CVec(cellRange, cellRange);

			// Target ranges are calculated in 2D, so ignore height differences
			return partitionedFrozenActorIds.InBox(Rectangle.FromLTRB(tl.X, tl.Y, br.X, br.Y))
				.Select(FromID)
				.Where(fa => fa.IsValid &&
					(!onlyVisible || fa.Visible) &&
					(fa.CenterPosition - origin).HorizontalLengthSquared <= r.LengthSquared);
		}
	}
}
