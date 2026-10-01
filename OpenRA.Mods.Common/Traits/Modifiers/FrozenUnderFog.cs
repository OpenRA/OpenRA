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

using System.Collections.Generic;
using System.Collections.Immutable;
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

	public class FrozenUnderFog : ICreatesFrozenActors, IRenderModifier, IDefaultVisibility,
		ITickRender, ISync, INotifyCreated, INotifyOwnerChanged, INotifyActorDisposing
	{
		[VerifySync]
		public int VisibilityHash;

		readonly FrozenUnderFogInfo info;
		readonly bool startsRevealed;
		readonly ImmutableArray<PPos> footprint;

		readonly HashSet<int> playersRequiringUpdate = [];

		PlayerDictionary<FrozenState> frozenStates;
		bool isRendering;
		bool created;

		sealed class FrozenState(FrozenActor frozenActor, int playerIndex)
		{
			public readonly FrozenActor FrozenActor = frozenActor;
			public readonly int PlayerIndex = playerIndex;
			public bool IsVisible;
		}

		public FrozenUnderFog(ActorInitializer init, FrozenUnderFogInfo info)
		{
			this.info = info;
			var map = init.World.Map;

			// Explore map-placed actors if the "Explore Map" option is enabled
			var shroudInfo = init.World.Map.Rules.Actors[SystemActors.Player].TraitInfo<ShroudInfo>();
			var exploredMap = init.World.LobbyInfo.GlobalSettings.OptionOrDefault("explored", shroudInfo.ExploredMapCheckboxEnabled);
			startsRevealed = exploredMap && init.Contains<SpawnedByMapInit>() && !init.Contains<HiddenUnderFogInit>();

			var buildingInfo = init.Self.Info.TraitInfoOrDefault<BuildingInfo>();
			var footprintCells = buildingInfo?.FrozenUnderFogTiles(init.Self.Location).ToArray() ?? [init.Self.Location];
			footprint = footprintCells.SelectMany(c => map.ProjectedCellsCovering(c.ToMPos(map))).ToImmutableArray();
		}

		void INotifyCreated.Created(Actor self)
		{
			frozenStates = new PlayerDictionary<FrozenState>(self.World, (player, playerIndex) =>
			{
				var frozenActor = new FrozenActor(self, this, footprint, player, startsRevealed);
				player.PlayerActor.Trait<FrozenActorLayer>().Add(frozenActor);
				return new FrozenState(frozenActor, playerIndex) { IsVisible = !frozenActor.Visible };
			});

			// Set the initial visibility state
			// This relies on actor.GetTargetablePositions(), which is also setup up in Created.
			// Since we can't be sure whether our method will run after theirs, defer by a frame.
			self.World.AddFrameEndTask(_ =>
			{
				for (var playerIndex = 0; playerIndex < frozenStates.Count; playerIndex++)
				{
					var state = frozenStates[playerIndex];
					var frozen = state.FrozenActor;
					if (startsRevealed || state.IsVisible)
						UpdateFrozenActor(frozen, state.PlayerIndex);

					frozen.RefreshHidden();
				}
			});

			created = true;
		}

		void UpdateFrozenActor(FrozenActor frozenActor, int playerIndex)
		{
			VisibilityHash |= 1 << (playerIndex % 32);
			frozenActor.RefreshState();

			if (frozenActor.NeedRenderables)
				playersRequiringUpdate.Add(playerIndex);
		}

		void ICreatesFrozenActors.OnVisibilityChanged(FrozenActor frozen)
		{
			// Ignore callbacks during initial setup
			if (!created)
				return;

			// Update state visibility to match the frozen actor to ensure consistency
			var state = frozenStates[frozen.Viewer];
			state.IsVisible = !frozen.Visible;

			if (state.IsVisible || frozen.NeedRenderables)
				UpdateFrozenActor(frozen, state.PlayerIndex);

			frozen.RefreshHidden();
		}

		bool IsVisibleInner(Player byPlayer)
		{
			// If fog is disabled visibility is determined by shroud
			if (!byPlayer.Shroud.FogEnabled)
				return byPlayer.Shroud.AnyExplored(footprint.AsSpan());

			return frozenStates[byPlayer].IsVisible;
		}

		public bool IsVisible(Actor self, Player byPlayer)
		{
			if (byPlayer == null)
				return true;

			var relationship = self.Owner.RelationshipWith(byPlayer);
			return info.AlwaysVisibleRelationships.HasRelationship(relationship) || IsVisibleInner(byPlayer);
		}

		void ITickRender.TickRender(WorldRenderer wr, Actor self)
		{
			if (playersRequiringUpdate.Count == 0)
				return;

			ImmutableArray<IRenderable> renderables = default;
			ImmutableArray<Rectangle> bounds = default;
			var mouseBounds = Polygon.Empty;
			foreach (var playerIndex in playersRequiringUpdate)
			{
				var state = frozenStates[playerIndex];
				var frozen = state.FrozenActor;

				if (!frozen.NeedRenderables)
					continue;

				if (renderables == default)
				{
					isRendering = true;
					renderables = self.Render(wr).ToImmutableArray();
					bounds = self.ScreenBounds(wr).ToImmutableArray();
					mouseBounds = self.MouseBounds(wr);

					isRendering = false;
				}

				frozen.NeedRenderables = false;
				frozen.Renderables = renderables;
				frozen.ScreenBounds = bounds;
				frozen.MouseBounds = mouseBounds;

				self.World.ScreenMap.AddOrUpdate(self.World.Players[playerIndex], frozen);
			}

			playersRequiringUpdate.Clear();
		}

		IEnumerable<IRenderable> IRenderModifier.ModifyRender(Actor self, WorldRenderer wr, IEnumerable<IRenderable> r)
		{
			return isRendering || IsVisible(self, self.World.RenderPlayer) ? r : SpriteRenderable.None;
		}

		IEnumerable<Rectangle> IRenderModifier.ModifyScreenBounds(Actor self, WorldRenderer wr, IEnumerable<Rectangle> bounds)
		{
			return bounds;
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			// Force a state update for the old owner so the tooltip etc doesn't show them as the owner
			var oldState = frozenStates[oldOwner];
			UpdateFrozenActor(oldState.FrozenActor, oldState.PlayerIndex);
			oldState.FrozenActor.RefreshHidden();
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
