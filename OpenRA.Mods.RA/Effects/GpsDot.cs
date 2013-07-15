#region Copyright & License Information
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

		GpsWatcher watcher;
		HiddenUnderFog huf;
		Spy spy;
		bool show = false;

		public GpsDot(Actor self, GpsDotInfo info)
		{
			this.self = self;
			this.info = info;
			anim = new Animation("gpsdot");
			anim.PlayRepeating(info.String);

			self.World.AddFrameEndTask(w => w.Add(this));
		}

		bool firstTick = true;
		public void Tick(World world)
		{
			if (self.Destroyed)
				world.AddFrameEndTask(w => w.Remove(this));

			if (!self.IsInWorld || self.Destroyed)
				return;

			// Can be granted at runtime via a crate, so can't cache
			var cloak = self.TraitOrDefault<Cloak>();

			if (firstTick)
			{
				huf = self.TraitOrDefault<HiddenUnderFog>();
				spy = self.TraitOrDefault<Spy>();
				firstTick = false;
			}

			// Can change with the Shroud selector for observers so don't cache.
			if (self.World.RenderPlayer != null)
				watcher = self.World.RenderPlayer.PlayerActor.Trait<GpsWatcher>();

			var hasGps = (watcher != null && (watcher.Granted || watcher.GrantedAllies));
			var hasDot = (huf != null && !huf.IsVisible(self, self.World.RenderPlayer));
			var dotHidden = (cloak != null && cloak.Cloaked) || (spy != null && spy.Disguised);

			show = hasGps && hasDot && !dotHidden;
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (!show || self.Destroyed)
				return SpriteRenderable.None;

			var palette = wr.Palette(info.IndicatorPalettePrefix+self.Owner.InternalName);
			return anim.Render(self.CenterPosition, palette);
		}
	}
}
