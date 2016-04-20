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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("This actor will remain visible (but not updated visually) under fog, once discovered.")]
	public class FrozenUnderFogInfo : ITraitInfo, Requires<BuildingInfo>, IDefaultVisibilityInfo
	{
		[Desc("Players with these stances can always see the actor.")]
		public readonly Stance AlwaysVisibleStances = Stance.Ally;

		public object Create(ActorInitializer init) { return new FrozenUnderFog(init, this); }
	}

	public class FrozenUnderFog : IRenderModifier, IDefaultVisibility, ITick, ITickRender, ISync, INotifyCreated
	{
		[Sync] public int VisibilityHash;

		readonly FrozenUnderFogInfo info;
		readonly bool startsRevealed;
		readonly PPos[] footprint;

		PlayerDictionary<FrozenState> frozenStates;
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

			// Explore map-placed actors if the "Explore Map" option is enabled
			var shroudInfo = init.World.Map.Rules.Actors["player"].TraitInfo<ShroudInfo>();
			var exploredMap = init.World.LobbyInfo.GlobalSettings.OptionOrDefault("explored", shroudInfo.ExploredMapEnabled);
			startsRevealed = exploredMap && init.Contains<SpawnedByMapInit>() && !init.Contains<HiddenUnderFogInit>();
			var footprintCells = FootprintUtils.FrozenUnderFogTiles(init.Self).ToList();
			footprint = footprintCells.SelectMany(c => map.ProjectedCellsCovering(c.ToMPos(map))).ToArray();
		}

		public void Created(Actor self)
		{
			frozenStates = new PlayerDictionary<FrozenState>(self.World, (player, playerIndex) =>
			{
				var frozenActor = new FrozenActor(self, footprint, player.Shroud, startsRevealed);
				if (startsRevealed)
					UpdateFrozenActor(self, frozenActor, playerIndex);
				player.PlayerActor.Trait<FrozenActorLayer>().Add(frozenActor);
				return new FrozenState(frozenActor) { IsVisible = startsRevealed };
			});
		}

		void UpdateFrozenActor(Actor self, FrozenActor frozenActor, int playerIndex)
		{
			VisibilityHash |= 1 << (playerIndex % 32);
			frozenActor.RefreshState();
		}

		bool IsVisibleInner(Actor self, Player byPlayer)
		{
			// If fog is disabled visibility is determined by shroud
			if (!byPlayer.Shroud.FogEnabled)
				return byPlayer.Shroud.AnyExplored(self.OccupiesSpace.OccupiedCells());

			return frozenStates[byPlayer].IsVisible;
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

			for (var playerIndex = 0; playerIndex < frozenStates.Count; playerIndex++)
			{
				var state = frozenStates[playerIndex];
				var frozenActor = state.FrozenActor;
				var isVisible = !frozenActor.Visible;
				state.IsVisible = isVisible;

				if (isVisible)
					UpdateFrozenActor(self, frozenActor, playerIndex);
			}
		}

		public void TickRender(WorldRenderer wr, Actor self)
		{
			IRenderable[] renderables = null;
			for (var playerIndex = 0; playerIndex < frozenStates.Count; playerIndex++)
			{
				var frozen = frozenStates[playerIndex].FrozenActor;
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
			return IsVisible(self, self.World.RenderPlayer) || isRendering ? r : SpriteRenderable.None;
		}
	}

	public class HiddenUnderFogInit : IActorInit { }
}