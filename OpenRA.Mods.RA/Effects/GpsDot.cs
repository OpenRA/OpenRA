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
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.RA.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Effects
{
	[Desc("Attach this to actors to render pictograms while hidden.")]
	class GpsDotInfo : ITraitInfo
	{
		[Desc("Sprite collection for symbols.")]
		public readonly string Image = "gpsdot";

		[Desc("Sprite used for this actor.")]
		[SequenceReference("Image")] public readonly string String = "Infantry";

		[PaletteReference(true)] public readonly string IndicatorPalettePrefix = "player";

		public object Create(ActorInitializer init)
		{
			return new GpsDot(init.Self, this);
		}
	}

	class GpsDot : IEffect
	{
		readonly Actor self;
		readonly GpsDotInfo info;
		readonly Animation anim;

		readonly PlayerDictionary<DotState> dotStates;
		readonly Lazy<HiddenUnderFog> huf;
		readonly Lazy<FrozenUnderFog> fuf;
		readonly Lazy<Disguise> disguise;
		readonly Lazy<Cloak> cloak;
		readonly Cache<Player, FrozenActorLayer> frozen;

		class DotState
		{
			public readonly GpsWatcher Gps;
			public bool IsTargetable;
			public bool ShouldRender;
			public DotState(GpsWatcher gps)
			{
				Gps = gps;
			}
		}

		public GpsDot(Actor self, GpsDotInfo info)
		{
			this.self = self;
			this.info = info;
			anim = new Animation(self.World, info.Image);
			anim.PlayRepeating(info.String);

			self.World.AddFrameEndTask(w => w.Add(this));

			huf = Exts.Lazy(() => self.TraitOrDefault<HiddenUnderFog>());
			fuf = Exts.Lazy(() => self.TraitOrDefault<FrozenUnderFog>());
			disguise = Exts.Lazy(() => self.TraitOrDefault<Disguise>());
			cloak = Exts.Lazy(() => self.TraitOrDefault<Cloak>());

			frozen = new Cache<Player, FrozenActorLayer>(p => p.PlayerActor.Trait<FrozenActorLayer>());
			dotStates = new PlayerDictionary<DotState>(self.World, player => new DotState(player.PlayerActor.Trait<GpsWatcher>()));
		}

		public bool IsDotVisible(Player toPlayer)
		{
			return dotStates[toPlayer].IsTargetable;
		}

		bool IsTargetableBy(Player toPlayer, out bool shouldRenderIndicator)
		{
			shouldRenderIndicator = false;

			if (cloak.Value != null && cloak.Value.Cloaked)
				return false;

			if (disguise.Value != null && disguise.Value.Disguised)
				return false;

			if (huf.Value != null && !huf.Value.IsVisible(self, toPlayer)
				&& toPlayer.Shroud.IsExplored(self.CenterPosition))
			{
				var f1 = FrozenActorForPlayer(toPlayer);
				shouldRenderIndicator = f1 == null || !f1.HasRenderables;
				return true;
			}

			if (fuf.Value == null)
				return false;

			var f2 = FrozenActorForPlayer(toPlayer);
			if (f2 == null)
				return false;

			shouldRenderIndicator = !f2.HasRenderables;

			return f2.Visible && !f2.Shrouded;
		}

		FrozenActor FrozenActorForPlayer(Player player)
		{
			return frozen[player].FromID(self.ActorID);
		}

		public void Tick(World world)
		{
			if (self.Disposed)
				world.AddFrameEndTask(w => w.Remove(this));

			if (!self.IsInWorld || self.IsDead)
				return;

			for (var playerIndex = 0; playerIndex < dotStates.Count; playerIndex++)
			{
				var state = dotStates[playerIndex];
				var shouldRender = false;
				var targetable = (state.Gps.Granted || state.Gps.GrantedAllies) && IsTargetableBy(world.Players[playerIndex], out shouldRender);
				state.IsTargetable = targetable;
				state.ShouldRender = targetable && shouldRender;
			}
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (self.World.RenderPlayer == null || !dotStates[self.World.RenderPlayer].ShouldRender || self.Disposed)
				return SpriteRenderable.None;

			var palette = wr.Palette(info.IndicatorPalettePrefix + self.Owner.InternalName);
			return anim.Render(self.CenterPosition, palette);
		}
	}
}
