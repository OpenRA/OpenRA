﻿#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("This actor will remain visible (but not updated visually) under fog, once discovered.")]
	public class FrozenUnderFogInfo : ITraitInfo, Requires<BuildingInfo>
	{
		public readonly bool StartsRevealed = false;

		public object Create(ActorInitializer init) { return new FrozenUnderFog(init, this); }
	}

	public class FrozenUnderFog : IRenderModifier, IVisibilityModifier, ITick, ITickRender, ISync
	{
		[Sync] public int VisibilityHash;

		readonly bool startsRevealed;
		readonly CPos[] footprintInMapsCoords;
		readonly CellRegion footprintRegion;

		readonly Lazy<IToolTip> tooltip;
		readonly Lazy<Health> health;

		readonly Dictionary<Player, bool> visible;
		readonly Dictionary<Player, FrozenActor> frozen;

		bool initialized;

		public FrozenUnderFog(ActorInitializer init, FrozenUnderFogInfo info)
		{
			// Spawned actors (e.g. building husks) shouldn't be revealed
			startsRevealed = info.StartsRevealed && !init.Contains<ParentActorInit>();
			var footprint = FootprintUtils.Tiles(init.self).ToList();
			footprintInMapsCoords = footprint.Select(cell => Map.CellToMap(init.world.Map.TileShape, cell)).ToArray();
			footprintRegion = CellRegion.BoundingRegion(init.world.Map.TileShape, footprint);
			tooltip = Exts.Lazy(() => init.self.TraitsImplementing<IToolTip>().FirstOrDefault());
			tooltip = Exts.Lazy(() => init.self.TraitsImplementing<IToolTip>().FirstOrDefault());
			health = Exts.Lazy(() => init.self.TraitOrDefault<Health>());

			frozen = new Dictionary<Player, FrozenActor>();
			visible = init.world.Players.ToDictionary(p => p, p => false);
		}

		public bool IsVisible(Actor self, Player byPlayer)
		{
			return byPlayer == null || visible[byPlayer];
		}

		public void Tick(Actor self)
		{
			if (self.Destroyed)
				return;

			VisibilityHash = 0;
			foreach (var p in self.World.Players)
			{
				// We are doing the following LINQ manually to avoid allocating an extra delegate since this is a hot path.
				// var isVisible = footprintInMapsCoords.Any(mapCoord => p.Shroud.IsVisibleTest(footprintRegion)(mapCoord.X, mapCoord.Y));
				var isVisibleTest = p.Shroud.IsVisibleTest(footprintRegion);
				var isVisible = false;
				foreach (var mapCoord in footprintInMapsCoords)
					if (isVisibleTest(mapCoord.X, mapCoord.Y))
					{
						isVisible = true;
						break;
					}

				visible[p] = isVisible;
				if (isVisible)
					VisibilityHash += p.ClientIndex;
			}

			if (!initialized)
			{
				foreach (var p in self.World.Players)
				{
					visible[p] |= startsRevealed;
					p.PlayerActor.Trait<FrozenActorLayer>().Add(frozen[p] = new FrozenActor(self, footprintInMapsCoords, footprintRegion));
				}

				initialized = true;
			}

			foreach (var player in self.World.Players)
			{
				if (!visible[player])
					continue;

				var actor = frozen[player];
				actor.Owner = self.Owner;

				if (health.Value != null)
				{
					actor.HP = health.Value.HP;
					actor.DamageState = health.Value.DamageState;
				}

				if (tooltip.Value != null)
				{
					actor.TooltipInfo = tooltip.Value.TooltipInfo;
					actor.TooltipOwner = tooltip.Value.Owner;
				}
			}
		}

		public void TickRender(WorldRenderer wr, Actor self)
		{
			if (self.Destroyed || !initialized || !visible.Values.Any(v => v))
				return;

			var renderables = self.Render(wr).ToArray();
			foreach (var player in self.World.Players)
				if (visible[player])
					frozen[player].Renderables = renderables;
		}

		public IEnumerable<IRenderable> ModifyRender(Actor self, WorldRenderer wr, IEnumerable<IRenderable> r)
		{
			return IsVisible(self, self.World.RenderPlayer) ? r : SpriteRenderable.None;
		}
	}
}