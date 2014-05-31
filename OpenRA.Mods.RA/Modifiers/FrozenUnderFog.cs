#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class FrozenUnderFogInfo : ITraitInfo, Requires<BuildingInfo>, Requires<RenderSpritesInfo>
	{
		public readonly bool StartsRevealed = false;

		public object Create(ActorInitializer init) { return new FrozenUnderFog(init, this); }
	}

	public class FrozenUnderFog : IRenderModifier, IVisibilityModifier, ITick, ITickRender, ISync
	{
		[Sync] public int VisibilityHash;

		bool initialized, startsRevealed;
		readonly CPos[] footprint;
		Lazy<IToolTip> tooltip;
		Lazy<Health> health;

		Dictionary<Player, bool> visible;
		Dictionary<Player, FrozenActor> frozen;

		public FrozenUnderFog(ActorInitializer init, FrozenUnderFogInfo info)
		{
			// Spawned actors (e.g. building husks) shouldn't be revealed
			startsRevealed = info.StartsRevealed && !init.Contains<ParentActorInit>();
			footprint = FootprintUtils.Tiles(init.self).ToArray();
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
				var isVisible = false;
				foreach (var pos in footprint)
				{
					if (p.Shroud.IsVisible(pos))
					{
						isVisible = true;
						break;
					}
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
					p.PlayerActor.Trait<FrozenActorLayer>().Add(frozen[p] = new FrozenActor(self, footprint));
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
					actor.TooltipName = tooltip.Value.Name();
					actor.TooltipOwner = tooltip.Value.Owner();
				}
			}
		}

		public void TickRender(WorldRenderer wr, Actor self)
		{
			if (self.Destroyed || !initialized || !visible.Any(v => v.Value))
				return;

			IRenderable[] renderables = null;
			foreach (var player in self.World.Players)
				if (visible[player])
				{
					// Lazily generate a copy of the underlying data.
					if (renderables == null)
						renderables = self.Render(wr).ToArray();
					frozen[player].Renderables = renderables;
				}
		}

		public IEnumerable<IRenderable> ModifyRender(Actor self, WorldRenderer wr, IEnumerable<IRenderable> r)
		{
			return IsVisible(self, self.World.RenderPlayer) ? r : SpriteRenderable.None;
		}
	}
}