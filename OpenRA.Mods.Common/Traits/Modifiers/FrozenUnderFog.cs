#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
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
	public class FrozenUnderFogInfo : ITraitInfo, Requires<BuildingInfo>, IDefaultVisibilityInfo
	{
		public readonly bool StartsRevealed = false;

		[Desc("Players with these stances can always see the actor.")]
		public readonly Stance AlwaysVisibleStances = Stance.Ally;

		public object Create(ActorInitializer init) { return new FrozenUnderFog(init, this); }
	}

	public class FrozenUnderFog : IRenderModifier, IDefaultVisibility, ITick, ISync
	{
		[Sync] public int VisibilityHash;

		readonly FrozenUnderFogInfo info;
		readonly bool startsRevealed;
		readonly PPos[] footprint;

		readonly Lazy<ITooltip> tooltip;
		readonly Lazy<Health> health;

		readonly Dictionary<Player, FrozenState> stateByPlayer = new Dictionary<Player, FrozenState>();

		bool initialized;

		class FrozenState
		{
			public readonly FrozenActor FrozenActor;
			public bool IsVisible;
			public FrozenState(FrozenActor frozenActor)
			{
				FrozenActor = frozenActor;
			}
		}

		public FrozenUnderFog(ActorInitializer init, FrozenUnderFogInfo info)
		{
			this.info = info;

			var map = init.World.Map;

			// Spawned actors (e.g. building husks) shouldn't be revealed
			startsRevealed = info.StartsRevealed && !init.Contains<ParentActorInit>();
			var footprintCells = FootprintUtils.Tiles(init.Self).ToList();
			footprint = footprintCells.SelectMany(c => map.ProjectedCellsCovering(c.ToMPos(map))).ToArray();
			tooltip = Exts.Lazy(() => init.Self.TraitsImplementing<ITooltip>().FirstOrDefault());
			health = Exts.Lazy(() => init.Self.TraitOrDefault<Health>());
		}

		bool IsVisibleInner(Actor self, Player byPlayer)
		{
			// If fog is disabled visibility is determined by shroud
			if (!byPlayer.Shroud.FogEnabled)
				return self.OccupiesSpace.OccupiedCells()
					.Any(o => byPlayer.Shroud.IsExplored(o.First));

			return initialized && stateByPlayer[byPlayer].IsVisible;
		}

		public bool IsVisible(Actor self, Player byPlayer)
		{
			if (byPlayer == null)
				return true;

			var stance = self.Owner.Stances[byPlayer];
			return info.AlwaysVisibleStances.HasStance(stance) || IsVisibleInner(self, byPlayer);
		}

		public void Tick(Actor self)
		{
			if (self.Disposed)
				return;

			VisibilityHash = 0;
			foreach (var player in self.World.Players)
			{
				FrozenActor frozenActor;
				bool isVisible;
				if (!initialized)
				{
					frozenActor = new FrozenActor(self, footprint, player.Shroud, startsRevealed);
					isVisible = startsRevealed;
					stateByPlayer.Add(player, new FrozenState(frozenActor) { IsVisible = isVisible });
					player.PlayerActor.Trait<FrozenActorLayer>().Add(frozenActor);
				}
				else
				{
					var state = stateByPlayer[player];
					frozenActor = state.FrozenActor;
					isVisible = !frozenActor.Visible;
					state.IsVisible = isVisible;
				}

				if (isVisible)
					VisibilityHash += player.ClientIndex;
				else
					continue;

				frozenActor.Owner = self.Owner;

				if (health.Value != null)
				{
					frozenActor.HP = health.Value.HP;
					frozenActor.DamageState = health.Value.DamageState;
				}

				if (tooltip.Value != null)
				{
					frozenActor.TooltipInfo = tooltip.Value.TooltipInfo;
					frozenActor.TooltipOwner = tooltip.Value.Owner;
				}
			}

			initialized = true;
		}

		public IEnumerable<IRenderable> ModifyRender(Actor self, WorldRenderer wr, IEnumerable<IRenderable> r)
		{
			return
				IsVisible(self, self.World.RenderPlayer) ||
				(initialized && stateByPlayer[self.World.RenderPlayer].FrozenActor.IsRendering) ?
				r : SpriteRenderable.None;
		}
	}
}