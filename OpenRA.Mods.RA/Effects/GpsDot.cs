﻿#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Effects;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Effects
{
	class GpsDotInfo : ITraitInfo
	{
		public readonly string String = "Infantry";
		public readonly string IndicatorPalettePrefix = "player";

		public object Create(ActorInitializer init)
		{
			return new GpsDot(init.self, this);
		}
	}

	class GpsDot : IEffect
	{
		Actor self;
		GpsDotInfo info;
		Animation anim;

		Lazy<HiddenUnderFog> huf;
		Lazy<FrozenUnderFog> fuf;
		Lazy<Spy> spy;
		Cache<Player, GpsWatcher> watcher;
		Cache<Player, FrozenActorLayer> frozen;

		bool show = false;

		public GpsDot(Actor self, GpsDotInfo info)
		{
			this.self = self;
			this.info = info;
			anim = new Animation("gpsdot");
			anim.PlayRepeating(info.String);

			self.World.AddFrameEndTask(w => w.Add(this));

			huf = Lazy.New(() => self.TraitOrDefault<HiddenUnderFog>());
			fuf = Lazy.New(() => self.TraitOrDefault<FrozenUnderFog>());
			spy = Lazy.New(() => self.TraitOrDefault<Spy>());
			watcher = new Cache<Player, GpsWatcher>(p => p.PlayerActor.Trait<GpsWatcher>());
			frozen = new Cache<Player, FrozenActorLayer>(p => p.PlayerActor.Trait<FrozenActorLayer>());
		}

		bool ShouldShowIndicator()
		{
			// Can be granted at runtime via a crate, so can't cache
			var cloak = self.TraitOrDefault<Cloak>();
			if (cloak != null && cloak.Cloaked)
				return false;

			if (spy.Value != null && spy.Value.Disguised)
				return false;

			if (huf.Value != null && !huf.Value.IsVisible(self, self.World.RenderPlayer))
				return true;

			if (fuf.Value == null)
				return false;

			var f = frozen[self.World.RenderPlayer].FromID(self.ActorID);
			return f.Visible && !f.HasRenderables;
		}

		public void Tick(World world)
		{
			if (self.Destroyed)
				world.AddFrameEndTask(w => w.Remove(this));

			show = false;
			if (!self.IsInWorld || self.Destroyed || self.World.RenderPlayer == null)
				return;

			var gps = watcher[self.World.RenderPlayer];
			show = (gps.Granted || gps.GrantedAllies) && ShouldShowIndicator();
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (!show || self.Destroyed)
				return SpriteRenderable.None;

			var palette = wr.Palette(info.IndicatorPalettePrefix + self.Owner.InternalName);
			return anim.Render(self.CenterPosition, palette);
		}
	}
}
