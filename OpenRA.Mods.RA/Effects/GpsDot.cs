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
using System.Drawing;
using OpenRA.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Effects
{
	class GpsDotInfo : ITraitInfo
	{
		public readonly string String = "o";
		public object Create(ActorInitializer init)
		{
			return new GpsDot(init, String);
		}
	}
	class GpsDot : IEffect
	{
		string s;
		int2 loc;
		Color color;
		Actor self;
		GpsWatcher watcher;
		bool show = false;

		public GpsDot(ActorInitializer init, string s)
		{
			this.s = s;
			self = init.self;
			loc = self.CenterLocation;
			color = self.Owner.ColorRamp.GetColor(0);
			self.World.AddFrameEndTask(w => w.Add(this));
			if(self.World.LocalPlayer != null)
				watcher = self.World.LocalPlayer.PlayerActor.Trait<GpsWatcher>();
		}

		public void Tick(World world)
		{
			show = false;
			
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
				loc = self.CenterLocation;
			}
		}

		public IEnumerable<Renderable> Render()
		{
			if(show)
				Game.Renderer.TinyBoldFont.DrawTextWithContrast(s, loc - Game.viewport.Location, color, Color.Black, 1);
			yield break;
		}
	}
}
