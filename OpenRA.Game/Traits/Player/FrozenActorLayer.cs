#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Traits
{
	[Desc("Required for FrozenUnderFog to work. Attach this to the player actor.")]
	public class FrozenActorLayerInfo : Requires<ShroudInfo>, ITraitInfo
	{
		[Desc("Size of partition bins (cells)")]
		public readonly int BinSize = 10;

		public object Create(ActorInitializer init) { return new FrozenActorLayer(init.Self, this); }
	}

	public class FrozenActor
	{
		public readonly PPos[] Footprint;
		public readonly WPos CenterPosition;
		public readonly Rectangle Bounds;
		public readonly HashSet<string> TargetTypes;
		readonly Actor actor;
		readonly Shroud shroud;

		public Player Owner { get; private set; }

		public ITooltipInfo TooltipInfo { get; private set; }
		public Player TooltipOwner { get; private set; }
		readonly ITooltip tooltip;

		public int HP { get; private set; }
		public DamageState DamageState { get; private set; }
		readonly IHealth health;

		public bool Visible = true;
		public bool Shrouded { get; private set; }
		public bool NeedRenderables { get; set; }
		public IRenderable[] Renderables = NoRenderables;
		static readonly IRenderable[] NoRenderables = new IRenderable[0];

		int flashTicks;

		public FrozenActor(Actor self, PPos[] footprint, Shroud shroud, bool startsRevealed)
		{
			actor = self;
			this.shroud = shroud;
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

			CenterPosition = self.CenterPosition;
			Bounds = self.Bounds;
			TargetTypes = self.GetEnabledTargetTypes().ToHashSet();

			tooltip = self.TraitsImplementing<ITooltip>().FirstOrDefault();
			health = self.TraitOrDefault<IHealth>();

			UpdateVisibility();
		}

		public uint ID { get { return actor.ActorID; } }
		public bool IsValid { get { return Owner != null; } }
		public ActorInfo Info { get { return actor.Info; } }
		public Actor Actor { get { return !actor.IsDead ? actor : null; } }

		public void RefreshState()
		{
			Owner = actor.Owner;

			if (health != null)
			{
				HP = health.HP;
				DamageState = health.DamageState;
			}

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

			NeedRenderables |= Visible && !wasVisible;
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
				var highlight = wr.Palette("highlight");
				return Renderables.Concat(Renderables.Where(r => !r.IsDecoration)
					.Select(r => r.WithPalette(highlight)));
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
		[Sync] public int VisibilityHash;
		[Sync] public int FrozenHash;

		readonly int binSize;
		readonly World world;
		readonly Player owner;
		readonly Dictionary<uint, FrozenActor> frozenActorsById;
		readonly SpatiallyPartitioned<uint> partitionedFrozenActorIds;
		readonly bool[] dirtyBins;
		readonly HashSet<uint> dirtyFrozenActorIds = new HashSet<uint>();

		public FrozenActorLayer(Actor self, FrozenActorLayerInfo info)
		{
			binSize = info.BinSize;
			world = self.World;
			owner = self.Owner;
			frozenActorsById = new Dictionary<uint, FrozenActor>();

			// PERF: Partition the map into a series of coarse-grained bins and track changes in the shroud against
			// bin - marking that bin dirty if it changes. This is fairly cheap to track and allows us to perform the
			// expensive visibility update for frozen actors in these regions.
			partitionedFrozenActorIds = new SpatiallyPartitioned<uint>(
				world.Map.MapSize.X, world.Map.MapSize.Y, binSize);
			var maxX = world.Map.MapSize.X / binSize + 1;
			var maxY = world.Map.MapSize.Y / binSize + 1;
			dirtyBins = new bool[maxX * maxY];
			self.Trait<Shroud>().CellsChanged += cells =>
			{
				foreach (var cell in cells)
				{
					var x = cell.U / binSize;
					var y = cell.V / binSize;
					dirtyBins[y * maxX + x] = true;
				}
			};
		}

		public void Add(FrozenActor fa)
		{
			frozenActorsById.Add(fa.ID, fa);
			world.ScreenMap.Add(owner, fa);
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

		public void Tick(Actor self)
		{
			UpdateDirtyFrozenActorsFromDirtyBins();

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

		void UpdateDirtyFrozenActorsFromDirtyBins()
		{
			// Check which bins on the map were dirtied due to changes in the shroud and gather the frozen actors whose
			// footprint overlap with these bins.
			var maxX = world.Map.MapSize.X / binSize + 1;
			var maxY = world.Map.MapSize.Y / binSize + 1;
			for (var y = 0; y < maxY; y++)
			{
				for (var x = 0; x < maxX; x++)
				{
					if (!dirtyBins[y * maxX + x])
						continue;
					var box = new Rectangle(x * binSize, y * binSize, binSize, binSize);
					dirtyFrozenActorIds.UnionWith(partitionedFrozenActorIds.InBox(box));
				}
			}

			Array.Clear(dirtyBins, 0, dirtyBins.Length);
		}

		public virtual IEnumerable<IRenderable> Render(Actor self, WorldRenderer wr)
		{
			return world.ScreenMap.FrozenActorsInBox(owner, wr.Viewport.TopLeft, wr.Viewport.BottomRight)
				.Where(f => f.Visible)
				.SelectMany(ff => ff.Render(wr));
		}

		public FrozenActor FromID(uint id)
		{
			FrozenActor fa;
			if (!frozenActorsById.TryGetValue(id, out fa))
				return null;

			return fa;
		}
	}
}
