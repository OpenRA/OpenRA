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
	class GpsDotInfo : ITraitInfo, Requires<RenderSimpleInfo>
	{
		public readonly string String = "Infantry";
		public object Create(ActorInitializer init)
		{
			return new GpsDot(init, String);
		}
	}

	class GpsDot : IEffect
	{
		Actor self;
		GpsWatcher watcher;
		RenderSimple rs;
		bool show = false;
		Animation anim;

		public GpsDot(ActorInitializer init, string s)
		{
			anim = new Animation("gpsdot");
			anim.PlayRepeating(s);

			self = init.self;
			rs = self.Trait<RenderSimple>();
			self.World.AddFrameEndTask(w => w.Add(this));
			if(self.World.LocalPlayer != null)
				watcher = self.World.LocalPlayer.PlayerActor.Trait<GpsWatcher>();
		}

		public void Tick(World world)
		{
			show = false;

			if (self.Destroyed)
				world.AddFrameEndTask(w => w.Remove(this));

			if (world.LocalPlayer == null)
				return;

			if (
				self.IsInWorld
				&& (watcher.Granted || watcher.GrantedAllies)
				&& !self.Trait<HiddenUnderFog>().IsVisible(self)
				&& (!self.HasTrait<Cloak>() || !self.Trait<Cloak>().Cloaked)
				)
			{
				show = true;
			}
		}

		public IEnumerable<Renderable> Render()
		{
			if (show && !self.Destroyed)
			{
				var p = self.CenterLocation;
				yield return new Renderable(anim.Image, p - 0.5f * anim.Image.size, rs.Palette(self.Owner), p.Y)
					.WithScale(1.5f);
			}
		}
	}
}
