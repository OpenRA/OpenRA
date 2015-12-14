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

	public class FrozenUnderFog : IRenderModifier, IDefaultVisibility, ITick, ITickRender, ISync
	{
		[Sync] public int VisibilityHash;

		readonly FrozenUnderFogInfo info;
		readonly bool startsRevealed;
		readonly PPos[] footprint;

		readonly Dictionary<Player, FrozenState> stateByPlayer = new Dictionary<Player, FrozenState>();

		FrozenState[] stateByPlayerIndex;
		ITooltip tooltip;
		Health health;
		bool initialized;
		bool isRendering;

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
		}

		bool IsVisibleInner(Actor self, Player byPlayer)
		{
			// If fog is disabled visibility is determined by shroud
			if (!byPlayer.Shroud.FogEnabled)
				return byPlayer.Shroud.AnyExplored(self.OccupiesSpace.OccupiedCells());

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
			var players = self.World.Players;

			if (!initialized)
			{
				// The world players never change, so we can safely index this collection.
				stateByPlayerIndex = new FrozenState[players.Length];
				tooltip = self.TraitsImplementing<ITooltip>().FirstOrDefault();
				health = self.TraitOrDefault<Health>();
			}

			for (var i = 0; i < players.Length; i++)
			{
				FrozenActor frozenActor;
				bool isVisible;
				if (!initialized)
				{
					var player = players[i];
					frozenActor = new FrozenActor(self, footprint, player.Shroud, startsRevealed);
					isVisible = startsRevealed;
					var state = new FrozenState(frozenActor) { IsVisible = isVisible };
					stateByPlayer.Add(player, state);
					stateByPlayerIndex[i] = state;
					player.PlayerActor.Trait<FrozenActorLayer>().Add(frozenActor);
				}
				else
				{
					// PERF: Minimize lookup cost by combining all state into one, and using an array rather than a dictionary.
					var state = stateByPlayerIndex[i];
					frozenActor = state.FrozenActor;
					isVisible = !frozenActor.Visible;
					state.IsVisible = isVisible;
				}

				if (isVisible)
					VisibilityHash |= 1 << (i % 32);
				else
					continue;

				frozenActor.Owner = self.Owner;

				if (health != null)
				{
					frozenActor.HP = health.HP;
					frozenActor.DamageState = health.DamageState;
				}

				if (tooltip != null)
				{
					frozenActor.TooltipInfo = tooltip.TooltipInfo;
					frozenActor.TooltipOwner = tooltip.Owner;
				}
			}

			initialized = true;
		}

		public void TickRender(WorldRenderer wr, Actor self)
		{
			if (!initialized)
				return;

			IRenderable[] renderables = null;
			foreach (var player in self.World.Players)
			{
				var frozen = stateByPlayer[player].FrozenActor;
				if (!frozen.NeedRenderables)
					continue;

				if (renderables == null)
				{
					isRendering = true;
					renderables = self.Render(wr).ToArray();
					isRendering = false;
				}

				frozen.NeedRenderables = false;
				frozen.Renderables = renderables;
			}
		}

		public IEnumerable<IRenderable> ModifyRender(Actor self, WorldRenderer wr, IEnumerable<IRenderable> r)
		{
			return
				IsVisible(self, self.World.RenderPlayer) ||
				(initialized && isRendering) ?
				r : SpriteRenderable.None;
		}
	}
}