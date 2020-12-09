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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("This actor will remain visible (but not updated visually) under fog, once discovered.")]
	public class FrozenUnderFogInfo : TraitInfo, Requires<BuildingInfo>, IDefaultVisibilityInfo
	{
		[Desc("Players with these relationships can always see the actor.")]
		public readonly PlayerRelationship AlwaysVisibleRelationships = PlayerRelationship.Ally;

		public override object Create(ActorInitializer init) { return new FrozenUnderFog(init, this); }
	}

	public class FrozenUnderFog : ICreatesFrozenActors, IRenderModifier, IDefaultVisibility, ITick, ITickRender, ISync, INotifyCreated, INotifyOwnerChanged, INotifyActorDisposing
	{
		[Sync]
		public int VisibilityHash;

		readonly FrozenUnderFogInfo info;
		readonly bool startsRevealed;
		readonly PPos[] footprint;

		PlayerDictionary<FrozenState> frozenStates;
		bool isRendering;
		bool created;

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
			var exploredMap = init.World.LobbyInfo.GlobalSettings.OptionOrDefault("explored", shroudInfo.ExploredMapCheckboxEnabled);
			startsRevealed = exploredMap && init.Contains<SpawnedByMapInit>() && !init.Contains<HiddenUnderFogInit>();
			var buildingInfo = init.Self.Info.TraitInfoOrDefault<BuildingInfo>();
			var footprintCells = buildingInfo != null ? buildingInfo.FrozenUnderFogTiles(init.Self.Location).ToList() : new List<CPos>() { init.Self.Location };
			footprint = footprintCells.SelectMany(c => map.ProjectedCellsCovering(c.ToMPos(map))).ToArray();
		}

		void INotifyCreated.Created(Actor self)
		{
			frozenStates = new PlayerDictionary<FrozenState>(self.World, (player, playerIndex) =>
			{
				var frozenActor = new FrozenActor(self, this, footprint, player, startsRevealed);
				player.PlayerActor.Trait<FrozenActorLayer>().Add(frozenActor);
				return new FrozenState(frozenActor) { IsVisible = startsRevealed };
			});
		}

		void UpdateFrozenActor(Actor self, FrozenActor frozenActor, int playerIndex)
		{
			VisibilityHash |= 1 << (playerIndex % 32);
			frozenActor.RefreshState();
		}

		void ICreatesFrozenActors.OnVisibilityChanged(FrozenActor frozen)
		{
			// Ignore callbacks during initial setup
			if (!created)
				return;

			// Update state visibility to match the frozen actor to ensure consistency within the tick
			// The rest of the state will be updated by ITick.Tick below
			frozenStates[frozen.Viewer].IsVisible = !frozen.Visible;
		}

		bool IsVisibleInner(Actor self, Player byPlayer)
		{
			// If fog is disabled visibility is determined by shroud
			if (!byPlayer.Shroud.FogEnabled)
				return byPlayer.Shroud.AnyExplored(footprint);

			return frozenStates[byPlayer].IsVisible;
		}

		public bool IsVisible(Actor self, Player byPlayer)
		{
			if (byPlayer == null)
				return true;

			var stance = self.Owner.RelationshipWith(byPlayer);
			return info.AlwaysVisibleRelationships.HasStance(stance) || IsVisibleInner(self, byPlayer);
		}

		void ITick.Tick(Actor self)
		{
			if (self.Disposed)
				return;

			// Set the initial visibility state
			// This relies on actor.GetTargetablePositions(), which is not safe to use from Created
			// so we defer until the first real tick.
			if (!created && startsRevealed)
			{
				for (var playerIndex = 0; playerIndex < frozenStates.Count; playerIndex++)
					UpdateFrozenActor(self, frozenStates[playerIndex].FrozenActor, playerIndex);

				created = true;
				return;
			}

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

		void ITickRender.TickRender(WorldRenderer wr, Actor self)
		{
			IRenderable[] renderables = null;
			Rectangle[] bounds = null;
			var mouseBounds = Polygon.Empty;
			for (var playerIndex = 0; playerIndex < frozenStates.Count; playerIndex++)
			{
				var frozen = frozenStates[playerIndex].FrozenActor;
				if (!frozen.NeedRenderables)
					continue;

				if (renderables == null)
				{
					isRendering = true;
					renderables = self.Render(wr).ToArray();
					bounds = self.ScreenBounds(wr).ToArray();
					mouseBounds = self.MouseBounds(wr);

					isRendering = false;
				}

				frozen.NeedRenderables = false;
				frozen.Renderables = renderables;
				frozen.ScreenBounds = bounds;
				frozen.MouseBounds = mouseBounds;
				self.World.ScreenMap.AddOrUpdate(self.World.Players[playerIndex], frozen);
			}
		}

		IEnumerable<IRenderable> IRenderModifier.ModifyRender(Actor self, WorldRenderer wr, IEnumerable<IRenderable> r)
		{
			return IsVisible(self, self.World.RenderPlayer) || isRendering ? r : SpriteRenderable.None;
		}

		IEnumerable<Rectangle> IRenderModifier.ModifyScreenBounds(Actor self, WorldRenderer wr, IEnumerable<Rectangle> bounds)
		{
			return bounds;
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			// Force a state update for the old owner so the tooltip etc doesn't show them as the owner
			var oldOwnerIndex = self.World.Players.IndexOf(oldOwner);
			UpdateFrozenActor(self, frozenStates[oldOwnerIndex].FrozenActor, oldOwnerIndex);
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			// Invalidate the frozen actor (which exists if this actor was captured from an enemy)
			// for the current owner
			frozenStates[self.Owner].FrozenActor.Invalidate();
		}
	}

	public class HiddenUnderFogInit : RuntimeFlagInit, ISingleInstanceInit { }
}
