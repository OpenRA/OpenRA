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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	class WithCrateBodyInfo : ITraitInfo, Requires<RenderSpritesInfo>
	{
		public readonly string[] Images = { "crate" };
		public readonly string[] XmasImages = { };

		public object Create(ActorInitializer init) { return new WithCrateBody(init.self, this); }
	}

	class WithCrateBody : INotifyParachuteLanded
	{
		readonly Actor self;
		readonly Animation anim;

		public WithCrateBody(Actor self, WithCrateBodyInfo info)
		{
			this.self = self;
			var rs = self.Trait<RenderSprites>();
			var images = info.XmasImages.Any() && DateTime.Today.Month == 12 ? info.XmasImages : info.Images;
			anim = new Animation(self.World, images.Random(Game.CosmeticRandom));
			anim.Play("idle");
			rs.Add("", anim);
		}

		public void OnLanded()
		{
			var seq = self.World.Map.GetTerrainInfo(self.Location).IsWater ? "water" : "land";
			anim.PlayRepeating(seq);
		}
	}
}
