#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
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
				var frozenActor = new FrozenActor(self, footprint, player, startsRevealed);
				player.PlayerActor.Trait<FrozenActorLayer>().Add(frozenActor);
				return new FrozenState(frozenActor) { IsVisible = startsRevealed };
			});

			if (startsRevealed)
				for (var playerIndex = 0; playerIndex < frozenStates.Count; playerIndex++)
					UpdateFrozenActor(self, frozenStates[playerIndex].FrozenActor, playerIndex);
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

		void ITick.Tick(Actor self)
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

		void ITickRender.TickRender(WorldRenderer wr, Actor self)
		{
			IRenderable[] renderables = null;
			Rectangle[] bounds = null;
			Rectangle mouseBounds = Rectangle.Empty;
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
	}

	public class HiddenUnderFogInit : IActorInit { }
}