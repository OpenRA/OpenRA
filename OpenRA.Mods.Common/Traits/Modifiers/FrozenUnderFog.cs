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
		readonly MPos[] footprint;

		readonly Lazy<IToolTip> tooltip;
		readonly Lazy<Health> health;

		readonly Dictionary<Player, bool> visible;
		readonly Dictionary<Player, FrozenActor> frozen;

		bool initialized;

		public FrozenUnderFog(ActorInitializer init, FrozenUnderFogInfo info)
		{
			this.info = info;

			// Spawned actors (e.g. building husks) shouldn't be revealed
			startsRevealed = info.StartsRevealed && !init.Contains<ParentActorInit>();
			var footprintCells = FootprintUtils.Tiles(init.Self).ToList();
			footprint = footprintCells.Select(cell => cell.ToMPos(init.World.Map)).ToArray();
			tooltip = Exts.Lazy(() => init.Self.TraitsImplementing<IToolTip>().FirstOrDefault());
			health = Exts.Lazy(() => init.Self.TraitOrDefault<Health>());

			frozen = new Dictionary<Player, FrozenActor>();
			visible = init.World.Players.ToDictionary(p => p, p => false);
		}

		public bool IsVisible(Actor self, Player byPlayer)
		{
			if (byPlayer == null)
				return true;

			var stance = self.Owner.Stances[byPlayer];
			return info.AlwaysVisibleStances.HasStance(stance) || visible[byPlayer];
		}

		public void Tick(Actor self)
		{
			if (self.Disposed)
				return;

			VisibilityHash = 0;
			foreach (var player in self.World.Players)
			{
				bool isVisible;
				FrozenActor frozenActor;
				if (!initialized)
				{
					frozen[player] = frozenActor = new FrozenActor(self, footprint, player.Shroud);
					frozen[player].NeedRenderables = frozenActor.NeedRenderables = startsRevealed;
					player.PlayerActor.Trait<FrozenActorLayer>().Add(frozenActor);
					isVisible = visible[player] |= startsRevealed;
				}
				else
				{
					frozenActor = frozen[player];
					isVisible = visible[player] = !frozenActor.Visible;
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
			return IsVisible(self, self.World.RenderPlayer) || (initialized && frozen[self.World.RenderPlayer].IsRendering) ? r : SpriteRenderable.None;
		}
	}
}